#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Exercises the real World prefab, pointer adapter and light/logistics lifecycle on a backed-up save.</summary>
[InitializeOnLoad]
public static class LightDraggingValidation
{
    const string Key = "LightDraggingValidation";
    const string Report = "Temp/LightDraggingValidation.txt";
    static IEnumerator routine;
    static readonly List<string> report = new();
    static readonly List<string> errors = new();
    static WorldFlashlightAvailability crystal;
    static CrystalLightBeam[] beams;
    static MethodInfo pointer;
    static LightDraggingValidation() { EditorApplication.playModeStateChanged += Changed; }

    [MenuItem("Tools/Validation/Light Dragging in World")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before the audit.");
        Directory.CreateDirectory("Temp");
        // Play Mode restores the user's open scene; keep an extra copy of any unsaved edits as well.
        if (SceneManager.GetActiveScene().isDirty)
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Temp/LightDraggingEditorScene.unity", true);
        SessionState.SetString(Key + ".scene", SceneManager.GetActiveScene().path);
        SessionState.SetString(Key + ".start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/World.unity");
        SessionState.SetBool(Key, true);
        Directory.CreateDirectory("Temp"); File.WriteAllText(Report, "Starting World light dragging audit\n");
        BuildingUpgradeValidation.Begin();
        foreach (string id in ScientificUpgrades.Flashlights) SaveSlotPrefs.SetInt(id + "_Purchased", 1);
        SaveSlotPrefs.SetInt("Global_Resource_MagicOre", 20);
        SaveSlotPrefs.SetInt("Global_Resource_Wagon", 2);
        SaveSlotPrefs.Save();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Application.runInBackground = true;
            report.Clear(); errors.Clear(); Application.logMessageReceived += Log;
            routine = Checks(); EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + ".start", ""));
            BuildingUpgradeValidation.Restore();
            SessionState.SetBool(Key, false);
        }
    }
    static void Log(string text, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(text); }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        report.Add("PASS " + message); File.WriteAllLines(Report, report);
    }
    static void Tick()
    {
        try { if (routine.MoveNext()) return; report.Add("COMPLETE " + report.Count + " checks"); }
        catch (Exception e)
        {
            report.Add("FAIL " + e);
            if (crystal != null)
            {
                report.Add("State: escaped=" + crystal.Escaped + " activated=" + crystal.LightsActivated + " dragging=" + crystal.IsDragging + " speed=" + GameSpeedControls.SimulationSpeed);
                for (int i = 0; i < crystal.LightCount; i++)
                    report.Add("Light " + i + ": " + crystal.State(i) + " visible=" + beams[i].MarkerVisible + " position=" + beams[i].MarkerPosition);
            }
        }
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        foreach (var error in errors) report.Add("RUNTIME ERROR " + error);
        File.WriteAllLines(Report, report);
        EditorApplication.ExitPlaymode();
    }
    static IEnumerator Wait(float seconds)
    {
        double end = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < end) yield return null;
    }
    static void Pointer(Vector2 screen, bool down, bool held, bool up) => pointer.Invoke(crystal, new object[] { screen, down, held, up });
    static Vector2 ScreenPoint(Vector2 world) => Camera.main.WorldToScreenPoint(world);
    static bool Place(int index, Vector2 point)
    {
        if (!crystal.BeginDrag(index)) return false;
        crystal.DragTo(point);
        return crystal.DropAt(point);
    }
    static ResourceRequester Spawn(string path, Vector2 position)
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), position, Quaternion.identity);
        return root.GetComponentsInChildren<ResourceRequester>().First(x => x.requirements.Count > 0);
    }
    static void Feed(ResourceRequester requester)
    {
        foreach (var requirement in requester.requirements)
            while (requirement.currentAmount < requirement.requiredAmount) requester.DeliverResource(requirement.resourceType);
    }

    static IEnumerator Checks()
    {
        var wait = Wait(1); while (wait.MoveNext()) yield return null;
        crystal = Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        Check(crystal != null && crystal.LightCount == 6, "World loads six upgraded lights");
        CheckPausedAlarmPreview();
        // This long logistics/input audit opens enough hexes to summon real waves.
        // Protect only the test instance so combat cannot end the expedition halfway through it.
        var escapeView = Object.FindFirstObjectByType<WorldEscapeController>();
        var portalHealth = (Health)new SerializedObject(escapeView).FindProperty("portalTowerHealth").objectReferenceValue;
        var testStats = Object.Instantiate(portalHealth.Stats);
        testStats.baseMaxHealth = 1000000;
        var healthConfig = new SerializedObject(portalHealth);
        healthConfig.FindProperty("stats").objectReferenceValue = testStats;
        healthConfig.ApplyModifiedPropertiesWithoutUndo();
        portalHealth.SetNormalizedHealth(1);
        var roots = new SerializedObject(crystal).FindProperty("flashlights");
        beams = Enumerable.Range(0, roots.arraySize).Select(i => ((GameObject)roots.GetArrayElementAtIndex(i).objectReferenceValue).GetComponent<CrystalLightBeam>()).ToArray();
        pointer = typeof(WorldFlashlightAvailability).GetMethod("ProcessDragPointer", BindingFlags.Instance | BindingFlags.NonPublic);
        var autoToggle = (Toggle)new SerializedObject(crystal).FindProperty("autoRepeatToggle").objectReferenceValue;
        Check(autoToggle != null && autoToggle.isOn && crystal.AutoRepeat, "World has an enabled auto-repeat checkbox wired to its lights");
        autoToggle.isOn = false;
        Check(!crystal.AutoRepeat, "checkbox disables auto-repeat");
        Check(!crystal.LightsActivated && beams.All(b => !b.MarkerVisible), "all beams wait for activation");
        Check(beams.Count(b => b.ActivationButton.gameObject.activeInHierarchy) == 1, "exactly one restored activation button visible");
        Check(!crystal.BeginDrag(0), "no dragging before activation");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-dragging-before.png"));
        yield return null; yield return null;
        beams[0].ActivationButton.Activate(); beams[0].ActivationButton.Activate();
        wait = Wait(.9f); while (wait.MoveNext()) yield return null;
        Check(crystal.LightsActivated && beams.All(b => b.MarkerVisible), "activation reveals every available beam once");
        Check(crystal.State(0) == WorldFlashlightAvailability.LightState.Working,
            "activation immediately starts the first illuminated hex even with auto-repeat disabled");
        GameSpeedControls.SetSimulationSpeed(4);
        wait = Wait(3); while (wait.MoveNext()) yield return null;
        GameSpeedControls.SetSimulationSpeed(1);
        Check(beams.All(b => ((SpriteRenderer)new SerializedObject(b.Flag).FindProperty("idleRenderer").objectReferenceValue).sprite != null),
            "every free draggable marker has its original visible sprite");
        Check(beams.All(b => !b.ActivationButton.gameObject.activeInHierarchy), "activation button hides after use");
        Check(Enumerable.Range(0, 6).All(i => crystal.State(i) == WorldFlashlightAvailability.LightState.Ready), "initial actions finish once when auto-repeat is off");
        Check(!Place(0, Vector2.zero), "free light can be parked on empty portal ground");
        CheckEdgeDragging(0, false);
        var hexes = HexLightUnlocker.ActiveInstances.Where(h => crystal.GetHoverTarget(h.transform.position).State == WorldFlashlightAvailability.HoverState.Available)
            .Where(h => Camera.main.pixelRect.Contains(ScreenPoint(h.transform.position)) && !crystal.IsPointerOverBlockingUI(ScreenPoint(h.transform.position)))
            .OrderBy(h => h.transform.position.sqrMagnitude).Take(3).ToArray();
        Check(hexes.Length >= 2, "visible actionable World hexes exist");
        Vector2 target = hexes[0].transform.position;
        var alarm = AlarmSystem.Instance;
        float expectedOpeningAlarm = crystal.GetActionAlarm(crystal.GetHoverTarget(target));
        var expectedRed = Enumerable.Range(0, alarm.PreviewOrbCount(expectedOpeningAlarm))
            .Select(i => alarm.PreviewOrbRaisesLevel(expectedOpeningAlarm, i)).ToArray();
        Pointer(ScreenPoint(target), true, true, false); Pointer(ScreenPoint(target), false, false, true);
        Check(!hexes[0].IsUnlocking() && crystal.State(0) == WorldFlashlightAvailability.LightState.Ready, "clicking a hex does not assign or spend a beam");
        CheckManualHint(target, false);
        Physics2D.SyncTransforms();
        Vector2 origin = beams[0].MarkerPosition;
        Check(!crystal.IsPointerOverBlockingUI(ScreenPoint(origin)), "first marker is reachable outside HUD");
        Pointer(ScreenPoint(origin), true, true, false);
        Pointer(ScreenPoint(origin) + Vector2.one, false, false, true);
        Check(!crystal.IsDragging && crystal.State(0) == WorldFlashlightAvailability.LightState.Ready, "tiny pointer movement is not a drag");
        Pointer(ScreenPoint(origin), true, true, false);
        Pointer(ScreenPoint(target), false, true, false);
        Check(crystal.IsDragging && Vector2.Distance(beams[0].MarkerPosition, target) < .01f && !hexes[0].IsUnlocking(), "pointer moves marker and beam without starting action on pass-over");
        Pointer(ScreenPoint(target), false, false, true);
        Check(!crystal.IsDragging && hexes[0].IsUnlocking() && crystal.State(0) == WorldFlashlightAvailability.LightState.Working, "drop starts opening with exactly the dragged beam");
        CheckEdgeDragging(0, true);
        CheckWorkingAlarm(target, expectedOpeningAlarm, expectedRed);
        var openingHover = CaptureOpeningHover(target); while (openingHover.MoveNext()) yield return null;
        CheckManualHint(target, false);
        var light = beams[0].GetComponentInChildren<FlashlightController>(true).transform;
        Check(Vector2.Dot(light.up, (target - (Vector2)light.position).normalized) > .999f, "light points at marker on its first visible frame");
        wait = Wait(.35f); while (wait.MoveNext()) yield return null;
        float pausedOpening = hexes[0].TimeRemaining;
        float openingActiveForecast = alarm.ActiveReservedActionAlarm;
        float openingOwnForecast = crystal.GetActionAlarm(crystal.GetHoverTarget(target));
        Check(crystal.BeginDrag(0), "working opening beam can be moved");
        crystal.DragTo(Vector2.zero); crystal.DropAt(Vector2.zero);
        wait = Wait(.35f); while (wait.MoveNext()) yield return null;
        Check(hexes[0].IsPaused && Mathf.Approximately(pausedOpening, hexes[0].TimeRemaining), "unlit hex preserves opening progress");
        Check(Mathf.Approximately(alarm.ActiveReservedActionAlarm, openingActiveForecast - openingOwnForecast),
            "paused hex opening is excluded from the red-skull forecast");
        var openingBar = (RadialProgressBar)new SerializedObject(hexes[0]).FindProperty("progressBar").objectReferenceValue;
        Check(openingBar.GetComponent<CanvasGroup>().alpha == 1, "paused opening keeps its progress bar visible");
        Check(Place(0, target) && !hexes[0].IsPaused && Mathf.Approximately(pausedOpening, hexes[0].TimeRemaining), "returning light resumes opening without reset");
        Check(Mathf.Approximately(alarm.ActiveReservedActionAlarm, openingActiveForecast),
            "resumed hex opening returns to the forecast exactly once");
        Check(Center(beams[0]).forceRenderingOff, "resuming opening hides the central sprite immediately");
        Check(crystal.BeginDrag(0), "active opening can be picked up again");
        crystal.CancelDrag();
        Check(!hexes[0].IsPaused && crystal.State(0) == WorldFlashlightAvailability.LightState.Working, "cancelled drag restores active opening");
        Check(!Place(1, target) && crystal.State(1) == WorldFlashlightAvailability.LightState.Ready, "another beam cannot duplicate the same action");
        Check(Place(1, hexes[1].transform.position), "two beams open independent hexes");
        GameSpeedControls.SetSimulationSpeed(0);
        float remaining = hexes[0].TimeRemaining;
        wait = Wait(.25f); while (wait.MoveNext()) yield return null;
        Check(Mathf.Approximately(remaining, hexes[0].TimeRemaining) && crystal.State(0) == WorldFlashlightAvailability.LightState.Working && !crystal.BeginDrag(2), "pause freezes opening and dragging");
        GameSpeedControls.SetSimulationSpeed(4);
        wait = Wait(3); while (wait.MoveNext()) yield return null;
        Check(hexes.Take(2).All(h => h == null || h.IsUnlocked()), "both real World hexes finish opening");
        Check(crystal.State(0) == WorldFlashlightAvailability.LightState.Ready && beams[0].MarkerVisible,
            "completed beam is immediately available with no recharge delay");
        Check(!Center(beams[0]).forceRenderingOff, "central marker returns after opening completes");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-dragging-world.png"));
        yield return null; yield return null;
        GameSpeedControls.SetSimulationSpeed(1);
        var warehouse = Warehouse.Instance;
        var orderManager = OrderManager.Instance;
        // Synthetic buildings are outside the map; do not dispatch real porters to this fixture.
        orderManager.enabled = false;
        foreach (var type in Resources.FindObjectsOfTypeAll<ResourceType>())
            if (type.isHumanResource) GlobalResourceManager.Instance.SetResourceAmount(type, 0);
        var stone = Spawn("Assets/Prefabs/Buildings/Stone 3.prefab", new Vector2(50, 0));
        var mine = Spawn("Assets/Prefabs/Buildings/Magic Ore Mine.prefab", new Vector2(55, 0));
        yield return null;
        Check(Place(0, stone.transform.position), "just-finished beam immediately accepts the next action");
        Check(crystal.RecallHumansAt(stone.transform.position) && crystal.State(0) == WorldFlashlightAvailability.LightState.Ready,
            "cancelled beam is immediately ready again");
        CheckManualHint(stone.transform.position, true);
        var hintPreview = PreviewManualHint(stone.transform.position); while (hintPreview.MoveNext()) yield return null;
        var ore = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/MagicOre.asset");
        int oreBefore = GlobalResourceManager.Instance.GetResourceAmount(ore);
        for (int pass = 0; pass < 3; pass++)
        {
            float reservedAlarm = AlarmSystem.Instance.ReservedActionAlarm;
            Check(Place(2, mine.transform.position), "reserve mine by dragging, pass " + pass);
            Check(crystal.State(2) == WorldFlashlightAvailability.LightState.WaitingForResources, "waiting reserves the dragged beam, pass " + pass);
            Check(beams[2].GetComponentsInChildren<UnityEngine.Rendering.Universal.Light2D>(true).All(l => l.gameObject.activeInHierarchy && l.enabled),
                "beam and ground light stay lit while awaiting resources, pass " + pass);
            Check(Place(2, stone.transform.position) && !mine.HasLogisticFlag(), "moving waiting beam cancels old assignment, pass " + pass);
            Check(crystal.RecallHumansAt(stone.transform.position) && !stone.HasLogisticFlag() && crystal.State(2) == WorldFlashlightAvailability.LightState.Ready, "right-click cancellation releases the beam, pass " + pass);
            Check(Mathf.Approximately(AlarmSystem.Instance.ReservedActionAlarm, reservedAlarm), "cancelled actions release pending danger, pass " + pass);
        }
        Feed(stone);
        Check(!stone.IsProcessing, "late deliveries after cancellation cannot start production");
        Check(Place(2, stone.transform.position) && stone.IsProcessing && crystal.State(2) == WorldFlashlightAvailability.LightState.Working, "reassignment starts exactly one supplied production cycle");
        CheckWorkingAlarm(stone.transform.position);
        Check(!Place(3, stone.transform.position), "second beam cannot duplicate working production");
        var pauses = PauseChecks(stone); while (pauses.MoveNext()) yield return null;
        GameSpeedControls.SetSimulationSpeed(4);
        wait = Wait(2); while (wait.MoveNext()) yield return null;
        Check(!stone.IsProcessing && !stone.HasLogisticFlag() && beams[2].MarkerVisible, "production releases ownership without auto-restarting");
        Check(crystal.State(2) == WorldFlashlightAvailability.LightState.Ready, "production has no recharge cooldown");
        CheckManualHint(stone.transform.position, true);
        Check(GlobalResourceManager.Instance.GetResourceAmount(ore) == oreBefore, "light actions and repeated cancellations never spend magic ore");
        GameSpeedControls.SetSimulationSpeed(1);
        Place(3, Vector2.zero);
        // Put a UI blocker above the whole Game view and exercise the same input adapter.
        var overlay = new GameObject("Drag audit UI", typeof(Canvas), typeof(GraphicRaycaster));
        overlay.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.GetComponent<Canvas>().sortingOrder = 999;
        var cover = new GameObject("Block", typeof(RectTransform), typeof(Image)); cover.transform.SetParent(overlay.transform, false);
        var rect = (RectTransform)cover.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        // A newly created Canvas needs its render/layout pass before it participates in raycasts.
        yield return null; yield return null;
        Canvas.ForceUpdateCanvases(); Physics2D.SyncTransforms();
        origin = beams[3].MarkerPosition;
        Check(crystal.IsPointerOverBlockingUI(ScreenPoint(origin)), "overlay raycaster covers the test marker");
        Pointer(ScreenPoint(origin), true, true, false); Pointer(ScreenPoint(origin) + new Vector2(25, 25), false, true, false); Pointer(ScreenPoint(origin), false, false, true);
        Check(!crystal.IsDragging && Vector2.Distance(origin, beams[3].MarkerPosition) < .01f, "UI consumes presses above beam markers");
        Vector2 coveredEdge = ScreenPoint(origin + new Vector2(1.7f, 0));
        Pointer(coveredEdge, true, true, false); Pointer(coveredEdge + new Vector2(25, 0), false, true, false);
        Pointer(coveredEdge, false, false, true);
        Check(!crystal.IsDragging && Vector2.Distance(origin, beams[3].MarkerPosition) < .01f, "HUD blocks dragging at the enlarged footprint edge too");
        Check(crystal.BeginDrag(3), "free beam can be picked up"); crystal.DragTo(origin + Vector2.one);
        Pointer(ScreenPoint(origin + Vector2.one), false, false, true);
        Check(!crystal.IsDragging && Vector2.Distance(origin, beams[3].MarkerPosition) < .01f, "dropping over UI restores marker without an action");
        Object.DestroyImmediate(overlay);
        Check(crystal.BeginDrag(3), "begin drag for focus test"); crystal.DragTo(origin + Vector2.one);
        typeof(WorldFlashlightAvailability).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(crystal, new object[] { false });
        Check(!crystal.IsDragging && Vector2.Distance(origin, beams[3].MarkerPosition) < .01f, "losing focus cancels drag safely");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-dragging-after.png"));
        yield return null; yield return null;
        var drains = new List<ResourceRequester>();
        for (int i = 0; i < crystal.LightCount; i++)
        {
            var drain = Spawn("Assets/Prefabs/Buildings/Stone 3.prefab", new Vector2(65 + i * 5, 0));
            drains.Add(drain); yield return null;
            Check(Place(i, drain.transform.position), "reserve every beam independently " + i);
        }
        Check(crystal.GetHoverTarget(mine.transform.position).State == WorldFlashlightAvailability.HoverState.LightsBusy,
            "all occupied beams block additional assignments");
        Check(crystal.RecallHumansAt(drains[0].transform.position) && Place(0, mine.transform.position),
            "cancelling one waiting action immediately frees its beam");
        crystal.RecallHumansAt(mine.transform.position);
        foreach (var drain in drains) { crystal.RecallHumansAt(drain.transform.position); Object.Destroy(drain.transform.root.gameObject); }
        yield return null;
        // Automatic cycles run on real building prefabs after completion callbacks have settled.
        var repeatStone = Spawn("Assets/Prefabs/Buildings/Stone 3.prefab", new Vector2(105, 0));
        yield return null;
        autoToggle.isOn = true;
        Check(crystal.AutoRepeat && Place(4, repeatStone.transform.position), "checkbox enables automatic production on the placed light");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-auto-repeat.png"));
        GameSpeedControls.SetSimulationSpeed(4);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            Check(repeatStone.HasLogisticFlag() && crystal.State(4) == WorldFlashlightAvailability.LightState.WaitingForResources,
                "next stone cycle automatically awaits deliveries " + cycle);
            Feed(repeatStone);
            Check(repeatStone.IsProcessing, "supplied automatic cycle starts " + cycle);
            wait = Wait(2.5f); while (wait.MoveNext()) yield return null;
        }
        Check(!repeatStone.CanSelectCrystalCycle && !repeatStone.IsProcessing && !repeatStone.HasLogisticFlag(),
            "exhausted three-cycle deposit stops without a fourth cycle");
        var autoBuilder = Spawn("Assets/Prefabs/Buildings/Magic Ore Mine.prefab", new Vector2(112, 0));
        var autoMine = autoBuilder.transform.root.GetComponentsInChildren<ResourceRequester>(true).Single(r => r != autoBuilder);
        yield return null;
        Check(Place(5, autoBuilder.transform.position), "light starts mine construction");
        Feed(autoBuilder);
        GameSpeedControls.SetSimulationSpeed(1);
        wait = Wait(.4f); while (wait.MoveNext()) yield return null;
        var constructionTimer = autoBuilder.GetComponentsInChildren<TimerController>(true).First(t => t.IsRunning);
        float constructionRemaining = constructionTimer.TimeRemaining;
        Check(crystal.BeginDrag(5), "construction beam can be moved before the mine is built");
        crystal.DropAt(new Vector2(112, -5));
        wait = Wait(.5f); while (wait.MoveNext()) yield return null;
        Check(autoBuilder.IsCyclePaused && !autoMine.isActiveAndEnabled && Mathf.Approximately(constructionRemaining, constructionTimer.TimeRemaining),
            "construction pauses without activating the finished mine");
        Check(Place(5, autoBuilder.transform.position) && Mathf.Approximately(constructionRemaining, constructionTimer.TimeRemaining),
            "construction resumes remaining time with retained worker and materials");
        GameSpeedControls.SetSimulationSpeed(4);
        wait = Wait(4.5f); while (wait.MoveNext()) yield return null;
        Check(autoMine.isActiveAndEnabled && autoMine.HasLogisticFlag() && crystal.State(5) == WorldFlashlightAvailability.LightState.WaitingForResources,
            "completed construction hands the same light to mining automatically");
        Check(autoMine.requirements.Single(r => r.resourceType.isHumanResource).currentAmount == 1,
            "construction retains exactly one worker for mining");
        Check(crystal.RecallHumansAt(autoMine.transform.position), "right-click cancels automatic waiting action");
        wait = Wait(.4f); while (wait.MoveNext()) yield return null;
        Check(!autoMine.HasLogisticFlag() && crystal.State(5) == WorldFlashlightAvailability.LightState.Ready,
            "cancellation stays cancelled while the global auto-repeat checkbox is on");
        CheckManualHint(autoMine.transform.position, true);
        Feed(autoMine);
        // Exercise the actual pointer adapter on the illuminated tile, away from the marker edge.
        var cameraOrigin = Camera.main.transform.position;
        Camera.main.transform.position = new Vector3(autoMine.transform.position.x, autoMine.transform.position.y, cameraOrigin.z);
        Vector2 click = ScreenPoint(autoMine.transform.position + new Vector3(1.7f, 0));
        Pointer(click, true, true, false); Pointer(click, false, false, true);
        Check(autoMine.IsProcessing && crystal.State(5) == WorldFlashlightAvailability.LightState.Working,
            "plain click at illuminated hex edge restarts work without dragging");
        var currentTimer = autoMine.GetComponentsInChildren<TimerController>(true).First(t => t.IsRunning);
        float timeBefore = currentTimer.TimeRemaining;
        Pointer(click, true, true, false); Pointer(click, false, false, true);
        Check(Mathf.Approximately(currentTimer.TimeRemaining, timeBefore), "repeated click cannot restart an active timer");
        autoToggle.isOn = false;
        wait = Wait(2.5f); while (wait.MoveNext()) yield return null;
        Check(!autoMine.IsProcessing && !autoMine.HasLogisticFlag() && crystal.State(5) == WorldFlashlightAvailability.LightState.Ready,
            "switching auto-repeat off lets the current cycle finish and stops the next");
        CheckManualHint(autoMine.transform.position, true);
        Camera.main.transform.position = cameraOrigin;
        GameSpeedControls.SetSimulationSpeed(1);
        Check(crystal.BeginDrag(3), "begin drag before escape");
        crystal.DisableAllForEscape();
        Check(crystal.Escaped && !crystal.IsDragging && beams.All(b => !b.MarkerVisible), "escape extinguishes all beams and cancels current drag");
        Check(!crystal.DropAt(target) && !crystal.BeginDrag(0), "no action after escape");
        for (int i = 0; i < ScientificUpgrades.Flashlights.Length; i++)
        {
            SaveSlotPrefs.SetInt(ScientificUpgrades.Flashlights[i] + "_Purchased", 0);
            SaveSlotPrefs.SetInt("Flashlight" + (i + 2) + "_Purchased", 0);
        }
        SceneManager.LoadScene("World");
        wait = Wait(1.5f); while (wait.MoveNext()) yield return null;
        crystal = Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        Check(crystal != null && !crystal.Escaped && !crystal.LightsActivated, "World reload resets drag and activation state");
        Check(crystal.LightCount == 1, "no upgrades expose exactly one light on a fresh run");
        Check(crystal.AutoRepeat, "new expedition defaults to auto-repeat enabled");
        crystal.ActivateAvailableLights();
        Check(Object.FindObjectsByType<CrystalLightBeam>(FindObjectsInactive.Include, FindObjectsSortMode.None).Count(b => b.MarkerVisible) == 1,
            "locked lights stay hidden after activation");
        Check(errors.Count == 0, "no runtime errors during World audit");
    }

    static void CheckPausedAlarmPreview()
    {
        var alarm = AlarmSystem.Instance;
        var preview = Object.FindObjectsByType<HexAlarmPreview>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
        var source = new GameObject("Paused alarm audit source");
        alarm.SetAlarm(20);
        alarm.ReserveActionAlarm(source, 4);
        CheckPreviewSprite(preview, true, "active 4 danger makes the next skull red at 20 + 4 + 1");
        alarm.SetActionAlarmPaused(source, true);
        CheckPreviewSprite(preview, false, "paused 4 danger no longer makes another hex's skull red");
        Check(alarm.GetReservedActionAlarm(source) == 4 && alarm.ActiveReservedActionAlarm == 0,
            "pause preserves the amount for resuming while excluding it from the forecast");
        alarm.SetActionAlarmPaused(source, false);
        alarm.SetActionAlarmPaused(source, false);
        CheckPreviewSprite(preview, true, "resuming restores the red skull without doubling danger");
        Check(alarm.ActiveReservedActionAlarm == 4, "repeated resume keeps one reservation");
        alarm.SetActionAlarmPaused(source, true);
        alarm.CancelActionAlarm(source);
        alarm.ReserveActionAlarm(source, 4);
        CheckPreviewSprite(preview, true, "a new cycle does not inherit the cancelled cycle's paused flag");
        alarm.CancelActionAlarm(source);
        alarm.SetAlarm(0);
        preview.Show(0);
        Object.DestroyImmediate(source);
    }
    static void CheckPreviewSprite(HexAlarmPreview preview, bool red, string message)
    {
        preview.Show(1);
        var image = preview.GetComponentsInChildren<Image>(true)
            .Single(i => i.transform.parent == preview.transform && i.gameObject.activeSelf);
        Check(image.sprite == (red ? AlarmSystem.Instance.SkullSprite : AlarmSystem.Instance.InactiveSkullSprite) &&
            image.color == Color.white, message);
    }

    static SpriteRenderer Center(CrystalLightBeam beam) =>
        (SpriteRenderer)new SerializedObject(beam.Flag).FindProperty("idleRenderer").objectReferenceValue;

    static void CheckEdgeDragging(int index, bool opening)
    {
        var beam = beams[index];
        var frame = beam.Flag.FrameRenderer;
        var collider = (BoxCollider2D)new SerializedObject(beam).FindProperty("markerCollider").objectReferenceValue;
        var size = collider.size;
        var origin = beam.MarkerPosition;
        var state = crystal.State(index);
        // Isolate this footprint: neighbouring decorative frames can overlap at their tips.
        // The runtime picker resolves such overlap by the nearest beam centre.
        var parked = new Dictionary<CrystalLightBeam, Vector2>();
        for (int i = 0; i < beams.Length; i++)
            if (i != index && crystal.State(i) == WorldFlashlightAvailability.LightState.Ready)
            { parked[beams[i]] = beams[i].MarkerPosition; beams[i].ShowIdle(new Vector2(200 + i * 8, 100)); }
        var edgePoints = new[] { new Vector2(-1.78f, 0), new Vector2(1.78f, 0),
            new Vector2(-.5f, 1.55f), new Vector2(.5f, 1.55f), new Vector2(-.5f, -1.53f), new Vector2(.5f, -1.53f),
            new Vector2(-1.3f, .83f), new Vector2(1.3f, .83f), new Vector2(-1.25f, -.95f), new Vector2(1.25f, -.95f) };
        Physics2D.SyncTransforms();
        Check(!beam.ContainsMarkerPoint(frame.transform.TransformPoint(new Vector2(1.8f, 1.5f))),
            "transparent square corner outside the hex does not grab the beam");
        foreach (var edge in edgePoints)
        {
            Vector2 point = frame.transform.TransformPoint(edge);
            Check(!collider.OverlapPoint(point) && beam.ContainsMarkerPoint(point),
                "frame edge is selectable beyond the delivery collider: " + edge);
            Vector2 screen = ScreenPoint(point);
            Check(Camera.main.pixelRect.Contains(screen) && !crystal.IsPointerOverBlockingUI(screen),
                "frame edge is not blocked by the working progress bar or hover UI: " + edge);
            Pointer(screen, true, true, false);
            Vector2 moved = screen + new Vector2(18, 10);
            Pointer(moved, false, true, false);
            Vector2 delta = (Vector2)Camera.main.ScreenToWorldPoint(moved) - point;
            Check(crystal.IsDragging && Vector2.Distance(beam.MarkerPosition, origin + delta) < .02f,
                "mouse drag starts at frame edge without snapping marker to cursor: " + edge);
            Check(!Center(beam).forceRenderingOff, "dragging restores central marker");
            crystal.CancelDrag();
            Check(!crystal.IsDragging && crystal.State(index) == state && Vector2.Distance(beam.MarkerPosition, origin) < .01f,
                "edge drag cancellation restores original beam and action");
            Check(Center(beam).forceRenderingOff == opening, "central marker visibility matches opening state");
        }
        Check(collider.size == size && size == new Vector2(1.75f, 1.75f), "delivery collider retains its original size");
        foreach (var other in parked) other.Key.ShowIdle(other.Value);
    }

    static void CheckWorkingAlarm(Vector2 point, float expectedAmount = -1, bool[] expectedRed = null)
    {
        var hover = Object.FindFirstObjectByType<CrystalHexHover>();
        hover.enabled = false;
        var target = crystal.GetHoverTarget(point);
        float amount = crystal.GetActionAlarm(target, out float reserved);
        Check(target.State == WorldFlashlightAvailability.HoverState.Working && amount > 0 && Mathf.Approximately(amount, reserved),
            "working action exposes its remaining reserved danger");
        if (expectedAmount >= 0) Check(Mathf.Approximately(amount, expectedAmount), "opening preserves the previewed danger amount");
        hover.Present(target, 1); hover.Present(target, 1);
        var views = new SerializedObject(hover).FindProperty("views");
        bool shown = false;
        for (int i = 0; i < views.arraySize; i++)
        {
            var view = views.GetArrayElementAtIndex(i);
            var root = (Transform)view.FindPropertyRelative("root").objectReferenceValue;
            if (!root.gameObject.activeSelf) continue;
            var group = (CanvasGroup)view.FindPropertyRelative("alarmGroup").objectReferenceValue;
            var preview = (HexAlarmPreview)view.FindPropertyRelative("alarmPreview").objectReferenceValue;
            var icons = preview.GetComponentsInChildren<Image>().Where(image => image.transform.parent == preview.transform).ToArray();
            Check(group.alpha == 1 && icons.Length == Mathf.CeilToInt(amount), "working hex shows all danger skulls on hover");
            if (expectedRed != null)
                Check(icons.Select(icon => icon.sprite == AlarmSystem.Instance.SkullSprite).SequenceEqual(expectedRed),
                    "red threshold skull does not shift after reserving the action");
            shown = true;
        }
        Check(shown, "active hover view is present for working action");
        hover.enabled = true;
    }

    static IEnumerator CaptureOpeningHover(Vector2 point)
    {
        var hover = Object.FindFirstObjectByType<CrystalHexHover>();
        hover.enabled = false;
        hover.Present(crystal.GetHoverTarget(point), 1); hover.Present(crystal.GetHoverTarget(point), 1);
        yield return null; yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-working-hover.png"));
        yield return null; yield return null;
        hover.enabled = true;
    }

    static void CheckManualHint(Vector2 point, bool expected)
    {
        var target = crystal.GetHoverTarget(point);
        Check(crystal.CanStartManually(target) == expected, "manual start hint matches illuminated idle action: " + expected);
        var hover = Object.FindFirstObjectByType<CrystalHexHover>();
        hover.enabled = false;
        hover.Present(target, 1);
        Canvas.ForceUpdateCanvases();
        var views = new SerializedObject(hover).FindProperty("views");
        bool visible = false;
        for (int i = 0; i < views.arraySize; i++)
        {
            var row = (GameObject)views.GetArrayElementAtIndex(i).FindPropertyRelative("startRow").objectReferenceValue;
            if (!row.activeInHierarchy) continue;
            visible = true;
            var icon = row.GetComponentInChildren<Image>();
            Check(icon.sprite == AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/LBM.png") &&
                icon.rectTransform.rect.size == icon.sprite.rect.size * 2, "manual start displays LMB icon at double pixel size");
        }
        Check(visible == expected, "hover displays manual start row only when usable: " + expected);
        hover.enabled = true;
    }

    static IEnumerator PreviewManualHint(Vector2 point)
    {
        var hover = Object.FindFirstObjectByType<CrystalHexHover>();
        hover.enabled = false;
        hover.Present(crystal.GetHoverTarget(point), 1);
        var views = new SerializedObject(hover).FindProperty("views");
        for (int i = 0; i < views.arraySize; i++)
        {
            var root = (Transform)views.GetArrayElementAtIndex(i).FindPropertyRelative("root").objectReferenceValue;
            if (root.gameObject.activeSelf) root.position = Vector3.zero;
        }
        Canvas.ForceUpdateCanvases();
        yield return null; yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-manual-start-hint.png"));
        yield return null; yield return null;
        hover.enabled = true;
    }

    static IEnumerator PauseChecks(ResourceRequester stone)
    {
        int starts = 0, completions = 0;
        stone.OnAllResourcesReceived.AddListener(() => starts++);
        stone.OnActionExecuted.AddListener(() => completions++);
        var timer = stone.GetComponentsInChildren<TimerController>(true).First(t => t.IsRunning);
        var wait = Wait(.4f); while (wait.MoveNext()) yield return null;
        float remaining = timer.TimeRemaining;
        float alarm = AlarmSystem.Instance.ReservedActionAlarm;
        float activeAlarm = AlarmSystem.Instance.ActiveReservedActionAlarm;
        float ownAlarm = crystal.GetActionAlarm(crystal.GetHoverTarget(stone.transform.position));
        var materials = stone.requirements.Where(r => !r.resourceType.isHumanResource).Select(r => r.currentAmount).ToArray();
        Check(crystal.BeginDrag(2), "working production beam can be moved");
        crystal.DragTo(new Vector2(48, -5)); crystal.DropAt(new Vector2(48, -5));
        wait = Wait(.4f); while (wait.MoveNext()) yield return null;
        Check(stone.IsCyclePaused && timer.IsPaused && !timer.IsRunning && Mathf.Approximately(timer.TimeRemaining, remaining), "production pauses with exact progress when light leaves");
        var bar = (GameObject)new SerializedObject(timer).FindProperty("progressBarObject").objectReferenceValue;
        Check(bar.activeInHierarchy && bar.GetComponent<CanvasGroup>().alpha == 1, "paused production keeps visible progress bar");
        Check(!stone.GetComponentsInChildren<ProductionFragments>(true).Any(p => p.IsEmitting), "paused production emits no work fragments");
        Check(Mathf.Approximately(alarm, AlarmSystem.Instance.ReservedActionAlarm), "pause retains alarm reservation without adding danger");
        Check(Mathf.Approximately(AlarmSystem.Instance.ActiveReservedActionAlarm, activeAlarm - ownAlarm),
            "paused production is excluded from the red-skull forecast");
        Check(Place(3, stone.transform.position) && timer.IsRunning && Mathf.Approximately(timer.TimeRemaining, remaining), "different beam resumes the same paid cycle");
        Check(Mathf.Approximately(AlarmSystem.Instance.ActiveReservedActionAlarm, activeAlarm),
            "resumed production is included in the forecast exactly once");
        Check(crystal.BeginDrag(3), "resumed production remains draggable");
        crystal.CancelDrag();
        Check(timer.IsRunning && !stone.IsCyclePaused, "cancelled drag resumes production at its original position");
        Place(3, new Vector2(48, -5));
        remaining = timer.TimeRemaining;

        // Recall and replace an actual walking worker near the portal, keeping the timer paused.
        Vector3 fixturePosition = stone.transform.root.position;
        stone.transform.root.position = Warehouse.Instance.GetSpawnPoint() + Vector3.right;
        var human = stone.requirements.Single(r => r.resourceType.isHumanResource);
        var resources = GlobalResourceManager.Instance;
        resources.SetResourceAmount(human.resourceType, OrderManager.CountHumansAwayFromPortal(human.resourceType));
        int before = resources.GetResourceAmount(human.resourceType);
        int freeBefore = OrderManager.CountHumansAtPortal(human.resourceType);
        var previousWalkers = Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None);
        Check(stone.CanRecallHumans && crystal.RecallHumansAt(stone.transform.position), "right click on unlit paused building recalls resident");
        var returning = Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None).Except(previousWalkers).Single();
        Check(human.currentAmount == 0 && resources.GetResourceAmount(human.resourceType) == before, "recall creates a returning worker without immediate refund");
        Check(!crystal.RecallHumansAt(stone.transform.position), "repeated recall cannot duplicate resident");
        wait = Wait(1.8f); while (wait.MoveNext()) yield return null;
        Check(returning == null && resources.GetResourceAmount(human.resourceType) == before &&
            OrderManager.CountHumansAtPortal(human.resourceType) == freeBefore + 1, "recalled resident physically returns once while population stays constant");
        Check(stone.IsCyclePaused && Mathf.Approximately(timer.TimeRemaining, remaining) && bar.GetComponent<CanvasGroup>().alpha == 1, "worker recall preserves paused progress and visible bar");
        var icons = (Transform)new SerializedObject(stone).FindProperty("iconsContainer").objectReferenceValue;
        Check(icons.gameObject.activeInHierarchy && icons.GetComponentsInChildren<SpriteRenderer>().Any(s => s.sprite == human.resourceType.resourceIcon), "paused building displays its missing worker resource icon");
        var humanIcon = icons.GetComponentsInChildren<SpriteRenderer>().First(s => s.sprite == human.resourceType.resourceIcon);
        Check(humanIcon.bounds.min.y > bar.transform.position.y + .5f, "missing worker icon stays above the visible progress bar without overlap");
        Check(materials.SequenceEqual(stone.requirements.Where(r => !r.resourceType.isHumanResource).Select(r => r.currentAmount)), "recall preserves all prepaid materials");
        Vector3 nearPortal = stone.transform.root.position;
        var camera = Camera.main;
        var cameraControls = camera.GetComponent<CameraView2D>();
        bool cameraControlsEnabled = cameraControls != null && cameraControls.enabled;
        if (cameraControls != null) cameraControls.enabled = false;
        Vector3 cameraPosition = camera.transform.position;
        float cameraSize = camera.orthographicSize;
        stone.transform.root.position = fixturePosition;
        camera.transform.position = new Vector3(fixturePosition.x, fixturePosition.y, cameraPosition.z);
        camera.orthographicSize = 4;
        yield return null; yield return null;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/light-paused-worker.png"));
        yield return null; yield return null;
        stone.transform.root.position = nearPortal;
        camera.transform.position = cameraPosition;
        camera.orthographicSize = cameraSize;
        if (cameraControls != null) cameraControls.enabled = cameraControlsEnabled;
        Check(Place(2, stone.transform.position) && stone.NeedsAnyResource() && !timer.IsRunning, "returning light waits for replacement worker with old timer intact");
        Check(Mathf.Approximately(AlarmSystem.Instance.ActiveReservedActionAlarm, activeAlarm - ownAlarm),
            "paused production awaiting a replacement worker remains excluded from forecast");
        var replacement = Warehouse.Instance.SpawnHumanForJob(stone, human.resourceType);
        Check(replacement != null, "portal dispatches replacement to paused production");
        wait = Wait(1.8f); while (wait.MoveNext()) yield return null;
        Check(replacement == null && human.currentAmount == 1 && timer.IsRunning && !stone.IsCyclePaused, "replacement arrival resumes paused work");
        Check(Mathf.Approximately(AlarmSystem.Instance.ActiveReservedActionAlarm, activeAlarm),
            "actual worker arrival restores production danger to the forecast");
        Check(resources.GetResourceAmount(human.resourceType) == before && starts == 0 && completions == 0, "resume neither duplicates a resident nor repeats start and completion events");
        Check(timer.TimeRemaining < remaining && timer.TimeRemaining > 0, "replacement continues remaining time without reset");
        Check(crystal.BeginDrag(2), "replacement cycle can be paused again");
        crystal.DropAt(new Vector2(48, -5));
        stone.transform.root.position = fixturePosition;
        Check(Place(2, stone.transform.position), "original production fixture resumes for completion");
        resources.SetResourceAmount(human.resourceType, 0);
    }
}
#endif
