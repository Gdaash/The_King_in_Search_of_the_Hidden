#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class DayReportValidation
{
    const string Key = "DayReportValidation";
    const string ReportPath = "Temp/DayReportValidation.txt";
    static IEnumerator routine;
    static readonly List<string> checks = new();
    static GlobalResourceManager resources;
    static ResourceType food, humans, influence, wood;
    static DayReportValidation() { EditorApplication.playModeStateChanged += Changed; }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory("Temp");
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) EditorSceneManager.SaveScene(scene, "Temp/DayReportUserScene.unity", true);
        SessionState.SetString(Key + ".start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Base.unity");
        SessionState.SetBool(Key, true);
        File.WriteAllText(ReportPath, "Starting day report regression\n");
        BuildingUpgradeValidation.Begin();
    }

    static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            checks.Clear();
            Application.runInBackground = true;
            routine = Test();
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Key + ".start", ""));
            BuildingUpgradeValidation.Restore();
            SessionState.SetBool(Key, false);
        }
    }

    static void Tick()
    {
        try { if (routine.MoveNext()) return; checks.Add("COMPLETE " + checks.Count + " checks"); }
        catch (Exception e) { checks.Add("FAIL " + e); }
        File.WriteAllLines(ReportPath, checks);
        EditorApplication.update -= Tick;
        EditorApplication.ExitPlaymode();
    }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        checks.Add("PASS " + message);
        File.WriteAllLines(ReportPath, checks);
    }
    static void ResetDay(int people = 5, int meals = 15, int crowns = 8)
    {
        resources.SetResourceAmount(humans, people);
        resources.SetResourceAmount(food, meals);
        resources.SetResourceAmount(influence, crowns);
        resources.SetResourceAmount(wood, 20);
        resources.SetResourceAmount(ResourceCatalog.Find("Swordsman"), 0);
        resources.SetResourceAmount(ResourceCatalog.Find("Archer"), 0);
        SaveSlotPrefs.DeleteKey("foundation.dayResourceLedger");
        DayResourceLedger.ResetForSlot();
        DayResourceLedger.EnsureDay(DayCycleService.Instance.Day);
    }
    static DayResourceLedger.Entry Entry(DayResourceLedger.Report report, ResourceType type) => report.entries.Single(e => e.resource == type.Id);
    static void Balanced(DayResourceLedger.Report report) => Check(report.entries.All(e => (long)e.start + e.runChange + e.ShelterChange == e.end), "all rows reconcile beginning, run, shelter and ending balances");

    static IEnumerator Test()
    {
        double ready = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < ready) yield return null;
        resources = GlobalResourceManager.Instance;
        food = ResourceCatalog.Find("Berry"); humans = ResourceCatalog.Find("Human");
        influence = ResourceCatalog.Find("Crown"); wood = ResourceCatalog.Find("Wood");
        ResetDay();
        DayResourceLedger.BeginRun(); // Reproduce a saved unfinished run while already in the shelter.
        DayResourceLedger.ResetForSlot(); // Also cover loading the stale run flag from disk.
        DayCycleService.Instance.NextDay();
        var report = DayResourceLedger.LastReport;
        Check(Entry(report, food).runChange == 0 && Entry(report, food).ShelterChange == -5 && Entry(report, food).end == 10, "daily food belongs to shelter after restoring an unfinished run");
        Check(Entry(report, influence).runChange == 0 && Entry(report, influence).ShelterChange == 1, "daily influence belongs to shelter");
        Balanced(report);
        var popup = Object.FindObjectsByType<DayReportPopup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(p => p.transform.parent.gameObject.activeInHierarchy);
        popup.Open(report);
        yield return null; yield return null;
        Canvas.ForceUpdateCanvases();
        var rows = popup.GetComponentsInChildren<DayReportRow>().ToArray();
        Check(rows.Length == 3, "day report displays food, influence and residents");
        Check(!popup.GetComponentsInChildren<TMP_Text>(true).Any(t => t.name == "BaseSpent" || t.text == "Укрытие +" || t.text == "Укрытие −"), "separate shelter gain and expense columns removed");
        var row = rows.First(r => ((TMP_Text)new SerializedObject(r).FindProperty("endLabel").objectReferenceValue).text.StartsWith("10 "));
        var shelter = (TMP_Text)new SerializedObject(row).FindProperty("shelterLabel").objectReferenceValue;
        Check(shelter.text == "−5" && shelter.color.r > shelter.color.g, "combined shelter value is signed and red for spending");
        var preview = new DayResourceLedger.Entry { resource = wood.Id, start = 20, baseGained = 7, baseSpent = 3, end = 24 };
        row.SetData(wood, preview);
        Check(shelter.text == "+4" && shelter.color.g > shelter.color.r, "gains and spending merge into one positive green result");
        preview.baseSpent = 7; preview.end = 20; row.SetData(wood, preview);
        Check(shelter.text == "0" && preview.HasChanges, "net-zero exchange remains in the report");
        row.SetData(food, Entry(report, food));
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/day-report-shelter.png"));
        yield return null; yield return null;
        popup.Close();

        ResetDay();
        var baseScene = SceneManager.GetActiveScene();
        var runScene = SceneManager.CreateScene("Day report run fixture");
        SceneManager.SetActiveScene(runScene);
        DayResourceLedger.BeginRun();
        resources.AddResource(food, 4); resources.AddResource(wood, 2);
        DayResourceLedger.EndRun(42);
        SceneManager.SetActiveScene(baseScene);
        SceneManager.UnloadSceneAsync(runScene);
        DayResourceLedger.EnterShelter();
        resources.TrySpendResource(food, 1); resources.TrySpendResource(wood, 3);
        DayCycleService.Instance.NextDay(); report = DayResourceLedger.LastReport;
        Check(Entry(report, food).runChange == 4 && Entry(report, food).ShelterChange == -6, "real run food is separate from shelter spending and daily meals");
        Check(Entry(report, wood).runChange == 2 && Entry(report, wood).ShelterChange == -3 && report.runDurationSeconds == 42, "run wood and duration are preserved");
        Balanced(report);

        ResetDay(); DayResourceLedger.BeginRun();
        Check(resources.TryExchangeResources(wood, 2, food, 1, ResourceCatalog.Find("Bow"), 1), "shelter crafting succeeds with a stale run flag");
        report = DayResourceLedger.FinishDay(DayCycleService.Instance.Day);
        Check(report.entries.All(e => e.runChange == 0) && Entry(report, wood).ShelterChange == -2 && Entry(report, food).ShelterChange == -1, "crafting is recorded before balances mutate and attributed to shelter");
        Balanced(report);

        ResetDay(3, 1, 1); DayResourceLedger.BeginRun(); DayCycleService.Instance.NextDay(); report = DayResourceLedger.LastReport;
        Check(report.starved == 2 && Entry(report, humans).ShelterChange == -2 && Entry(report, influence).ShelterChange == -1 && report.entries.All(e => e.runChange == 0), "starvation and influence penalty are shelter changes");
        Balanced(report);
    }
}
#endif
