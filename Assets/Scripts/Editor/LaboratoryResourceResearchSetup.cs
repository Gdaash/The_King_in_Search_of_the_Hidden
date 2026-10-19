#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEngine;

/// <summary>Authors resource research without changing existing purchase IDs or saved progress.</summary>
public static class LaboratoryResourceResearchSetup
{
    private static readonly Dictionary<string, (string title, string description)> english = new();
    private static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UnitStats/" + name + ".png");

    public static void ConfigureCatalog(ScientificUpgradeTable table)
    {
        // These belong to expedition progression / decrees, not permanent laboratory research.
        table.entries.RemoveAll(e => e.id == ScientificUpgrades.PortalArrows || e.id == ScientificUpgrades.WarriorRetreat || ScientificUpgrades.Flashlights.Contains(e.id));
        english.Clear();
        var stone = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Stone.asset");
        var ore = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/MagicOre.asset");
        void Add(string id, string title, string enTitle, string description, string enDescription, int gate, float value, int price, ResourceType resource, string icon)
        {
            var e = table.Find(id);
            if (e == null) { e = new ScientificUpgradeTable.Entry { id = id, cost = price, costResource = resource }; table.entries.Add(e); }
            e.groupId = id; e.groupTitle = e.title = title; e.description = description;
            e.parentId = ""; e.level = 1; e.requiredPurchases = gate; e.effectValue = value;
            if (e.icon == null) e.icon = Icon(icon);
            english[id] = (enTitle, enDescription);
        }
        Add(ScientificUpgrades.SharpAxes, "Острые топоры", "Sharp axes", "Уменьшает время добычи дерева на 20%.", "Reduces wood gathering time by 20%.", 0, .8f, 8, stone, "Sword");
        Add(ScientificUpgrades.QuietScouting, "Тихая разведка", "Quiet scouting", "Уменьшает опасность открытия гекса на 1 череп. Не менее 1 черепа.", "Reduces hex exploration danger by 1 skull, to a minimum of 1 skull.", 0, 1, 4, ore, "Eye");
        Add(ScientificUpgrades.StrongWalls, "Крепкие стены", "Strong walls", "Повышает здоровье башни портала на 20%.", "Increases portal tower health by 20%.", 2, 1.2f, 8, stone, "Shield");
        Add(ScientificUpgrades.SharpKnives, "Острые ножи", "Sharp knives", "Уменьшает время добычи еды на 20%.", "Reduces food gathering time by 20%.", 2, .8f, 8, stone, "Sword");
        Add(ScientificUpgrades.SharpPickaxes, "Острые кирки", "Sharp pickaxes", "Уменьшает время добычи камня на 20%.", "Reduces stone gathering time by 20%.", 2, .8f, 8, stone, "Sword");
        Add(ScientificUpgrades.QuietAxes, "Тихие топоры", "Quiet axes", "Уменьшает опасность каждого цикла добычи дерева на 1 череп. Не менее 1 черепа.", "Reduces danger per wood gathering cycle by 1 skull, to a minimum of 1 skull.", 2, 1, 4, ore, "Eye");
        Add(ScientificUpgrades.QuietHunting, "Тихая охота", "Quiet hunting", "Уменьшает опасность каждого цикла добычи еды на 1 череп. Не менее 1 черепа.", "Reduces danger per food gathering cycle by 1 skull, to a minimum of 1 skull.", 5, 1, 8, ore, "Eye");
        Add(ScientificUpgrades.QuietQuarries, "Тихие каменоломни", "Quiet quarries", "Уменьшает опасность каждого цикла добычи камня на 1 череп. Не менее 1 черепа.", "Reduces danger per stone gathering cycle by 1 skull, to a minimum of 1 skull.", 5, 1, 8, ore, "Eye");
        Add(ScientificUpgrades.HardenedPickaxes, "Закалённые кирки", "Hardened pickaxes", "Уменьшает время добычи железной руды на 20%.", "Reduces iron ore gathering time by 20%.", 5, .8f, 12, stone, "Sword");
        Add(ScientificUpgrades.FastHex, "Быстрая разведка", "Fast scouting", "Уменьшает время открытия гекса на 50%.", "Reduces hex exploration time by 50%.", 5, .5f, 20, ore, "Hourglass");
        Add(ScientificUpgrades.QuietMines, "Тихие шахты", "Quiet mines", "Уменьшает опасность каждого цикла добычи железной и магической руды на 1 череп. Не менее 1 черепа.", "Reduces danger per iron and magic ore gathering cycle by 1 skull, to a minimum of 1 skull.", 8, 1, 12, ore, "Eye");
        Add(ScientificUpgrades.CrystalCutters, "Кристальные резцы", "Crystal cutters", "Уменьшает время добычи магической руды на 20%.", "Reduces magic ore gathering time by 20%.", 8, .8f, 20, ore, "Magic");
        Add(ScientificUpgrades.WarriorBaseRegen, "Полевой лазарет", "Field infirmary", "Воины у портала восстанавливают 1% здоровья в секунду.", "Warriors at the portal recover 1% of maximum health per second.", 8, .01f, 20, ore, "Healing");
        for (int level = 1; level <= 5; level++)
        {
            string id = ScientificUpgrades.CrystalEfficiency + "." + level;
            int percent = level * 10;
            Add(id, "Кристальная настройка " + level, "Crystal attunement " + level,
                $"Уменьшает требование кристаллов для каждого уровня башни ещё на 10%. Всего: −{percent}%. Округление вверх, минимум 1 кристалл.",
                $"Reduces crystals required for every tower level by another 10%. Total reduction: {percent}%. Rounded up, minimum 1 crystal.",
                2 + (level - 1) * 3, .1f, level * 4, ore, "Magic");
            var e = table.Find(id); e.groupId = ScientificUpgrades.CrystalEfficiency; e.groupTitle = "Кристальная настройка"; e.level = level;
        }
        table.entries = table.entries.OrderBy(e => e.requiredPurchases).ThenBy(e => e.GroupId, StringComparer.Ordinal).ThenBy(e => e.level).ToList();
        LaboratoryListSetup.ValidateEntries(table.entries);
        EditorUtility.SetDirty(table);
        Localize(table);
    }

