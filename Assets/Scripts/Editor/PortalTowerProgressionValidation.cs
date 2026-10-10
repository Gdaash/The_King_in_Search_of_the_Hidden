#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PortalTowerProgressionValidation
{
    const string Key = "PortalTowerProgressionValidation";
    static IEnumerator routine;
    static int checks;
    static PortalTowerProgressionValidation() { EditorApplication.playModeStateChanged += StateChanged; }
    [MenuItem("Tools/Validation/Portal Tower Progression")]
    public static void Run()
    {
        // The laboratory-style purchase audit belongs to the retired UI.
        PortalRoguelikeValidation.Run();
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        { checks = 0; routine = Audit(); EditorApplication.update += Tick; }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick; routine = null;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + ".scene", ""));
            SessionState.SetBool(Key, false);
        }
    }
    static void Tick()
    {
        try { if (routine != null && routine.MoveNext()) return; Finish(true, "PASS: " + checks + " portal tower progression checks."); }
        catch (Exception e) { Debug.LogException(e); Finish(false, e.Message); }
    }
    static void Finish(bool pass, string text)
    {
        EditorApplication.update -= Tick; routine = null;
        SessionState.SetString(Key + ".result", text);
        if (pass) Debug.Log(text); else Debug.LogError(text);
        EditorApplication.isPlaying = false;
    }
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception("Portal tower: " + message); checks++; }
    static IEnumerator Audit()
    {
        yield return null; yield return null;
        var p = PortalTowerProgression.Instance;
        var lights = Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        var bar = Object.FindFirstObjectByType<PortalTowerExperienceBar>();
        var view = Object.FindObjectsByType<LaboratoryUpgradeList>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(v => v.portalProgression != null);
        var tower = Object.FindObjectsByType<ArcherTower>(FindObjectsSortMode.None).First(t => t.GetStats() != null && t.GetStats().IsPortalTower);
        Check(p != null && p.Level == 1 && p.Experience == 0 && p.LevelPoints == 0, "new run starts fresh");
        Check(lights.LightCount == 1 && !tower.GetStats().CanAttack, "one beam and locked attack at start");
        Check(bar.fill.rectTransform.rect.width == 0, "empty XP bar really empty");
        Check(!p.Purchase("lights"), "cannot buy without points");
        var hex = Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None).Where(h => !h.shouldAutoUnlock)
            .OrderBy(h => Vector3.Distance(h.transform.position, tower.transform.position)).First();
        hex.RemoveHex(); int xp = p.Experience;
        Check(xp == 3, "hex awards three XP"); hex.RemoveHex(); Check(p.Experience == xp, "opening cannot award twice");
        var crystals = Object.FindObjectsByType<PortalExperienceCrystal>(FindObjectsSortMode.None);
        Check(crystals.Length == 3 && crystals.All(c => Vector3.Distance(c.transform.position, hex.transform.position) < .01f), "three XP crystals originate on opened hex");
        double previewTime = EditorApplication.timeSinceStartup + .25;
        while (EditorApplication.timeSinceStartup < previewTime) yield return null;
        ScreenCapture.CaptureScreenshot("Temp/PortalExperienceFlight.png");
        double screenshotTime = EditorApplication.timeSinceStartup + .1;
        while (EditorApplication.timeSinceStartup < screenshotTime) yield return null;
        var target = new GameObject("Portal Upgrade Validation Target"); target.tag = "Enemy1";
        target.transform.position = new Vector3(100, 100); target.AddComponent<BoxCollider2D>(); var health = AddHealth(target);
        health.TakeDamage(10000, (DamageType)0); Check(health.IsDead && p.Experience == xp + 1, "enemy death awards one XP");
        Check(Object.FindObjectsByType<PortalExperienceCrystal>(FindObjectsSortMode.None).Count(c => Vector3.Distance(c.transform.position, target.transform.position) < .01f) == 1, "one XP crystal originates at dead enemy");
        health.TakeDamage(10000, (DamageType)0); Check(p.Experience == xp + 1, "death cannot award twice");
        var friendly = new GameObject("Portal Upgrade Validation Friendly"); var friendlyHealth = AddHealth(friendly);
        friendlyHealth.TakeDamage(10000, (DamageType)0); Check(p.Experience == xp + 1, "friendly death awards no XP");
        p.AddExperience(p.RequiredExperience - p.Experience);
        Check(p.Level == 2 && p.LevelPoints == 1 && Time.timeScale == 0 && view.gameObject.activeInHierarchy, "level pauses and opens modal");
        Check(view.transform.Find("Window/Close") == null, "no close control");
        Check(GameSpeedControls.SetSimulationSpeed(4) == 0, "speed cannot bypass mandatory choice");
        Check(EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(view.transform), "keyboard focus stays in upgrade window");
        var outside = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b => b.name == "Settings");
        Check(!outside.IsInteractable(), "other buttons cannot receive keyboard submit");
        yield return null; // Let the newly enabled nested Canvas enter the raycast registry.
        Canvas.ForceUpdateCanvases();
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width / 2, 30) }, hits);
        Check(hits.Count > 0 && hits[0].gameObject == view.gameObject, "overlay blocks bottom controls: " + string.Join(", ", hits.Select(h => h.gameObject.name)));
        double flightEnd = EditorApplication.timeSinceStartup + 1.3;
        while (EditorApplication.timeSinceStartup < flightEnd) yield return null;
        Check(Time.timeScale == 0 && Object.FindObjectsByType<PortalExperienceCrystal>(FindObjectsSortMode.None).Length == 0, "crystal flights finish and clean up during level-up pause");
        var resource = Resources.LoadAll<ResourceType>("").First(x => x.name == "Wood");
        int wood = GlobalResourceManager.Instance.GetResourceAmount(resource);
        view.content.GetComponentsInChildren<LaboratoryUpgradeRow>().First(r => r.groupId == "attack").purchaseButton.onClick.Invoke();
        Check(p.CanAttack && Time.timeScale == 1 && !view.gameObject.activeSelf, "real UI click unlocks attack and resumes");
        Check(GlobalResourceManager.Instance.GetResourceAmount(resource) == wood, "level point purchase spends no resources");
        lights.ActivateAvailableLights(); yield return null;
        var firstBeam = Object.FindObjectsByType<CrystalLightBeam>(FindObjectsSortMode.None).First(b => b.MarkerVisible);
        Vector2 firstPosition = firstBeam.MarkerPosition;
        p.AddExperience(p.RequiredExperience - p.Experience); Check(p.Purchase("lights"), "buy extra beam");
        Check(lights.LightCount == 2 && Object.FindObjectsByType<CrystalLightBeam>(FindObjectsSortMode.None).Count(b => b.MarkerVisible) == 2, "extra beam visible immediately");
        Check(firstBeam.MarkerPosition == firstPosition, "original beam position preserved");
        GameSpeedControls.SetSimulationSpeed(2);
        p.AddExperience(p.RequiredExperience + p.Balance.RequiredExperience(p.Level + 1));
        Check(p.LevelPoints == 2 && Time.timeScale == 0, "two level points queue");
        Check(p.Purchase("projectiles") && p.ProjectileCount == 2 && Time.timeScale == 0, "first point keeps modal paused");
        Check(p.Purchase("speed") && Time.timeScale == 2, "last point restores original speed");
        Check(Mathf.Approximately(tower.CurrentCooldownBase, tower.GetStats().TotalCooldown / 1.2f), "speed applies live");
        p.AddExperience(p.RequiredExperience); Check(p.Purchase("damage") && Mathf.Approximately(p.DamageMultiplier, 1.25f), "damage upgrade applies");
        tower.enabled = false;
        var dummy = new GameObject("Portal Projectile Validation Target"); dummy.tag = "Enemy1";
        dummy.transform.position = tower.transform.position + Vector3.right * 3;
        var collider = dummy.AddComponent<BoxCollider2D>(); var victim = AddHealth(dummy);
        typeof(ArcherTower).GetField("_target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(tower, dummy.transform);
        var visuals = tower.GetComponent<TowerVisuals>();
        var serialized = new SerializedObject(visuals); serialized.FindProperty("windupDuration").floatValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo();
        var previous = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
        visuals.StartShoot();
        var projectiles = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Except(previous).ToArray();
        Check(projectiles.Length == 2, "real tower volley spawns two projectiles");
        float before = victim.CurrentHealth;
        projectiles[0].SendMessage("OnTriggerEnter2D", collider);
        Check(Mathf.Approximately(before - victim.CurrentHealth, tower.GetStats().damageSettings.Sum(d => d.TotalDamage) * 1.25f), "real projectile deals upgraded damage");
        Object.Destroy(dummy); tower.enabled = true;
        for (int i = 0; i < 4; i++) { p.AddExperience(p.RequiredExperience); Check(p.Purchase("lights"), "light tier purchasable"); }
        Check(p.LightCount == 6 && lights.LightCount == 6, "six beams maximum");
        p.AddExperience(p.RequiredExperience);
        Check(!p.Purchase("lights") && p.LevelPoints == 1, "maxed upgrade spends nothing");
        Check(p.Purchase("damage") && p.Rank("damage") == 2, "repeatable damage remains available");
        p.AddExperience(3); Canvas.ForceUpdateCanvases();
        Check(bar.fill.rectTransform.rect.width > 0 && bar.fill.rectTransform.rect.width < bar.transform.GetComponent<RectTransform>().rect.width, "XP fill grows proportionally");
        var language = LocalizationService.Instance; string originalLanguage = language.Language; language.SetLanguage("en");
        Check(bar.label.text.StartsWith("Portal tower"), "XP HUD English localization"); language.SetLanguage(originalLanguage);
        Object.Destroy(target); Object.Destroy(friendly);
        SceneManager.LoadScene("World"); yield return null; yield return null;
        p = PortalTowerProgression.Instance;
        Check(p.Level == 1 && p.Experience == 0 && p.LevelPoints == 0 && !p.CanAttack && p.LightCount == 1, "next run resets all progression");
        Check(!Resources.Load<ScientificUpgradeTable>("ScientificUpgradeTable").entries.Any(e => e.id == ScientificUpgrades.PortalArrows || ScientificUpgrades.Flashlights.Contains(e.id)), "moved upgrades removed from laboratory");
    }
    static Health AddHealth(GameObject owner)
    {
        var health = owner.AddComponent<Health>();
        health.OnDeath = new UnityEngine.Events.UnityEvent();
        health.OnHealthChanged = new UnityEngine.Events.UnityEvent<float>();
        return health;
    }
}
#endif
