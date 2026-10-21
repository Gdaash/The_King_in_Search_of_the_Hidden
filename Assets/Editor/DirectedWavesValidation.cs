#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using GameFoundation.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class DirectedWavesValidation
{
    private const string ScenePath = "Assets/Editor/DirectedWavesValidationScene.unity";
    private static AlarmSystem alarm;
    private static EnemyRosterView roster;
    private static int step;
    private static double stamp, started;
    private static float frozen, restStart;
    private static ResourceRequester forestJob, mineJob;
    private static TimerController productionTimer;
    private static ResourceType humanType;
    private static readonly List<string> passed = new();
    static DirectedWavesValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DirectedWaveQA", false))
            {
                try { Create(); EditorApplication.update += Tick; }
                catch (Exception e) { Fail(e); }
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool("DirectedWaveQA", false))
            {
                var saves = typeof(GameFoundation.Saves.SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
                saves.GetField("WritesSuspended", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, false);
                saves.GetMethod("ResetCache", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
                EditorApplication.update -= Tick;
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("DirectedWavePreviousStart", ""));
                SessionState.SetBool("DirectedWaveQA", false);
                AssetDatabase.DeleteAsset(ScenePath);
            }
        };
    }
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        passed.Add(message);
    }
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode before validation.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorSceneManager.CloseScene(scene, true);
        SceneManager.SetActiveScene(previous);
        SessionState.SetString("DirectedWavePreviousStart", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetString("DirectedWaveQAResult", "RUNNING");
        SessionState.SetBool("DirectedWaveQA", true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorApplication.isPlaying = true;
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Enemies/" + name + ".prefab");
    private static void Create()
    {
        typeof(GameFoundation.Saves.SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService")
            .GetField("WritesSuspended", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, true);
        passed.Clear(); step = 0; started = stamp = EditorApplication.timeSinceStartup;
        var canvas = new GameObject("Wave QA Canvas", typeof(Canvas));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Tooltip.prefab"), canvas.transform);
        var go = new GameObject("Wave QA alarm"); go.SetActive(false); go.transform.SetParent(canvas.transform);
        alarm = go.AddComponent<AlarmSystem>();
        Set(alarm, "createRuntimeUiWhenMissing", false);
        go.SetActive(true);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Octopus t:Prefab", new[] { "Assets/Prefabs/Units" }).First(g => AssetDatabase.GUIDToAssetPath(g).EndsWith("/Octopus.prefab"))));
        var threshold = new AlarmThreshold { alarmValue = 0, waveCountdown = 1.5f, minEnemiesPerWave = 3, maxEnemiesPerWave = 3 };
        threshold.enemies.Add(new AlarmEnemy { prefab = prefab });
        threshold.waves.Add(new AlarmWaveComposition { name = "QA first", units = new List<AlarmWaveUnit> { new AlarmWaveUnit { prefab = prefab, count = 3 } } });
        threshold.waves.Add(new AlarmWaveComposition { name = "QA second", units = new List<AlarmWaveUnit> { new AlarmWaveUnit { prefab = prefab, count = 4 } } });
        Set(alarm, "thresholds", new List<AlarmThreshold> { threshold });
        var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HUD/Enemy Roster.prefab"), canvas.transform);
        roster = panel.GetComponent<EnemyRosterView>(); Set(roster, "alarm", alarm);
        for (int i = 0; i < 16; i++)
        for (int ring = 1; ring <= 2; ring++)
        {
            var hexGo = new GameObject("QA blocked hex");
            float a = i * Mathf.PI / 8;
            hexGo.transform.position = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * (ring == 1 ? 3 : 6);
            var hex = hexGo.AddComponent<HexBlocker>(); hex.enabled = false;
            HexBlocker.ActiveInstances.Add(hex);
        }
        GameSpeedControls.SetSimulationSpeed(1);
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || alarm == null) return;
        try
        {
            if (EditorApplication.timeSinceStartup - started > 35) throw new Exception("Validation timed out at step " + step);
            var type = alarm.CurrentWaveEnemyTypes.FirstOrDefault();
            if (step == 0 && alarm.CurrentWaveStage == AlarmSystem.WaveStage.Countdown)
            {
                Check(type != null && alarm.GetWaveEnemyCount(type) == 3, "Planned composition visible before spawning");
                Check(roster.GetComponent<CanvasGroup>().alpha == 1, "Roster appears during countdown");
                frozen = alarm.WaveSecondsRemaining; Time.timeScale = 0; stamp = EditorApplication.timeSinceStartup; step++;
            }
            else if (step == 1 && EditorApplication.timeSinceStartup - stamp > .25)
            {
                Check(Mathf.Abs(alarm.WaveSecondsRemaining - frozen) < .01f, "Pause freezes preparation");
                var timer = roster.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t => t.name == "Subtitle");
                var title = roster.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t => t.name == "Title");
                Check(timer.fontSize == 27 && timer.fontSize > title.fontSize, "Countdown font reduced by 25 percent to 27");
                Check(timer.color.r > .98f && timer.color.g > .98f, "Countdown starts white");
                Set(alarm, "<WaveSecondsRemaining>k__BackingField", 6f); roster.SendMessage("Update");
                Check(!timer.text.Contains("<size="), "Countdown digits do not pulse before final five seconds");
                Set(alarm, "<WaveSecondsRemaining>k__BackingField", 4.75f); roster.SendMessage("Update");
                Check(timer.text.Contains("<size=") && timer.text.EndsWith("</size>"), "Only countdown digits pulse during final five seconds");
                string pausedPulse = timer.text; roster.SendMessage("Update");
                Check(timer.text == pausedPulse, "Countdown pulse freezes with game time");
                Set(alarm, "<WaveSecondsRemaining>k__BackingField", frozen); roster.SendMessage("Update");
                CaptureCountdown();
                foreach (var hex in HexBlocker.ActiveInstances.ToArray()) if (hex.transform.position.magnitude < 4) hex.gameObject.SetActive(false);
                Time.timeScale = 1; stamp = EditorApplication.timeSinceStartup; step++;
            }
            else if (step == 2 && EditorApplication.timeSinceStartup - stamp > .35)
            {
                var timer = roster.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t => t.name == "Subtitle");
                Check(timer.color.g < .99f && timer.color.r > timer.color.g, "Countdown gradually turns muted red");
                CaptureCountdown("warning");
                Check(Object.FindObjectsByType<WaveSpawnTelegraph>(FindObjectsSortMode.None).All(v => v.transform.position.magnitude > 5), "Spawn marker moves outward after opening frontier");
                Time.timeScale = 6; step++;
            }
            else if (step == 3 && alarm.CurrentWaveStage == AlarmSystem.WaveStage.Attacking)
            {
                var timer = roster.GetComponentsInChildren<UnityEngine.UI.Text>(true).First(t => t.name == "Subtitle");
                if (timer.text != "Атака") return;
                Check(Mathf.Abs(timer.color.r - .82f) < .01f && Mathf.Abs(timer.color.g - .43f) < .01f, "Attack label uses panel-compatible muted red");
                CaptureCountdown("attacking");
                var enemies = Object.FindObjectsByType<AlarmSpawnedEnemy>(FindObjectsSortMode.None);
                if (enemies.Length < 3) return;
                Check(enemies.Length == 3, "Whole cluster spawned");
                enemies[0].GetComponent<Health>().TakeDamage(100000, DamageType.Physical);
                Check(alarm.GetWaveEnemyCount(type) == 2, "Kill decreases wave roster immediately");
                foreach (var enemy in enemies.Skip(1)) enemy.GetComponent<Health>().TakeDamage(100000, DamageType.Physical);
                step++;
            }
            else if (step == 4 && alarm.CurrentWaveStage == AlarmSystem.WaveStage.Rest)
            {
                Check(roster.GetComponent<CanvasGroup>().alpha == 0, "Completed wave hides panel");
                restStart = Time.time; step++;
            }
            else if (step == 5 && alarm.CurrentWaveStage == AlarmSystem.WaveStage.Countdown)
            {
                Check(Time.time - restStart >= 4.8f, "Five game seconds of silence before next countdown");
                Check(alarm.GetWaveEnemyCount(type) == 4, "Next authored composition selected");
                Check(alarm.WaveNumber == 2, "One sequential wave scheduler");
                TestProvokeWave();
                TestCivilians();
                step++;
            }
            else if (step == 6 && alarm.CurrentWaveStage == AlarmSystem.WaveStage.Attacking)
            {
                Check(alarm.GetAliveEnemyCount(type) == 4, "Provoke starts the announced enemy wave without waiting");
                alarm.enabled = false;
                CreateWorkforce(); stamp = EditorApplication.timeSinceStartup; step++;
            }
            else if (step == 7 && EditorApplication.timeSinceStartup - stamp > .7)
            {
                var worker = Object.FindAnyObjectByType<ResourceWorker>();
                Check(worker != null && forestJob.ResidentHumanCount == 1, "Delivered resident has physical worker representation without extra population");
                Check(mineJob.GetComponent<Health>() != null && WorldCivilianTarget.Contains(mineJob.gameObject), "Mine has health and is a hostile target");
                productionTimer.SetDurationAndStart(1000);
                worker.GetComponent<Health>().TakeDamage(1, DamageType.Physical);
                Check(forestJob.ResidentHumanCount == 0 && productionTimer.IsPaused, "Worker hit removes staffing and pauses production");
                forestJob.DeliverResource(humanType);
                Check(forestJob.ResidentHumanCount == 1 && !productionTimer.IsPaused, "Replacement worker resumes interrupted production");
                int returning = Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None).Length;
                mineJob.GetComponent<Health>().TakeDamage(10000, DamageType.Physical);
                Check(mineJob.ResidentHumanCount == 0 && Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None).Length > returning, "Destroyed mine releases resident toward portal");
                TestWaveRelocation();
                TestStartingLights();
                EquipmentUpgradeValidation.Run(roster.GetComponentInParent<Canvas>().transform);
                RangedStoppingValidation.Run();
                CartRecoveryValidation.Run();
                TutorialCarrierValidation.Run();
                UnfinishedRunValidation.Run();
                SessionState.SetString("DirectedWaveQAResult", "PASS: " + string.Join("; ", passed));
                Debug.Log(SessionState.GetString("DirectedWaveQAResult", ""));
                EditorApplication.update -= Tick; Time.timeScale = 1; EditorApplication.isPlaying = false;
            }
        }
        catch (Exception e) { Fail(e); }
    }
    // Change only the cached envelope while all persistent writes remain suspended.
    private static void TestWaveRelocation()
    {
        var type=typeof(AlarmSystem);
        var create=type.GetMethod("CreateDirectedGroups",BindingFlags.Instance|BindingFlags.NonPublic);
        var groups=(System.Collections.IList)type.GetField("directedGroups",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(alarm);
        var previous=(List<Vector3>)type.GetField("previousSpawnPoints",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(alarm);
        Vector3 Position()=> (Vector3)groups[0].GetType().GetField("position").GetValue(groups[0]);
        var random=UnityEngine.Random.state;
        Check(Mathf.Approximately((float)type.GetField("waveRelocationChance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(alarm),.4f),"Wave relocation defaults to forty percent");
        try
        {
            Set(alarm,"waveRelocationChance",0f);create.Invoke(alarm,new object[]{1});var first=Position();
            previous.Clear();previous.Add(first);create.Invoke(alarm,new object[]{1});
            Check((Position()-first).sqrMagnitude<.01f,"No relocation roll retains the spawn while its hex stays closed");
            Set(alarm,"waveRelocationChance",1f);
            for(int i=0;i<3;i++)
            {
                first=Position();previous.Clear();previous.Add(first);create.Invoke(alarm,new object[]{1});
                Check((Position()-first).sqrMagnitude>.01f,"Successful relocation chooses another point without opening the old hex");
            }
        }
        finally
        {
            UnityEngine.Random.state=random;Set(alarm,"waveRelocationChance",.4f);
            type.GetMethod("ClearDirectedGroups",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(alarm,null);
        }
    }
    private static void SetMemoryPurchase(string key, int value)
    {
        GameFoundation.Saves.SaveSlotPrefs.GetInt(key, 0);
        var service = typeof(GameFoundation.Saves.SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
        var entry = service.GetMethod("Find", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { GameFoundation.Saves.SaveSlotPrefs.SelectedSlot, key });
        entry.GetType().GetField("value").SetValue(entry, value.ToString());
    }
    private static void TestStartingLights()
    {
        var stats = ScriptableObject.CreateInstance<GlobalStats>();
        var table = ScriptableObject.CreateInstance<ScientificUpgradeTable>();
        for (int i = 0; i < ScientificUpgrades.Flashlights.Length; i++)
        {
            var id = ScientificUpgrades.Flashlights[i];
            SetMemoryPurchase(id + "_Purchased", 0);
            SetMemoryPurchase("Flashlight" + (i + 2) + "_Purchased", 0);
            table.entries.Add(new ScientificUpgradeTable.Entry { id = id, groupId = ScientificUpgrades.StartingLights, level = i + 1 });
        }
        Set(stats, "scientificUpgradeTable", table);
        var progression = GameFoundation.MetaProgression.PortalTowerProgression.Instance;
        Set(progression, "scientificStats", stats);
        var balance = ScriptableObject.CreateInstance<GameFoundation.MetaProgression.PortalTowerBalance>();
        var extra = new GameFoundation.MetaProgression.PortalTowerBalance.Upgrade {
            id = "qa.extra_light", effect = GameFoundation.MetaProgression.PortalTowerBalance.Effect.ExtraLight,
            scope = GameFoundation.MetaProgression.PortalTowerBalance.UpgradeScope.Portal, value = 1, maximumRank = 5 };
        balance.upgrades.Add(extra);
        Set(progression, "balance", balance); Set(progression, "modifiers", null);
        var ranks = (Dictionary<string, int>)typeof(GameFoundation.MetaProgression.PortalTowerProgression)
            .GetField("ranks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(progression);
        Check(stats.StartingFlashlightCount == 1 && !stats.IsUpgradeUnlocked(table.entries[1]), "Starting lights begin at one and laboratory levels require their predecessor");
        for (int level = 0; level <= 5; level++)
        {
            if (level > 0) { Check(stats.CanPurchaseUpgrade(table.entries[level - 1]), "Laboratory starting-light level " + level + " is purchasable"); SetMemoryPurchase(table.entries[level - 1].id + "_Purchased", 1); }
            Check(stats.StartingFlashlightCount == level + 1, "Permanent research starts expedition with " + (level + 1) + " lights");
            for (int battle = 0; battle <= 5; battle++)
            {
                ranks[extra.id] = battle;
                int expected = Mathf.Min(6, level + 1 + battle);
                if (progression.LightCount != expected || stats.AvailableFlashlightCount != expected || progression.IsComplete(extra) != (expected == 6))
                    throw new Exception("Starting and combat light upgrades do not stack or cap correctly");
            }
        }
        Check(true, "All 36 permanent/combat combinations respect six-light cap and suppress extra-light cards at cap");
    }
    private static void TestProvokeWave()
    {
        Check(AlarmSystem.ProvokeRewardForSeconds(30) == 3 && AlarmSystem.ProvokeRewardForSeconds(29.99f) == 2 &&
            AlarmSystem.ProvokeRewardForSeconds(20) == 2 && AlarmSystem.ProvokeRewardForSeconds(10) == 1 &&
            AlarmSystem.ProvokeRewardForSeconds(9.99f) == 0, "Provoke reward counts only full ten-second blocks");
        var progression = new GameObject("QA crystal progression").AddComponent<GameFoundation.MetaProgression.PortalTowerProgression>();
        progression.enabled = false;
        var balance = ScriptableObject.CreateInstance<GameFoundation.MetaProgression.PortalTowerBalance>(); balance.firstLevelExperience = 100;
        Set(progression, "balance", balance); Set(progression, "<CrystalTechnologyAvailable>k__BackingField", true);
        Set(progression, "<CrystalDebt>k__BackingField", 1);
        Set(alarm, "<WavePreparationDuration>k__BackingField", 30f);
        Set(alarm, "<WaveSecondsRemaining>k__BackingField", 9.99f);
        var button = roster.GetComponentInChildren<WaveProvokeButton>(true);
        Check(button != null, "Provoke button is wired in the enemy roster prefab");
        button.Bind(alarm);
        Check(!button.gameObject.activeSelf && !button.Button.interactable && button.RewardLabel.text == "+0" && !alarm.TryProvokeWave(), "Provoke disabled below ten seconds with zero reward");
        Set(alarm, "<WaveSecondsRemaining>k__BackingField", 25f); button.Bind(alarm);
        Check(button.gameObject.activeSelf && button.Button.interactable && button.RewardLabel.text == "+2", "Provoke displays the current crystal reward");
        var icon = button.GetComponentsInChildren<UnityEngine.UI.Image>(true).First(i => i.name == "Crystal");
        Check(icon.sprite != null && Vector2.Distance(icon.rectTransform.sizeDelta, icon.sprite.rect.size * 2) < .01f, "Crystal reward icon uses native 2x Canvas size");
        var tooltip = button.GetComponent<ResourceTooltipTrigger>();
        tooltip.OnPointerEnter(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        Check(TooltipManager.Instance.GetComponentInChildren<UnityEngine.UI.Text>(true).text.Contains("Мгновенно"), "Provoke hover explains instant attack and crystal reward");
        tooltip.OnPointerExit(null);
        roster.SendMessage("Update");
        CaptureCountdown("provoke");
        button.Button.onClick.Invoke();
        Check(!button.gameObject.activeSelf, "Provoke button hides immediately after pressing");
        Check(alarm.WaveSecondsRemaining == 0 && progression.CrystalDebt == 0 && progression.Experience == 1, "Provoke claims countdown and grants experience through crystal debt repayment");
        Check(!alarm.TryProvokeWave() && progression.Experience == 1, "Repeated provoke cannot duplicate reward");
    }
    private static void CreateWorkforce()
    {
        humanType = ScriptableObject.CreateInstance<ResourceType>(); humanType.isHumanResource = true;
        var output = new GameObject("QA production output", typeof(ResourceItem)); output.SetActive(false);
        forestJob = new GameObject("QA working forest", typeof(BoxCollider2D)).AddComponent<ResourceRequester>();
        forestJob.transform.position = new Vector3(20, 20, 0);
        forestJob.requirements.Add(new ResourceRequirement { resourceType = humanType, requiredAmount = 1, currentAmount = 1 });
        Set(forestJob, "outputResources", new List<ResourceOutput> { new ResourceOutput { prefab = output } });
        Set(forestJob, "ignoreFlag", true); Set(forestJob, "_isProcessing", true);
        productionTimer = forestJob.gameObject.AddComponent<TimerController>(); Set(productionTimer, "runOnStart", false);
        mineJob = new GameObject("QA working mine", typeof(BoxCollider2D)).AddComponent<ResourceRequester>();
        mineJob.transform.position = new Vector3(25, 20, 0);
        mineJob.requirements.Add(new ResourceRequirement { resourceType = humanType, requiredAmount = 1, currentAmount = 1 });
        Set(mineJob, "retainHumansBetweenCycles", true);
        Set(mineJob, "outputResources", new List<ResourceOutput> { new ResourceOutput { prefab = output } });
    }
    private static void TestCivilians()
    {
        var warehouse = new GameObject("QA portal").AddComponent<Warehouse>();
        Set(warehouse, "humanPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Human.prefab"));
        warehouse.transform.position = new Vector3(-10, 0, 0);
        var item = new GameObject("QA resource").AddComponent<ResourceItem>(); item.isReserved = true;
        var porterGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Porter.prefab"));
        var porter = porterGo.GetComponent<Porter>();
        var flag = new GameObject("QA light", typeof(CircleCollider2D), typeof(LogisticFlag));
        Check(item.IsIlluminated, "Light enables resource gathering");
        porter.AssignWarehouseGathering(item); item.isReserved = true;
        porter.GetComponent<Health>().TakeDamage(1, DamageType.Physical);
        Check(!item.isReserved && porter.IsReturningToWarehouse(), "Porter hit releases resource reservation and retreats");
        Object.DestroyImmediate(flag);
        Check(!item.IsIlluminated, "Unlit resource is unavailable");
        var humanGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Human.prefab"));
        var human = humanGo.GetComponent<HumanUnit>(); human.GetComponent<Health>().TakeDamage(1, DamageType.Physical);
        Check(human.IsReturningToWarehouse() && !human.CanBeReassigned, "Resident hit forces return without reassignment");
        Check(CombatTargetRegistry.ClosestHostileTarget(human.transform.position, 10) != null, "Hostiles detect civilians");
    }
    private static void CaptureCountdown(string name = "countdown")
    {
        var canvas = roster.GetComponentInParent<Canvas>();
        var camera = new GameObject("Wave QA capture camera").AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 6.5f; camera.transform.position = new Vector3(0, 0, -10);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .035f, .07f);
        var rt = new RenderTexture(1920, 1080, 24);
        var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
            System.IO.Directory.CreateDirectory("Temp/DirectedWavesQA");
            System.IO.File.WriteAllBytes("Temp/DirectedWavesQA/" + name + ".png", image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
            camera.targetTexture = null; Object.DestroyImmediate(rt); Object.DestroyImmediate(image); Object.DestroyImmediate(camera.gameObject);
        }
    }
    private static void Fail(Exception exception)
    {
        SessionState.SetString("DirectedWaveQAResult", "FAIL: " + exception);
        Debug.LogException(exception);
        EditorApplication.update -= Tick; Time.timeScale = 1; EditorApplication.isPlaying = false;
    }
}
#endif
