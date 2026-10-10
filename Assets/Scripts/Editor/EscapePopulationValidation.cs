#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using GameFoundation.MetaProgression;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EscapePopulationValidation
{
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Escape population: " + message);
    }

    public static void Test()
    {
        var manager = GlobalResourceManager.Instance;
        var resource = manager.AvailableResources.First(r => r.isHumanResource);
        Check(OrderManager.CountHumansAwayFromPortal(resource) == 0, "clean test scene");
        var root = new GameObject("Escape population validation");
        try
        {
            var controller = root.AddComponent<WorldEscapeController>();
            typeof(WorldEscapeController).GetField("humanResource", Private).SetValue(controller, resource);
            var settle = typeof(WorldEscapeController).GetMethod("SettleUnreturnedHumans", Private);
            var guard = typeof(WorldEscapeController).GetField("_humanLossesSettled", Private);
            manager.SetResourceAmount(resource, 13);
            settle.Invoke(controller, null);
            Check(manager.GetResourceAmount(resource) == 13, "all returned: no losses");

            var porterObject = new GameObject("Unreturned porter");
            porterObject.transform.SetParent(root.transform);
            var porter = porterObject.AddComponent<Porter>();
            typeof(Porter).GetField("_isReturningToWarehouse", Private).SetValue(porter, true);
            var workerObject = new GameObject("Unreturned worker");
            workerObject.transform.SetParent(root.transform);
            var worker = workerObject.AddComponent<HumanUnit>();
            typeof(HumanUnit).GetField("_isReturningToWarehouse", Private).SetValue(worker, true);
            Check(OrderManager.CountHumansAwayFromPortal(resource) == 2, "porter and worker counted");
            guard.SetValue(controller, false);
            settle.Invoke(controller, null);
            Check(manager.GetResourceAmount(resource) == 11, "13 population minus two unreturned");
            settle.Invoke(controller, null);
            Check(manager.GetResourceAmount(resource) == 11, "settlement cannot run twice");
            manager.SetResourceAmount(resource, 1);
            guard.SetValue(controller, false);
            settle.Invoke(controller, null);
            Check(manager.GetResourceAmount(resource) == 0, "losses cannot make population negative");
            Debug.Log("PASS: escape population — returned, porter + worker losses, repeat guard, zero floor.");
        }
        finally { Object.DestroyImmediate(root); }
    }
}
#endif
