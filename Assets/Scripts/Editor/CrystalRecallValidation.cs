#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.MetaProgression;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CrystalRecallValidation
{
    static IEnumerator routine;
    static readonly List<string> report = new();
    static void Check(bool value, string message)
    { if (!value) throw new Exception(message); report.Add("PASS " + message); }
    static IEnumerator Wait(float seconds)
    { double end = EditorApplication.timeSinceStartup + seconds; while (EditorApplication.timeSinceStartup < end) yield return null; }
    static void Tick()
    {
        try
        {
            if (!Application.isPlaying) throw new Exception("Play stopped");
            if (routine.MoveNext()) return;
            EditorApplication.update -= Tick;
            System.IO.File.WriteAllLines("Temp/CrystalRecallValidation.txt", report);
            Debug.Log("Crystal recall validation passed: " + report.Count);
        }
        catch (Exception e)
        {
            EditorApplication.update -= Tick; report.Add("FAIL " + e);
            System.IO.File.WriteAllLines("Temp/CrystalRecallValidation.txt", report); Debug.LogException(e);
        }
    }
    public static void Run()
    {
        if (!Application.isPlaying || SessionState.GetString("BuildingUpgradeValidation.Backup", "") == "")
            throw new Exception("Use the isolated validation save first.");
        var steam = Object.FindFirstObjectByType<Steamworks.SteamManager>(); if (steam != null) steam.enabled = false;
        report.Clear(); routine = Checks(); EditorApplication.update += Tick;
    }
    static void Show(CrystalHexHover hover, WorldFlashlightAvailability c, Vector3 position)
    { hover.Present(c.GetHoverTarget(position), 1); hover.Present(c.GetHoverTarget(position), 1); Canvas.ForceUpdateCanvases(); }
    static void CheckBar(CrystalHexHover hover, bool visible, WorldFlashlightAvailability c)
    {
        var visual = hover.GetComponentsInChildren<SpriteRenderer>().First(s => s.color.a > .9f).transform.parent;
        var bar = visual.Find("Crystal Energy");
        Check(bar.GetComponent<CanvasGroup>().alpha == (visible ? 1 : 0), "hover charge bar visibility " + visible);
        foreach (var cellBar in Object.FindObjectsByType<CrystalCellBar>(FindObjectsSortMode.None)) cellBar.Refresh();
        for (int i = 0; i < c.CellCount; i++)
        {
            var fill = bar.Find("Crystal Cells/Cell " + (i + 1) + "/Charge Area/Charge").GetComponent<Image>();
            Check(Mathf.Approximately(fill.rectTransform.anchorMax.y, c.Charge(i)), "hover cell " + (i + 1) + " matches charge");
        }
        var icon = visual.Find("Caption/Recall People/RBM").GetComponent<Image>();
        Check(icon.rectTransform.rect.size == icon.sprite.rect.size * 2, "RBM uses exact double pixel size");
    }
    static IEnumerator Checks()
    {
        var c = Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        var hover = Object.FindFirstObjectByType<CrystalHexHover>(); hover.enabled = false;
        var resources = GlobalResourceManager.Instance;
        OrderManager.Instance.enabled = false;
        Check(c.CellCount == 6, "six-cell configuration");
        var hex = HexLightUnlocker.ActiveInstances.Where(h => !h.IsUnlocked() && h.GetComponentInParent<HexBlocker>() != null && !h.GetComponentInParent<HexBlocker>().IsBlocked).OrderBy(h => h.transform.position.sqrMagnitude).First();
        Vector3 position = hex.transform.position;
        Check(c.SelectAt(position), "open a walkable test hex");
        GameSpeedControls.SetSimulationSpeed(4);
        var wait = Wait(2.2f); while (wait.MoveNext()) yield return null;
        Check(hex == null || hex.IsUnlocked(), "test hex opened with normal timer");
        foreach (var r in ResourceRequester.ActiveInstances.ToArray())
            if (Vector2.Distance(r.transform.position, position) < 2) r.enabled = false;
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Magic Ore Mine.prefab"), position, Quaternion.identity);
        var builder = root.transform.Find("Controller").GetComponent<ResourceRequester>();
        var mine = root.transform.Find("Mine").GetComponent<ResourceRequester>();
        yield return null;
        var human = mine.requirements.First(r => r.resourceType.isHumanResource);
        var berry = mine.requirements.First(r => !r.resourceType.isHumanResource);
        resources.SetResourceAmount(berry.resourceType, 0);
        int humansBefore = resources.GetResourceAmount(human.resourceType);
        Check(c.SelectAt(position), "reserve constructor");
        foreach (var requirement in builder.requirements)
            while (requirement.currentAmount < requirement.requiredAmount)
            {
                Check(resources.TrySpendResource(requirement.resourceType, 1), "construction resource spent " + requirement.resourceType.name);
                builder.DeliverResource(requirement.resourceType);
            }
        Check(builder.IsProcessing && !builder.CanRecallHumans, "working constructor cannot recall worker");
        wait = Wait(4.4f); while (wait.MoveNext()) yield return null;
        Check(mine.isActiveAndEnabled && !builder.CanSelectCrystalCycle, "constructor activates Mine and cannot be built twice");
        Check(human.currentAmount == 1 && builder.requirements.First(r => r.resourceType.isHumanResource).currentAmount == 0, "builder transfers exactly one resident worker");
        Check(Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None).Length == 0 && resources.GetResourceAmount(human.resourceType) == humansBefore - 1, "construction does not duplicate or refund resident worker");
        Check(mine.CanRecallHumans && c.GetHoverTarget(position).CanRecallHumans, "idle resident offers recall");
        GameSpeedControls.SetSimulationSpeed(0);
        Show(hover, c, position); CheckBar(hover, true, c);
        ScreenCapture.CaptureScreenshot("Temp/crystal-recall-idle.png");
        wait = Wait(.25f); while (wait.MoveNext()) yield return null;
        GameSpeedControls.SetSimulationSpeed(1);
        Check(c.SelectAt(position), "resident mine reserves production cell");
        Check(c.GetHoverTarget(position).State == WorldFlashlightAvailability.HoverState.WaitingForResources && mine.CanRecallHumans, "waiting for berries permits recall");
        Show(hover, c, position); CheckBar(hover, false, c);
        var captions = hover.GetComponentsInChildren<TMP_Text>().Where(t => t.gameObject.activeInHierarchy).ToArray();
        Check(captions.Any(t => t.text == "отменить действие") && captions.Any(t => t.text == "Вернуть людей в портал"), "cancel and recall hints share the visible block");
        ScreenCapture.CaptureScreenshot("Temp/crystal-recall-waiting.png");
        wait = Wait(.25f); while (wait.MoveNext()) yield return null;
        mine.DeliverResource(berry.resourceType);
        Check(mine.IsProcessing && !mine.CanRecallHumans && !c.RecallHumansAt(position), "working production denies recall");
        GameSpeedControls.SetSimulationSpeed(4);
        wait = Wait(1.7f); while (wait.MoveNext()) yield return null;
        Check(!mine.IsProcessing && human.currentAmount == 1 && berry.currentAmount == 0, "production consumes berries and retains worker");
        c.RechargeAll();
        var drains = new List<GameObject>();
        for (int i = 0; i < c.CellCount; i++)
        {
            var drain = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Stone 3.prefab"), new Vector3(50 + i * 4, 0), Quaternion.identity);
            drains.Add(drain);
            Check(c.SelectAt(drain.transform.position), "reserve cell for busy-crystal preview " + i);
        }
        Check(c.GetHoverTarget(position).State == WorldFlashlightAvailability.HoverState.CrystalBusy, "charged occupied cells show crystal busy");
        Show(hover, c, position); CheckBar(hover, true, c);
        Check(hover.GetComponentsInChildren<TMP_Text>().Any(t => t.text == "кристалл занят"), "busy hint appears with return people hint");
        ScreenCapture.CaptureScreenshot("Temp/crystal-recall-busy.png");
        wait = Wait(.25f); while (wait.MoveNext()) yield return null;
        foreach (var drain in drains) Object.Destroy(drain);
        wait = Wait(.15f); while (wait.MoveNext()) yield return null;
        var spawn = Warehouse.Instance.GetSpawnPoint();
        // Return from a nearby walkable position so the movement and deposit can be tested quickly.
        root.transform.position = spawn + Vector3.right;
        position = mine.transform.position;
        int beforeRecall = resources.GetResourceAmount(human.resourceType);
        Check(c.RecallHumansAt(position), "right-click action releases idle resident");
        Check(human.currentAmount == 0 && resources.GetResourceAmount(human.resourceType) == beforeRecall, "recall does not deposit before arrival");
        var returning = Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None).Single();
        Check(returning.IsReturningToWarehouse() && !returning.CanBeReassigned && returning.GetTarget() == Warehouse.Instance.GetSpawnPointTransform(), "recalled person targets portal and rejects reassignment");
        Check(!c.RecallHumansAt(position), "repeated recall cannot duplicate person");
        wait = Wait(1.5f); while (wait.MoveNext()) yield return null;
        Check(returning == null && resources.GetResourceAmount(human.resourceType) == beforeRecall + 1, "normal walking reaches portal and deposits exactly one person");
        Check(c.SelectAt(position), "mine can be selected after recall");
        mine.DeliverResource(berry.resourceType);
        Check(!mine.IsProcessing && human.currentAmount == 0, "berries alone cannot operate mine without new worker");
        var replacement = Warehouse.Instance.SpawnHumanForJob(mine, human.resourceType);
        Check(replacement != null, "new worker dispatched from available population");
        wait = Wait(1.6f); while (wait.MoveNext()) yield return null;
        Check(replacement == null && human.currentAmount == 1 && mine.IsProcessing, "replacement physically enters and starts work");
        var escape = Object.FindFirstObjectByType<WorldEscapeController>();
        var escapeSo = new SerializedObject(escape);
        ((Button)escapeSo.FindProperty("escapeButton").objectReferenceValue).onClick.Invoke();
        Check(c.Escaped && human.currentAmount == 0, "escape evacuates resident even during production");
        Check(Object.FindObjectsByType<HumanUnit>(FindObjectsSortMode.None).Any(h => h.IsReturningToWarehouse()), "escape waits for physical returning worker");
    }
}
#endif
