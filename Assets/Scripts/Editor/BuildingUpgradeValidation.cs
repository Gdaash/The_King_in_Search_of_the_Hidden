#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using GameFoundation.Base;
using GameFoundation.Saves;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BuildingUpgradeValidation
{
    const string BackupKey = "BuildingUpgradeValidation.Backup";
    [Serializable] class Backup { public bool envelopeExists, createdExists, selectedExists; public string envelope; public int created, selected; }
    static BuildingUpgradeValidation() { EditorApplication.playModeStateChanged += StateChanged; }
    static void ResetCache() => typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService").GetMethod("ResetCache", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
    public static void Begin()
    {
        if (EditorApplication.isPlaying || SessionState.GetString(BackupKey, "") != "") throw new Exception("Validation already active");
        const string key = "foundation.slot.3.saveData";
        var backup = new Backup { envelopeExists = PlayerPrefs.HasKey(key), envelope = PlayerPrefs.GetString(key), createdExists = PlayerPrefs.HasKey("foundation.slot.3.created"), created = PlayerPrefs.GetInt("foundation.slot.3.created"), selectedExists = PlayerPrefs.HasKey("foundation.selectedSaveSlot"), selected = PlayerPrefs.GetInt("foundation.selectedSaveSlot", 1) };
        SessionState.SetString(BackupKey, JsonUtility.ToJson(backup));
        PlayerPrefs.SetString(key, "{\"version\":1,\"entries\":[]}"); ResetCache(); SaveSlotPrefs.Select(3);
        foreach (var b in BuildingUpgradeService.Catalog.buildings)
        {
            SaveSlotPrefs.SetInt("foundation.building." + b.id + ".built", 0);
            SaveSlotPrefs.SetInt("foundation.building." + b.id + ".upgradeLevel", 0);
        }
        foreach (string id in new[] { "Human", "Swordsman", "Archer", "Wood", "Stone", "Sword", "Bow" })
            SaveSlotPrefs.SetInt("Global_Resource_" + id, id == "Human" ? 3 : id == "Swordsman" || id == "Archer" ? 0 : 100);
        SaveSlotPrefs.Save(); EditorApplication.isPlaying = true;
    }
    static void StateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode) Restore();
    }
    public static void Restore()
    {
        string json = SessionState.GetString(BackupKey, ""); if (json == "") return;
        var b = JsonUtility.FromJson<Backup>(json);
        if (b.envelopeExists) PlayerPrefs.SetString("foundation.slot.3.saveData", b.envelope); else PlayerPrefs.DeleteKey("foundation.slot.3.saveData");
        if (b.createdExists) PlayerPrefs.SetInt("foundation.slot.3.created", b.created); else PlayerPrefs.DeleteKey("foundation.slot.3.created");
        if (b.selectedExists) PlayerPrefs.SetInt("foundation.selectedSaveSlot", b.selected); else PlayerPrefs.DeleteKey("foundation.selectedSaveSlot");
        PlayerPrefs.Save(); ResetCache();
        typeof(SaveSlotPrefs).GetField("_selected", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 0);
        SessionState.EraseString(BackupKey);
        Debug.Log("Building upgrade validation: original player save restored.");
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception("Building upgrades: " + message); }
    public static void Test()
    {
        Check(EditorApplication.isPlaying && SessionState.GetString(BackupKey, "") != "", "run Begin first");
        var manager = GlobalResourceManager.Instance;
        Check(manager != null, "resource manager exists");
        Check(BuildingUpgradeService.Capacity("housing") == 3 && !BuildingUpgradeService.CanAdmitResident, "initial civilian limit");
        foreach (var b in BuildingUpgradeService.Catalog.buildings)
        {
            Check(!BuildingUpgradeService.TryUpgrade(b.id), "cannot upgrade before construction");
            var construction = UnityEngine.Object.FindObjectsByType<BaseBuildingConstruction>(FindObjectsInactive.Include, FindObjectsSortMode.None).First(c => c.BuildingId == b.id);
            typeof(BaseBuildingConstruction).GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(construction, null);
            Check(BuildingUpgradeService.Capacity(b.id) == (b.id == "housing" ? 8 : 3), "construction capacity");
            var cost = b.levels[0];
            manager.SetResourceAmount(cost.resourceA, 0);
            int other = manager.GetResourceAmount(cost.resourceB);
            Check(!BuildingUpgradeService.TryUpgrade(b.id) && manager.GetResourceAmount(cost.resourceB) == other, "unaffordable purchase is inert");
            manager.SetResourceAmount(cost.resourceA, 100);
            for (int i = 0; i < b.levels.Count; i++)
            {
                int before = manager.GetResourceAmount(cost.resourceA);
                Check(BuildingUpgradeService.TryUpgrade(b.id), "purchase level " + (i + 1));
                Check(manager.GetResourceAmount(cost.resourceA) == before - b.levels[i].costA, "exact resource charge");
            }
            int remaining = manager.GetResourceAmount(cost.resourceA);
            Check(!BuildingUpgradeService.TryUpgrade(b.id) && manager.GetResourceAmount(cost.resourceA) == remaining, "maximum level does not charge");
            Check(BuildingUpgradeService.Capacity(b.id) == (b.id == "housing" ? 53 : 10), "maximum capacity");
        }
        ResetCache();
        Check(BuildingUpgradeService.Level("fort") == 7 && BuildingUpgradeService.Level("housing") == 9, "levels survive save reload");
        foreach (var view in UnityEngine.Object.FindObjectsByType<MilitaryTrainingView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var s = new SerializedObject(view);
            var warrior = (ResourceType)s.FindProperty("warrior").objectReferenceValue;
            var human = (ResourceType)s.FindProperty("human").objectReferenceValue;
            var weapon = (ResourceType)s.FindProperty("weapon").objectReferenceValue;
            manager.SetResourceAmount(human, 20); manager.SetResourceAmount(weapon, 20); manager.SetResourceAmount(warrior, 10);
            Check(!view.CanArmWarrior, "recruitment disabled at capacity"); view.Arm();
            Check(manager.GetResourceAmount(human) == 20 && manager.GetResourceAmount(weapon) == 20, "rejected recruitment does not spend");
            manager.SetResourceAmount(warrior, 9); Check(view.CanArmWarrior, "last slot available"); view.Arm();
            Check(manager.GetResourceAmount(warrior) == 10 && manager.GetResourceAmount(human) == 19, "last slot purchase");
        }
        foreach (var button in UnityEngine.Object.FindObjectsByType<BuildingUpgradeButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Check(button.GetComponent<CanvasGroup>().alpha == 0, "max upgrade hidden");
            Check(!button.GetComponent<Button>().interactable, "max upgrade disabled");
        }
        Debug.Log("PASS: construction, initial capacities, all 23 upgrades, costs, unaffordable/max guards, persisted levels, recruitment limits, prefab buttons.");
    }
}
#endif
