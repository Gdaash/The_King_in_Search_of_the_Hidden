#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using GameFoundation.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FirstPortalRunValidation
{
    const string Key = "FirstPortalRunValidation";
    static IEnumerator routine;
    static readonly System.Collections.Generic.List<string> checks = new();
    static FirstPortalRunValidation() { EditorApplication.playModeStateChanged += Changed; }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        SessionState.SetString(Key + ".scene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");
        SessionState.SetBool(Key, true);
        BuildingUpgradeValidation.Begin(); // Uses slot 3 temporarily and restores the original envelope afterwards.
        SaveSlotPrefs.SetString("foundation.daycycle", "{\"day\":1,\"totalPortalEntries\":0,\"firstPortalRun\":false,\"refugees\":2}");
        SaveSlotPrefs.SetString("foundation.forestForaging", "{}");
        SaveSlotPrefs.SetInt("foundation.quest.first_expedition_supplies.completed", 0);
        SaveSlotPrefs.SetInt("foundation.quest.first_expedition_supplies.claimed", 0);
        SaveSlotPrefs.Save();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            checks.Clear(); routine = Test(); EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + ".scene", ""));
            BuildingUpgradeValidation.Restore();
            SessionState.SetBool(Key, false);
        }
    }

    static void Tick()
    {
        try { if (routine.MoveNext()) return; checks.Add("COMPLETE"); }
        catch (Exception ex) { checks.Add("FAIL " + ex); }
        Directory.CreateDirectory("Temp");
        File.WriteAllLines("Temp/FirstPortalRunValidation.txt", checks);
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }

    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks.Add("PASS " + message);
    }

    static IEnumerator Test()
    {
        yield return null;
        var day = DayCycleService.Instance;
        Check(day != null && day.Enter(day.Portals[0]), "First portal entry succeeds");
        Check(DayCycleService.IsFirstPortalRun, "First entry selects scripted layout");
        var loadWorld = SceneManager.LoadSceneAsync("World");
        while (!loadWorld.isDone) yield return null;
        yield return null;
        var hexes = UnityEngine.Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None).OrderByDescending(h => h.transform.position.x).ToArray();
        Check(hexes.Length >= 8 && hexes.All(h => h.prefabToSpawn == null && !h.shouldAutoUnlock), "No random content or automatic reveals in first run");
        var order = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Managers/First Portal Run Spawn Order.prefab").GetComponent<FirstPortalRunSpawnOrder>();
        string[] expected = Enumerable.Range(0, 8).Select(i => order.ContentAt(i) != null ? order.ContentAt(i).name : null).ToArray();
        for (int i = 0; i < expected.Length; i++)
        {
            hexes[i].RemoveHex();
            Check((hexes[i].prefabToSpawn != null ? hexes[i].prefabToSpawn.name : null) == expected[i], "Opening " + (i + 1) + " reveals " + (expected[i] ?? "empty"));
            if (expected[i] != null)
                Check(UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Any(g => g.name == expected[i] + "(Clone)" && Vector3.Distance(g.transform.position, hexes[i].transform.position) < 0.01f), "Content spawned on chosen hex");
            hexes[i].RemoveHex(); // Repeated completion must not advance the sequence.
        }
        var alarm = UnityEngine.Object.FindFirstObjectByType<AlarmSystem>();
        var presentation = UnityEngine.Object.FindFirstObjectByType<FirstPortalRunPresentation>();
        var escape = UnityEngine.Object.FindFirstObjectByType<WorldEscapeController>();
        var button = (UnityEngine.UI.Button)new SerializedObject(escape).FindProperty("escapeButton").objectReferenceValue;
        Check(alarm.ConfiguredThresholds.Count == 0 && !button.gameObject.activeSelf, "No waves or escape button before supplies");
        alarm.SetAlarm(42f);
        var quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/First Expedition.asset");
        foreach (var goal in quest.requirements)
            GlobalResourceManager.Instance.SetResourceAmount(goal.resource, goal.amount - 1);
        foreach (var goal in quest.requirements.Take(quest.requirements.Count - 1))
            GlobalResourceManager.Instance.SetResourceAmount(goal.resource, goal.amount);
        Check(alarm.ConfiguredThresholds.Count == 0 && !button.gameObject.activeSelf, "Partial supplies do not release wave");
        var last = quest.requirements.Last();
        GlobalResourceManager.Instance.SetResourceAmount(last.resource, last.amount);
        Check(presentation.WaveReleased && alarm.ConfiguredThresholds.Count == 1 && Mathf.Approximately(alarm.ConfiguredThresholds[0].alarmValue, 42f), "Supplies release one wave at current alarm");
        Check(!button.gameObject.activeSelf && UnityEngine.Object.FindFirstObjectByType<DangerLevelNotificationView>() != null, "Wave notification appears before escape button");
        var homeParent = button.transform.parent;
        float deadline = Time.realtimeSinceStartup + 15f;
        while (UnityEngine.Object.FindFirstObjectByType<DangerLevelNotificationView>() != null && Time.realtimeSinceStartup < deadline) yield return null;
        float notificationEnded = Time.realtimeSinceStartup;
        Check(!button.gameObject.activeSelf, "Escape stays hidden when danger notification disappears");
        while (!button.gameObject.activeSelf && Time.realtimeSinceStartup < deadline) yield return null;
        Check(Time.realtimeSinceStartup - notificationEnded >= 0.85f, "Escape waits one second after notification");
        Check(button.gameObject.activeSelf && UnityEngine.Object.FindFirstObjectByType<DangerLevelNotificationView>() == null, "Escape appears only after notification disappears");
        Check(Mathf.Approximately(button.transform.localScale.x, 1.5f) && !button.interactable, "Escape introduced at 1.5 scale with input disabled");
        var rect = (RectTransform)button.transform;
        var canvas = button.GetComponentInParent<Canvas>().rootCanvas;
        var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        var screenCenter = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        Check(Vector2.Distance(screenCenter, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) < 1f, "Visible escape button is centered horizontally and vertically");
        float centerUntil = Time.realtimeSinceStartup + 0.4f;
        while (Time.realtimeSinceStartup < centerUntil)
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            screenCenter = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            Check(Vector2.Distance(screenCenter, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)) < 1f, "Layout rebuild keeps introduction in screen center");
        }
        while (!button.interactable && Time.realtimeSinceStartup < deadline) yield return null;
        Check(button.interactable && Mathf.Approximately(button.transform.localScale.x, 1f) && button.transform.parent == homeParent, "Escape reaches its authored position and becomes clickable");
        GlobalResourceManager.Instance.AddResource(last.resource, 1);
        Check(alarm.ConfiguredThresholds.Count == 1, "Further resource changes do not duplicate wave");
        day.DeactivateSelectedPortal();
        Check(!DayCycleService.IsFirstPortalRun, "Returning finishes first-run mode");
        var loadBase = SceneManager.LoadSceneAsync("Base");
        while (!loadBase.isDone) yield return null;
        yield return null;
        day.NextDay();
        Check(day.Enter(day.Portals[0]) && !DayCycleService.IsFirstPortalRun, "Second entry uses regular generation");
        loadWorld = SceneManager.LoadSceneAsync("World");
        while (!loadWorld.isDone) yield return null;
        yield return null;
        Check(UnityEngine.Object.FindObjectsByType<HexBlocker>(FindObjectsSortMode.None).Any(h => h.prefabToSpawn != null), "Regular map content assigned in second run");
    }
}
#endif

