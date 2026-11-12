#if UNITY_EDITOR
using System;
using System.Linq;
using GameFoundation.Quests;
using GameFoundation.Base;
using GameFoundation.Localization;
using UnityEditor;
using UnityEngine;

/// <summary>Updates the post-housing quest stages without replacing their persistent IDs.</summary>
public static class StartingQuestChainSetup
{
    public const string CraftPath = "Assets/Resources/Quests/Build Smithy And Two Bows.asset";
    static T Load<T>(string path) where T : UnityEngine.Object =>
        AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);
    static void Localize(LocalizationTable table, string key, string ru, string en)
    {
        var entry = table.entries.Find(e => e.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        while (entry.values.Count < table.languages.Count) entry.values.Add("");
        entry.values[table.languages.IndexOf("ru")] = ru;
        entry.values[table.languages.IndexOf("en")] = en;
    }
    static void Dialogue(string path, QuestDefinition quest, QuestDefinition prerequisite, string ru, string en,
        LocalizationTable table, string localizationKey, bool afterHousing = false)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var dialogue = root.GetComponent<QuestDialogueDefinition>();
            dialogue.lines = QuestNarrativeRevision.Lines(dialogue.id, new[] { ru });
            dialogue.quest = quest; dialogue.prerequisite = prerequisite; dialogue.prerequisiteAccepted = false;
            if (!afterHousing) { dialogue.prerequisiteNextDay = false; dialogue.requiredBuildingAfterNight = ""; }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Localize(table, localizationKey, ru, en);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode");
        var first = Load<QuestDefinition>("Assets/Resources/Quests/First Expedition.asset");
        var materials = Load<QuestDefinition>("Assets/Resources/Quests/Smithy Materials.asset");
        var build = Load<QuestDefinition>(CraftPath);
        var archers = Load<QuestDefinition>("Assets/Resources/Quests/Recruit Two Archers.asset");
        var range = Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/archery_range.asset");
        var barracks = Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/barracks.asset");
        var smith = Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/blacksmith.asset");
        var wood = Load<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");
        var slinger = Load<ResourceType>("Assets/Resources/ResourceTypes/Slinger.asset");
        var table = Load<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");

        materials.title = "Дерево для военного квартала";
        materials.description = "Принеси десять связок дерева для военного квартала.";
        materials.inProgressStatus = "Принеси десять связок дерева для военного квартала.";
        materials.requirements = new() { new QuestDefinition.Requirement { resource = wood, amount = 10, purpose = "Дерево для военного квартала", fulfilledByBuilding = "barracks" } };
        materials.resourceRewards = new() { new QuestDefinition.ResourceReward { resource = wood, amount = 12 } };
        materials.unlockRewards = new() { barracks };
        materials.unlockRewardsOnCompletion = false; materials.consumeRequirementsOnClaim = false;

        build.title = "Построй военный квартал";
        build.description = "Построй военный квартал на карте базы за 12 дерева. Казарма откроется сразу.";
        build.inProgressStatus = "Построй военный квартал";
        build.requirements = new() { new QuestDefinition.Requirement { buildingId = "barracks", amount = 1, buildingIcon = barracks.icon, purpose = "Готовый военный квартал" } };
        build.resourceRewards.Clear();
        build.unlockRewards.Clear(); build.unlockRewardsOnCompletion = false;

        archers.title = "Первые пращники";
        archers.description = "Открой военный квартал, выбери \"Казарма\" и \"Пращники\". Нажми \"Вооружить\" дважды. Для каждого пращника нужен один свободный житель.";
        archers.inProgressStatus = "Найми двух пращников";
        archers.requirements = new() { new QuestDefinition.Requirement { resource = slinger, amount = 2, purpose = "Два пращника на страже" } };
        archers.resourceRewards.Clear();
        archers.unlockRewards = new()
        {
            smith,
            Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/magic_library.asset"),
            range
        };
        archers.unlockRewardsOnCompletion = false;

        Localize(table, "quest.archery_materials.title", materials.title, "Wood for the Military Quarter");
        Localize(table, "quest.archery_materials.description", materials.description, "Bring ten bundles of wood for the Military Quarter.");
        Localize(table, "quest.archery_materials.status", materials.inProgressStatus, "Bring ten bundles of wood for the Military Quarter");
        Localize(table, "quest.archery_materials.purpose", materials.requirements[0].purpose, "Wood for the Military Quarter");
        Localize(table, "quest.build_range.title", build.title, "A completed Military Quarter");
        Localize(table, "quest.build_range.description", build.description, "Build the Military Quarter on the base map for 12 wood. Its barracks opens immediately.");
        Localize(table, "quest.build_range.purpose", build.requirements[0].purpose, "A completed Military Quarter");
        Localize(table, "quest.recruit_archers.new_description", archers.description, "Open the Military Quarter, select Barracks and Slingers, then press Arm twice. Each slinger needs one free resident.");
        Dialogue("Assets/Prefabs/Quests/02 Smithy Materials.prefab", materials, first,
            "Ну вот, первая ночь позади. Уже похоже на хозяйство, а не на привал. Теперь позаботимся о страже.",
            "The first night is behind us. This is starting to look like a settlement rather than a campsite. Now let's see to the guard.", table, "quest.archery_materials.dialogue", true);
        Dialogue("Assets/Prefabs/Quests/02 Smithy And Bows.prefab", build, materials,
            "Из этого дерева выйдет славная казарма. Всё готово, осталось взяться за дело.",
            "This timber will make a fine barracks. Everything is ready to get to work.", table, "quest.build_range.dialogue");
        Dialogue("Assets/Prefabs/Quests/02 Two Archers.prefab", archers, build,
            "Казарма готова! Начнём с пращников – они смогут прикрывать наших людей издалека.",
            "The barracks are ready! Start with slingers. They can cover our people from afar.", table, "quest.recruit_archers.new_dialogue");

        // Update construction sources and variants, including authored overrides.
        foreach (string path in new[] {
            "Assets/Prefabs/Base/Buttons/Buildings/Archery Range Button.prefab",
            "Assets/Prefabs/Base/Islands/Archery Range Island.prefab",
            "Assets/Prefabs/Base/Variants/Base Location Variant.prefab",
            "Assets/Prefabs/Base/Variants/Base Islands Backup.prefab",
            "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (var building in root.GetComponentsInChildren<BaseBuildingConstruction>(true))
                {
                    if (building.BuildingId != "archery_range") continue;
                    var s = new SerializedObject(building);
                    s.FindProperty("wood").objectReferenceValue = wood;
                    s.FindProperty("woodCost").intValue = 10;
                    s.FindProperty("stoneCost").intValue = 0;
                    s.FindProperty("requiredUnlock").objectReferenceValue = range;
                    s.ApplyModifiedPropertiesWithoutUndo(); changed = true;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var asset in new UnityEngine.Object[] { materials, build, archers, table }) EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        InstallBlacksmithQuest();
        Debug.Log("Post-housing chain: 10 wood -> unlock and build Military Quarter -> train 2 slingers -> Craft Quarter, Magic Tower and Archery Range.");
    }
    public static void InstallBlacksmithQuest()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode");
        const string questPath = "Assets/Resources/Quests/Build Blacksmith.asset";
        const string dialoguePath = "Assets/Prefabs/Quests/03 Build Blacksmith.prefab";
        var quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(questPath);
        if (quest == null)
        {
            quest = ScriptableObject.CreateInstance<QuestDefinition>(); quest.id = "build_blacksmith";
            AssetDatabase.CreateAsset(quest, questPath);
        }
        var laboratory = Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/laboratory.asset");
        var smithy = Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/blacksmith.asset");
        var archers = Load<QuestDefinition>("Assets/Resources/Quests/Recruit Two Archers.asset");
        quest.title = "Построй ремесленный квартал";
        quest.description = "Защитникам понадобится оружие. Построй ремесленный квартал. В столярном цехе можно делать копья, луки и телеги. За постройку Стайк Пью даст две телеги и откроет строительство научной лаборатории и лагеря беженцев.";
        quest.inProgressStatus = "Построй ремесленный квартал";
        quest.requirements = new() { new QuestDefinition.Requirement { buildingId = "blacksmith", amount = 1, buildingIcon = smithy.icon, purpose = "Ремесленный квартал" } };
        quest.resourceRewards = new() { new QuestDefinition.ResourceReward { resource = Load<ResourceType>("Assets/Resources/ResourceTypes/Cart.asset"), amount = 2 } };
        quest.unlockRewards = new() { laboratory, Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/refugees.asset") }; quest.unlockRewardsOnCompletion = false;
        quest.consumeRequirementsOnClaim = false;
        EditorUtility.SetDirty(quest);
        var catalog = Load<QuestCatalog>("Assets/Resources/Quests/Quest Catalog.asset");
        var quests = catalog.quests.Where(q => q != quest).ToList();
        quests.Insert(quests.IndexOf(archers) + 1, quest); catalog.quests = quests.ToArray(); EditorUtility.SetDirty(catalog);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(dialoguePath) == null)
            AssetDatabase.CopyAsset("Assets/Prefabs/Quests/02 Smithy And Bows.prefab", dialoguePath);
        var root = PrefabUtility.LoadPrefabContents(dialoguePath);
        try
        {
            root.name = "03 Build Blacksmith";
            var d = root.GetComponent<QuestDialogueDefinition>(); d.id = "pew_build_blacksmith";
            d.quest = quest; d.prerequisite = archers; d.prerequisiteAccepted = false; d.prerequisiteNextDay = false; d.requiredBuildingAfterNight = "";
            d.lines = QuestNarrativeRevision.Lines(d.id, new[] { "Первые защитники уже на посту. Теперь нам нужна кузница, чтобы делать своё оружие." });
            PrefabUtility.SaveAsPrefabAsset(root, dialoguePath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var dialogue = Load<GameObject>(dialoguePath).GetComponent<QuestDialogueDefinition>();
        root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab");
        try
        {
            var s = new SerializedObject(root.GetComponent<QuestDialogueView>()); var list = s.FindProperty("conversations");
            bool exists = false; for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == dialogue) exists = true;
            if (!exists)
            {
                int at = list.arraySize; for (int i = 0; i < list.arraySize; i++)
                    if ((list.GetArrayElementAtIndex(i).objectReferenceValue as QuestDialogueDefinition)?.quest?.parallelQuest == true) { at = i; break; }
                list.InsertArrayElementAtIndex(at); list.GetArrayElementAtIndex(at).objectReferenceValue = dialogue;
                s.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Quests/00 Royal Tribute.prefab");
        try { root.GetComponent<QuestDialogueDefinition>().prerequisite = quest; PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Quests/00 Royal Tribute.prefab"); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var table = Load<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Localize(table, "quest.blacksmith_build.title", quest.title, "Craft Quarter");
        Localize(table, "quest.blacksmith_build.description", quest.description, "Build the Craft Quarter. The carpentry workshop makes spears, bows and carts. Pew will give you two carts and unlock the Science Laboratory and Refugee Camp.");
        Localize(table, "quest.blacksmith_build.purpose", quest.requirements[0].purpose, "Craft Quarter");
        Localize(table, "quest.blacksmith_build.dialogue", dialogue.lines[0], "Our first defenders are on watch. Build the Craft Quarter so we can make our own weapons in the carpentry workshop.");
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
    }
}
#endif
