#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.MetaProgression;
using GameFoundation.Saves;
using UnityEditor;
using UnityEngine;

public static class CrystalPowerValidation
{
    private static IEnumerator routine;
    private static readonly List<string> report = new();
    private static WorldFlashlightAvailability crystal;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        report.Add("PASS " + message);
    }
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < .0001f;
    public static void Run()
    {
        if (!Application.isPlaying || SessionState.GetString("BuildingUpgradeValidation.Backup", "") == "")
            throw new InvalidOperationException("Use CrystalLightValidation.SeedAndPlay first (isolated save).");
        report.Clear(); routine = Checks(); EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying) throw new Exception("Play stopped");
            if (routine.MoveNext()) return;
        }
        catch (Exception e) { report.Add("FAIL " + e); Debug.LogException(e); }
        EditorApplication.update -= Tick;
        System.IO.File.WriteAllLines("Temp/CrystalPowerValidation.txt", report);
        Debug.Log("Crystal power validation: " + report.Last());
    }
    private static IEnumerator Wait(float seconds)
    {
        double end = EditorApplication.timeSinceStartup + seconds;
        while (EditorApplication.timeSinceStartup < end) yield return null;
    }
    private static IEnumerator Drain(int count)
    {
        GameSpeedControls.SetSimulationSpeed(1);
        var hexes = HexLightUnlocker.ActiveInstances.Where(h => crystal.GetHoverTarget(h.transform.position).State == WorldFlashlightAvailability.HoverState.Available)
            .OrderBy(h => h.transform.position.sqrMagnitude).Take(count).ToArray();
        Check(hexes.Length == count, "enough available hexes for " + count + " cells");
        foreach (var hex in hexes) { Check(crystal.SelectAt(hex.transform.position), "spend cell on actual hex"); hex.gameObject.SetActive(false); }
        GameSpeedControls.SetSimulationSpeed(0);
        var wait = Wait(.12f); while (wait.MoveNext()) yield return null;
    }
    private static IEnumerator Checks()
    {
        crystal = UnityEngine.Object.FindFirstObjectByType<WorldFlashlightAvailability>();
        var stats = Resources.Load<GlobalStats>("Global/globalHexStats");
        GameSpeedControls.SetSimulationSpeed(0);
        Check(crystal.CellCount == 6 && Near(crystal.ChargingPower, 1), "base power is 1 with no purchased power upgrades");
        foreach (int count in new[] { 1, 2, 3, 6 })
        {
            var drain = Drain(count); while (drain.MoveNext()) yield return null;
            Check(crystal.ChargingCellCount == count, "only " + count + " empty free cells share power");
            crystal.TickRecharge(15);
            Check(Enumerable.Range(0, count).All(i => Near(crystal.Charge(i), .5f / count)), "15 seconds split equally between " + count + " cells");
            Check(Enumerable.Range(count, 6 - count).All(i => Near(crystal.Charge(i), 1)), "full cells do not take power");
            crystal.TickRecharge(30 * count - 15);
            Check(Enumerable.Range(0, 6).All(i => Near(crystal.Charge(i), 1)), count + " cells fully recharge in " + (30 * count) + " seconds");
        }
        var d = Drain(1); while (d.MoveNext()) yield return null;
        crystal.TickRecharge(24); // first cell at 80%, second newly spent
        d = Drain(1); while (d.MoveNext()) yield return null;
        crystal.TickRecharge(30);
        Check(Near(crystal.Charge(0), 1) && Near(crystal.Charge(1), .8f), "remaining power transfers immediately when the first cell finishes");
        float before = crystal.Charge(1);
        crystal.TickRecharge(0);
        var wait = Wait(.3f); while (wait.MoveNext()) yield return null;
        Check(Near(before, crystal.Charge(1)), "pause and zero simulation delta preserve charge");
        crystal.TickRecharge(6);

        GameSpeedControls.SetSimulationSpeed(1);
        var activeHex = HexLightUnlocker.ActiveInstances.First(h => crystal.GetHoverTarget(h.transform.position).State == WorldFlashlightAvailability.HoverState.Available);
        crystal.SelectAt(activeHex.transform.position);
        d = Drain(1); while (d.MoveNext()) yield return null;
        crystal.TickRecharge(15);
        Check(crystal.State(0) == WorldFlashlightAvailability.CellState.Working && Near(crystal.Charge(0), 0) && Near(crystal.Charge(1), .5f), "working beam does not recharge or take a power share");
        activeHex.gameObject.SetActive(false);
        wait = Wait(.12f); while (wait.MoveNext()) yield return null;
        var resources = GlobalResourceManager.Instance;
        resources.SetResourceAmount(crystal.RechargeResource, 1000);
        Check(crystal.RechargeAll() && resources.GetResourceAmount(crystal.RechargeResource) == 999, "manual full recharge still costs exactly one ore");

        var treeAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Laboratory Skill Tree.prefab");
        var tree = UnityEngine.Object.Instantiate(treeAsset);
        stats.UnlockUpgrade(ScientificUpgrades.PortalArrows);
        tree.GetComponentsInChildren<SkillButton>().Single(s => s.skillID == ScientificUpgrades.PortalArrows).RefreshStatus();
        var buttons = ScientificUpgrades.CrystalPower.Select(id => tree.GetComponentsInChildren<SkillButton>().Single(s => s.skillID == id)).ToArray();
        Check(!buttons[1].CanPurchaseNow, "next power tier is locked until the previous tier is purchased");
        resources.SetResourceAmount(crystal.RechargeResource, 0);
        buttons[0].uiButton.onClick.Invoke();
        Check(!stats.HasUpgrade(ScientificUpgrades.CrystalPower[0]) && Near(crystal.ChargingPower, 1), "unaffordable upgrade grants no power");
        resources.SetResourceAmount(crystal.RechargeResource, 1000);
        for (int i = 0; i < buttons.Length; i++)
        {
            int balance = resources.GetResourceAmount(crystal.RechargeResource);
            Check(buttons[i].CanPurchaseNow, "tier " + (i + 1) + " is purchasable");
            buttons[i].uiButton.onClick.Invoke();
            Check(Near(crystal.ChargingPower, 1 + .2f * (i + 1)), "tier " + (i + 1) + " adds 20% of base power");
            Check(resources.GetResourceAmount(crystal.RechargeResource) == balance - buttons[i].cost, "tier cost deducted exactly once");
            buttons[i].uiButton.onClick.Invoke();
            Check(resources.GetResourceAmount(crystal.RechargeResource) == balance - buttons[i].cost, "purchased tier cannot charge twice");
        }
        Check(crystal.CellCount == 6, "power upgrades do not change cell count");
        d = Drain(2); while (d.MoveNext()) yield return null;
        crystal.TickRecharge(30);
        Check(Near(crystal.Charge(0), 1) && Near(crystal.Charge(1), 1), "200% power recharges two cells in their standard 30 seconds");
        var envelope = UnityEngine.PlayerPrefs.GetString("foundation.slot.3.saveData");
        Check(ScientificUpgrades.CrystalPower.All(id => envelope.Contains(id + "_Purchased")), "all five purchases written to the existing player save envelope");
        UnityEngine.Object.DestroyImmediate(tree);
        tree = UnityEngine.Object.Instantiate(treeAsset);
        Check(tree.GetComponentsInChildren<SkillButton>().Where(s => ScientificUpgrades.CrystalPower.Contains(s.skillID)).All(s => s.isPurchased), "new laboratory instance restores every purchased tier");
        UnityEngine.Object.DestroyImmediate(tree);
        d = Drain(1); while (d.MoveNext()) yield return null;
        crystal.DisableAllForEscape(); crystal.TickRecharge(300);
        Check(Near(crystal.Charge(0), 0), "escape stops charging");
        Check(true, "ALL CRYSTAL POWER CHECKS PASSED");
    }
}
#endif
