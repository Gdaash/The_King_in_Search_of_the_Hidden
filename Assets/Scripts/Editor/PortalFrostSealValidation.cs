#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFoundation.Combat;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PortalFrostSealValidation
{
    const string Key = "PortalFrostSealValidation";
    static IEnumerator routine;
    static int checks;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static PortalFrostSealValidation() { EditorApplication.playModeStateChanged += State; }
    [MenuItem("Tools/Validation/Portal Frost Seal")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first");
        SessionState.SetString(Key + ".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/World.unity");
        SessionState.SetBool(Key, true); SessionState.SetString(Key + ".result", "Running");
        BuildingUpgradeValidation.Begin();
    }
    static void State(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { checks = 0; routine = Audit(); EditorApplication.update += Tick; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick; routine = null; SessionState.SetBool(Key, false);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + ".scene", ""));
        }
    }
    static void Tick()
    {
        try { if (routine != null && routine.MoveNext()) return; Finish("PASS: " + checks + " frost seal checks"); }
        catch (Exception error) { Debug.LogException(error); Finish("FAILED: " + error.Message); }
    }
    static void Finish(string result) { EditorApplication.update -= Tick; routine = null; SessionState.SetString(Key + ".result", result); Debug.Log(result); EditorApplication.isPlaying = false; }
    static void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks++; Debug.Log("Frost seal audit PASS: " + label); }
    static double Until(float seconds) => EditorApplication.timeSinceStartup + seconds;
    static SpriteRenderer[] Seals() => Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Where(s => s.name == "Portal Frost Seal").ToArray();
    static EnemyProjectile[] Shots(string name) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Where(p => p.name == "Portal " + name + "(Clone)").ToArray();
    static IEnumerator AuditProjectiles(PortalTowerProgression p, PortalWeaponController controller, ArcherTower tower)
    {
        tower.enabled = false;
        var ranks = (Dictionary<string, int>)typeof(PortalTowerProgression).GetField("ranks", Private).GetValue(p);
        var times = (Dictionary<PortalTowerBalance.Weapon, float>)typeof(PortalWeaponController).GetField("projectileTimes", Private).GetValue(controller);
        var source = tower.GetComponent<TowerVisuals>().ShootPoint;
        var enemy = new GameObject("Projectile audit target"); enemy.tag = "Enemy1";
        enemy.transform.position = source.position + Vector3.right * 2;
        var collider = enemy.AddComponent<CircleCollider2D>(); collider.radius = .65f; collider.isTrigger = true;
        var body = enemy.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        var combat = enemy.AddComponent<Combatant>(); combat.baseHealth = 1000;
        var defense = ScriptableObject.CreateInstance<CombatDefenseProfile>(); defense.fireMultiplier = .5f; defense.electricMultiplier = .5f; combat.defense = defense;
        var health = enemy.AddComponent<Health>(); health.OnDeath = new UnityEvent(); health.OnHealthChanged = new UnityEvent<float>();
        foreach (var weapon in new[] { PortalTowerBalance.Weapon.FireShot, PortalTowerBalance.Weapon.ElectricOrb })
        {
            controller.SendMessage("ClearPortalProjectiles"); ranks.Clear(); ((Dictionary<string,float>)typeof(PortalTowerProgression).GetField("acquiredValues",Private).GetValue(p)).Clear(); ranks["weapon_" + weapon] = 1;
            var definition = p.Balance.FindWeapon(weapon);
            Check(definition != null && definition.deliveryMethod == PortalTowerBalance.DeliveryMethod.Projectile && definition.projectilePrefab != null, weapon + " is fully configured as projectile weapon");
            float before = health.CurrentHealth;
            double wait = Until(.25f);
            while (EditorApplication.timeSinceStartup < wait && Shots(weapon.ToString()).Length == 0) yield return null;
            var shots = Shots(weapon.ToString());
            int count = weapon == PortalTowerBalance.Weapon.FireShot ? 3 : 1;
            Check(shots.Length == count, weapon + " emits authored base projectile count");
            times[weapon] = Time.time + 100;
            Check(shots.All(s => s.HitType == definition.damageType && s.HitDamage == definition.damage), weapon + " projectiles carry correct elemental damage");
            if (weapon == PortalTowerBalance.Weapon.FireShot)
            {
                float spread = shots.Max(a => shots.Max(b => Vector3.Angle(a.Direction, b.Direction)));
                Check(Mathf.Abs(spread - 14) < .1f && shots.All(s => s.transform.localScale.x < 1), "Fire pellets form a small 14 degree fan");
            }
            else
            {
                Check(shots[0].CurrentSpeed < 1 && shots[0].transform.localScale.x >= 2, "Electric projectile is large and very slow");
                float x = shots[0].transform.position.x; Time.timeScale = 0;
                wait = Until(.2f); while (EditorApplication.timeSinceStartup < wait) yield return null;
                Check(shots[0] != null && Mathf.Approximately(shots[0].transform.position.x, x), "Projectile movement stops during pause"); Time.timeScale = 1;
            }
            wait = Until(5);
            while (EditorApplication.timeSinceStartup < wait && Shots(weapon.ToString()).Length > 0) yield return null;
            Check(Mathf.Approximately(before - health.CurrentHealth, definition.damage * count * .5f), weapon + " real physics hits use elemental resistance exactly once");
            ranks["projectile_count"] = 1;
            ranks["all_damage"] = 1;
            Check(p.Count(weapon) == count + 1, weapon + " inherits shared projectile count");
            controller.SendMessage("ClearPortalProjectiles"); times[weapon] = Time.time;
            wait = Until(.2f); while (EditorApplication.timeSinceStartup < wait && Shots(weapon.ToString()).Length == 0) yield return null;
            Check(Shots(weapon.ToString()).Length == count + 1, weapon + " emits extra projectile after shared upgrade");
            Check(Shots(weapon.ToString()).All(s => Mathf.Approximately(s.HitDamage, definition.damage * p.Multiplier(weapon, PortalTowerBalance.Effect.Damage))) && Shots(weapon.ToString())[0].HitDamage > definition.damage, weapon + " inherits shared damage upgrade");
            controller.SendMessage("ClearPortalProjectiles");
        }
        ranks.Clear(); ((Dictionary<string,float>)typeof(PortalTowerProgression).GetField("acquiredValues",Private).GetValue(p)).Clear(); ranks["weapon_Lightning"] = 1;
        Check(p.Count(PortalTowerBalance.Weapon.Lightning) == 1 && p.Count(PortalTowerBalance.Weapon.Bolts) == 1, "Lightning keeps a single hit");
        enemy.transform.position = tower.transform.position + Vector3.right * 1.5f; body.position = enemy.transform.position;
        typeof(PortalWeaponController).GetField("lightningTime", Private).SetValue(controller, Time.time + 100);
        double cacheWait = Until(.25f); while (EditorApplication.timeSinceStartup < cacheWait) yield return null;
        float lightningHealth = health.CurrentHealth;
        controller.SendMessage("LightningVolley");
        Check(Mathf.Approximately(lightningHealth - health.CurrentHealth, p.Balance.FindWeapon(PortalTowerBalance.Weapon.Lightning).damage * .5f), "Instant strike hits one enemy with resistance");
        ranks["weapon_FrostSeal"] = 1; ranks["area_size"] = 1;
        Check(Mathf.Approximately(controller.FrostRadius, p.Balance.FindWeapon(PortalTowerBalance.Weapon.FrostSeal).radius * 1.1f), "Area upgrade increases radius linearly");
        Check(p.Balance.Find("projectile_count").maximumRank == 10 && p.Balance.Find("instant_barrage") == null, "Projectile family has ten ranks and instant count upgrades are absent");
        ranks.Clear(); ((Dictionary<string,float>)typeof(PortalTowerProgression).GetField("acquiredValues",Private).GetValue(p)).Clear(); Object.Destroy(enemy); Object.Destroy(defense);
        double end = Until(.2f); while (EditorApplication.timeSinceStartup < end) yield return null;
    }
    static IEnumerator AuditAttackSlow()
    {
        foreach (bool ranged in new[] { false, true })
        {
            var target = new GameObject("Attack slow audit target"); target.tag="Player"; target.transform.position=new Vector3(80,80);
            var enemy = new GameObject("Attack slow audit enemy"); enemy.tag="Enemy1"; enemy.transform.position=target.transform.position+Vector3.right*.1f;
            var combat=enemy.AddComponent<Combatant>(); var weapon=ScriptableObject.CreateInstance<CombatWeapon>();weapon.interval=.2f;weapon.range=2;combat.weapon=weapon;
            MonoBehaviour ai=ranged?(MonoBehaviour)enemy.AddComponent<EnemyAI_Ranged>():enemy.AddComponent<EnemyAI>();
            var move=enemy.AddComponent<EnemyMovement>();
            var so=new SerializedObject(ai);so.FindProperty("cooldownVariation").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();
            int count=0;var onAttack=new UnityEvent();onAttack.AddListener(()=>{count++;((IEnemyAI)ai).FinishAttack();});ai.GetType().GetField("OnAttack").SetValue(ai,onAttack);
            double wait=Until(.1f);while(EditorApplication.timeSinceStartup<wait)yield return null;
            int[] hits=new int[3];object slowSource=new object();
            for(int phase=0;phase<3;phase++)
            {
                move.SetAreaSlow(slowSource,phase==1?.5f:0);count=0;
                ai.GetType().GetField("_nextAttackTime",Private).SetValue(ai,Time.time+.2f);
                wait=Until(1.3f);while(EditorApplication.timeSinceStartup<wait)yield return null;hits[phase]=count;
            }
            Check(hits[0]>=5 && hits[1]>=2 && hits[1]<=hits[0]*.7f, (ranged?"Ranged":"Melee")+" actual attack frequency halves in slow area ("+string.Join(",",hits)+")");
            Check(hits[2]>=hits[0]-1, (ranged?"Ranged":"Melee")+" actual attack rate restores after leaving");
            Object.Destroy(enemy);Object.Destroy(target);Object.Destroy(weapon);
            wait=Until(.1f);while(EditorApplication.timeSinceStartup<wait)yield return null;
        }
    }
    static IEnumerator Audit()
    {
        double wait = Until(.4f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        var p = PortalTowerProgression.Instance;
        var controller = Object.FindFirstObjectByType<PortalWeaponController>();
        var tower = Object.FindObjectsByType<ArcherTower>(FindObjectsSortMode.None).First(t => t.GetStats() != null && t.GetStats().IsPortalTower);
        Check(p.IsInitialWeaponChoice && p.Level == 1 && Time.timeScale == 0, "Starting weapon choice pauses the run at level one");
        if (p.IsInitialWeaponChoice) p.Purchase(p.Offers[0].id);
        var shots = AuditProjectiles(p, controller, tower);
        while (shots.MoveNext()) yield return shots.Current;
        var attacks=AuditAttackSlow(); while(attacks.MoveNext()) yield return attacks.Current;
        var definition = p.Balance.FindWeapon(PortalTowerBalance.Weapon.FrostSeal);
        Check(definition != null && definition.damageType == DamageType.Ice && definition.deliveryMethod == PortalTowerBalance.DeliveryMethod.Area, "Cold damage and area delivery authored");
        Check(definition.radius == p.Balance.FindWeapon(PortalTowerBalance.Weapon.Beam).radius, "Base radius matches burning beam");
        Check(controller.frostSealSprite != null && controller.frostSealSprite.texture.filterMode == FilterMode.Point, "Frost art is assigned and uses Point filtering");
        var ranks = (Dictionary<string, int>)typeof(PortalTowerProgression).GetField("ranks", Private).GetValue(p);
        ranks.Clear(); ((Dictionary<string,float>)typeof(PortalTowerProgression).GetField("acquiredValues",Private).GetValue(p)).Clear(); ranks["weapon_FrostSeal"] = 1; tower.enabled = false;
        typeof(PortalWeaponController).GetField("frostTime", Private).SetValue(controller, Time.time + 100);
        wait = Until(.2f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(controller.ActiveFrostSealCount == 0, "No seal without a target");
        var enemy = new GameObject("Frost audit enemy"); enemy.tag = "Enemy1"; enemy.transform.position = tower.transform.position + Vector3.right * 3;
        var body = enemy.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic;
        var combat = enemy.AddComponent<Combatant>(); combat.baseHealth = 1000; combat.baseSpeed = 1.5f;
        var defense = ScriptableObject.CreateInstance<CombatDefenseProfile>(); defense.iceMultiplier = 0; combat.defense = defense;
        var health = enemy.AddComponent<Health>(); health.OnDeath = new UnityEvent(); health.OnHealthChanged = new UnityEvent<float>();
        var agent = enemy.AddComponent<EnemyAI>(); agent.enabled = false;
        typeof(EnemyAI).GetField("_target", Private).SetValue(agent, tower.transform);
        var movement = enemy.AddComponent<EnemyMovement>(); movement.SetMove(true);
        wait = Until(.2f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Vector2 before = enemy.transform.position;
        typeof(PortalWeaponController).GetField("frostTime", Private).SetValue(controller, Time.time);
        wait = Until(.12f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(controller.ActiveFrostSealCount == 1 && Seals().Length == 1, "Seal spawns for an in-range enemy");
        var seal = Seals()[0]; Vector2 center = seal.transform.position;
        Check(Vector2.Dot(center - before, (Vector2)tower.transform.position - before) > 0, "Seal is placed ahead of enemy toward portal");
        Check(Vector2.Distance(center, before) < definition.radius * .5f && PortalWeaponController.ContainsFrostPoint(enemy.transform.position, center, controller.FrostRadius), "Short base-radius offset keeps target inside newly placed seal");
        movement.SetMove(false); body.position = center; enemy.transform.position = center;
        typeof(PortalWeaponController).GetField("frostTime", Private).SetValue(controller, Time.time + 100);
        wait = Until(.4f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(Mathf.Approximately(health.CurrentHealth, 1000), "Cold immunity blocks seal damage");
        Check(Mathf.Approximately(movement.AreaSlowMultiplier, .5f), "Inside seal applies 50 percent slow");
        Check((Vector2)seal.transform.position == center, "Ground seal stays stationary");
        Check(Mathf.Approximately(agent.AttackSpeedMultiplier,.5f), "Seal slows melee attack speed by 50 percent");
        Check(definition.damage == 5 && definition.damageTickInterval == 1, "Frost baseline DPS is five damage per second");
        Check(Mathf.Approximately(seal.color.a, .5f), "Seal steady opacity is 50 percent");
        defense.iceMultiplier = .5f;
        float initialHealth = health.CurrentHealth;
        wait = Until(1.05f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(health.CurrentHealth < initialHealth && Mathf.Abs((initialHealth - health.CurrentHealth) % 2.5f) < .001f, "Periodic damage uses cold resistance");
        movement.SetMove(true);
        wait = Until(.1f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(Mathf.Abs(body.linearVelocity.magnitude / movement.CombatSpeed - .5f) < .03f, "Slow affects actual movement velocity");
        movement.SetMove(false); float pausedHealth = health.CurrentHealth; Time.timeScale = 0;
        wait = Until(.4f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(health.CurrentHealth == pausedHealth && seal != null, "Pause stops damage and seal lifetime"); Time.timeScale = 1;
        body.position = center + Vector2.up * 3; enemy.transform.position = body.position;
        wait = Until(.15f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(movement.AreaSlowMultiplier == 1, "Leaving area restores movement speed");
        Check(agent.AttackSpeedMultiplier == 1, "Leaving area restores attack speed");
        Check(!PortalWeaponController.ContainsFrostPoint(center + new Vector2(controller.FrostRadius, controller.FrostRadius), center, controller.FrostRadius), "Hex corner outside footprint is not hit");
        controller.SendMessage("SpawnFrostSeal", center);
        body.position = center; enemy.transform.position = center;
        wait = Until(.15f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(Mathf.Approximately(movement.AreaSlowMultiplier, .5f), "Overlapping seals do not multiply slow");
        controller.SendMessage("RemoveFrostSeal", 0);
        wait = Until(.1f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(Mathf.Approximately(movement.AreaSlowMultiplier, .5f), "Remaining seal keeps its slow after first expires");
        controller.SendMessage("ClearFrostSeals");
        Check(movement.AreaSlowMultiplier == 1, "Clearing all seals restores speed");
        ranks["area_size"] = 1; ranks["area_duration"] = 1; ranks["all_damage"] = 1;
        Check(Mathf.Approximately(controller.FrostRadius, definition.radius * 1.1f), "Shared area upgrade scales real radius");
        Check(Mathf.Approximately(p.Multiplier(PortalTowerBalance.Weapon.FrostSeal, PortalTowerBalance.Effect.Duration), 1.05f) && p.Multiplier(PortalTowerBalance.Weapon.Beam, PortalTowerBalance.Effect.Duration) == 1, "Duration upgrade applies only to persistent areas");
        Check(Mathf.Approximately(p.Multiplier(PortalTowerBalance.Weapon.FrostSeal, PortalTowerBalance.Effect.SlowStrength), 1f), "Base slow strength remains fixed");
        Check(Mathf.Approximately(p.Multiplier(PortalTowerBalance.Weapon.FrostSeal, PortalTowerBalance.Effect.Damage), 1.1f) && Mathf.Approximately(p.DamageMultiplier,1.1f), "Shared damage affects frost and magic bolts");
        ranks.Clear(); ((Dictionary<string,float>)typeof(PortalTowerProgression).GetField("acquiredValues",Private).GetValue(p)).Clear(); ranks["weapon_FrostSeal"] = 1;
        controller.SendMessage("SpawnFrostSeal", center);
        wait = Until(3.2f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(controller.ActiveFrostSealCount == 0 && movement.AreaSlowMultiplier == 1, "Seal expires after three seconds and releases slow");
        controller.SendMessage("SpawnFrostSeal", center);
        wait = Until(.1f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        controller.enabled = false;
        Check(controller.ActiveFrostSealCount == 0 && movement.AreaSlowMultiplier == 1, "Disabling controller clears seals and modifiers"); controller.enabled = true;
        controller.SendMessage("SpawnFrostSeal", center); p.EndExpedition();
        wait = Until(.1f); while (EditorApplication.timeSinceStartup < wait) yield return null;
        Check(controller.ActiveFrostSealCount == 0 && movement.AreaSlowMultiplier == 1, "Ending run clears seals and modifiers");
        Object.Destroy(enemy); Object.Destroy(defense);
    }
}
#endif