    public static void Localize(ScientificUpgradeTable table)
    {
        var loc = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        void Set(string key, string ru, string en)
        {
            var entry = loc.entries.Find(e => e.key == key);
            if (entry == null) { entry = new LocalizationTable.Entry { key = key }; loc.entries.Add(entry); }
            while (entry.values.Count < loc.languages.Count) entry.values.Add("");
            for (int i = 0; i < loc.languages.Count; i++) if (loc.languages[i] == "ru" || loc.languages[i] == "en") entry.values[i] = loc.languages[i] == "ru" ? ru : en;
        }
        foreach (var e in table.entries)
        {
            if (!english.TryGetValue(e.id, out var translated)) continue;
            Set("skill." + e.id + ".description", e.description, translated.description);
            Set("skill." + e.id + ".title", e.title, translated.title);
            Set("laboratory.group." + e.GroupId, e.groupTitle, e.GroupId == ScientificUpgrades.CrystalEfficiency ? "Crystal attunement" : translated.title);
        }
        EditorUtility.SetDirty(loc);
    }

    [MenuItem("Tools/Those UnderHex/Наука/Настроить исследования ресурсов")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var source = AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");
        ConfigureCatalog(source.UpgradeTable);
        var profiles = new Dictionary<ScientificUpgrades.GatheringResource, GlobalStats>();
        foreach (ScientificUpgrades.GatheringResource kind in Enum.GetValues(typeof(ScientificUpgrades.GatheringResource)))
        {
            if (kind == ScientificUpgrades.GatheringResource.None) continue;
            string path = kind == ScientificUpgrades.GatheringResource.Wood ? "Assets/Resources/Builds/Forest3.asset" : kind == ScientificUpgrades.GatheringResource.Food ? "Assets/Resources/Builds/Food Animals.asset" : "Assets/Resources/Builds/" + kind + " Gathering.asset";
            var stats = AssetDatabase.LoadAssetAtPath<GlobalStats>(path);
            if (stats == null) { stats = ScriptableObject.CreateInstance<GlobalStats>(); AssetDatabase.CreateAsset(stats, path); }
            var so = new SerializedObject(stats);
            so.FindProperty("gatheringResource").enumValueIndex = (int)kind;
            so.FindProperty("scientificProgressStats").objectReferenceValue = source;
            so.FindProperty("applySharpAxes").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(stats); profiles[kind] = stats;
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Buildings" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var kind = path.Contains("/Forest ") ? ScientificUpgrades.GatheringResource.Wood : path.Contains("/Animals/") ? ScientificUpgrades.GatheringResource.Food : path.Contains("/Stone ") ? ScientificUpgrades.GatheringResource.Stone : path.EndsWith("/Iron Ore Mine.prefab") ? ScientificUpgrades.GatheringResource.IronOre : path.EndsWith("/Magic Ore Mine.prefab") ? ScientificUpgrades.GatheringResource.MagicOre : ScientificUpgrades.GatheringResource.None;
            if (kind == ScientificUpgrades.GatheringResource.None) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var timer in root.GetComponentsInChildren<TimerController>(true))
                {
                    var emitters = timer.GetComponents<AlarmEmitter>();
                    if (emitters.Length == 0) continue; // Leave construction / recovery timers unchanged.
                    var t = new SerializedObject(timer); t.FindProperty("stats").objectReferenceValue = profiles[kind]; t.ApplyModifiedPropertiesWithoutUndo();
                    foreach (var emitter in emitters) { var e = new SerializedObject(emitter); e.FindProperty("productionStats").objectReferenceValue = profiles[kind]; e.ApplyModifiedPropertiesWithoutUndo(); }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        string progressionPath = "Assets/Prefabs/UI/Portal Tower Progression.prefab";
        var progression = PrefabUtility.LoadPrefabContents(progressionPath);
        try { var so = new SerializedObject(progression.GetComponent<PortalTowerProgression>()); so.FindProperty("scientificStats").objectReferenceValue = source; so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(progression, progressionPath); }
        finally { PrefabUtility.UnloadPrefabContents(progression); }
        LaboratoryListSetup.RefreshPopupCatalog(source.UpgradeTable);
        AssetDatabase.SaveAssets();
        Debug.Log("Resource research configured: 14 groups / 18 levels; gathering danger minimum 1 skull.");
    }
}
#endif
