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
        var smith = Load<ContentUnlockDefinition>("Assets/Resources/Quests/BuildingUnlocks/blacksmith.asset");
        var wood = Load<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");
        var bow = Load<ResourceType>("Assets/Resources/ResourceTypes/Bow.asset");
        var table = Load<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");

        materials.title = "Дерево для стрельбища";
        materials.description = "Крыша над головой уже есть. Теперь людям нужно учиться защищать земли портала. Принеси десять связок дерева для стрельбища, и Стайк Пью подготовит всё для его строительства.";
        materials.inProgressStatus = "Принеси десять связок дерева для стрельбища";
        materials.requirements = new() { new QuestDefinition.Requirement { resource = wood, amount = 10, purpose = "Дерево для стрельбища", fulfilledByBuilding = "archery_range" } };
        materials.resourceRewards.Clear(); materials.unlockRewards = new() { range };
        materials.unlockRewardsOnCompletion = false; materials.consumeRequirementsOnClaim = false;

        build.title = "Построй стрельбище";
        build.description = "Поставь стрельбище из десяти связок дерева. Стайк Пью приберёг два лука для первых защитников и отдаст их, когда площадка будет готова.";
        build.inProgressStatus = "Построй стрельбище";
        build.requirements = new() { new QuestDefinition.Requirement { buildingId = "archery_range", amount = 1, buildingIcon = range.icon, purpose = "Готовое стрельбище" } };
        build.resourceRewards = new() { new QuestDefinition.ResourceReward { resource = bow, amount = 2 } };
        build.unlockRewards.Clear(); build.unlockRewardsOnCompletion = false;

        archers.description = "Два лука без дела не защитят даже капустную грядку. Найми двух лучников в стрельбище. С первой стражей можно будет взяться за кузницу и устроить лагерь для беженцев.";
        archers.inProgressStatus = "Найми двух лучников";
        archers.unlockRewards.RemoveAll(u => u == smith);
        archers.unlockRewards.Insert(0, smith);
        archers.unlockRewardsOnCompletion = false;

        Localize(table, "quest.archery_materials.title", materials.title, "Timber for the archery range");
        Localize(table, "quest.archery_materials.description", materials.description, "Our people have a roof. Now they must learn to defend the Portal Lands. Bring ten bundles of wood for an archery range, and Stike Pew will prepare everything needed to build it.");
        Localize(table, "quest.archery_materials.status", materials.inProgressStatus, "Bring ten bundles of wood for the archery range");
        Localize(table, "quest.archery_materials.purpose", materials.requirements[0].purpose, "Timber for the archery range");
        Localize(table, "quest.build_range.title", build.title, "A completed archery range");
        Localize(table, "quest.build_range.description", build.description, "Build the archery range with ten bundles of wood. Stike Pew has set aside two bows for our first defenders and will hand them over when the range is ready.");
        Localize(table, "quest.build_range.purpose", build.requirements[0].purpose, "A completed archery range");
        Localize(table, "quest.recruit_archers.new_description", archers.description, "Two idle bows cannot guard so much as a cabbage patch. Recruit two archers at the range. With our first watch in place, we can turn to a blacksmith and a camp for refugees.");
        Dialogue("Assets/Prefabs/Quests/02 Smithy Materials.prefab", materials, first,
            "Ну вот, первая ночь под крышей! Уже похоже на хозяйство, а не на привал. Теперь позаботимся о страже: в Тёмных землях не все рады соседям.",
            "There we are, our first night under a roof! Beginning to look like a settlement rather than a campsite. Time to think about a guard. Not everything in the Dark Lands welcomes neighbours.", table, "quest.archery_materials.dialogue", true);
        Dialogue("Assets/Prefabs/Quests/02 Smithy And Bows.prefab", build, materials,
            "Вот это дерево! Из такого выйдет славное стрельбище. Всё для строительства готово, осталось взяться за дело.",
            "Now that's good timber! It'll make a fine archery range. Everything is ready for building; we just need to get to work.", table, "quest.build_range.dialogue");
        Dialogue("Assets/Prefabs/Quests/02 Two Archers.prefab", archers, build,
            "Стрельбище готово! Держи обещанные два лука. Не новые, зато тетивы крепкие. И стреляют они всё ещё в ту сторону, куда целишься.",
            "The range is ready! Here are the two bows I promised. Not new, but the strings are sound. And they still shoot in the direction you aim.", table, "quest.recruit_archers.new_dialogue");

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
        Debug.Log("Post-housing chain: 10 wood -> unlock range -> build range -> 2 bows -> recruit 2 archers -> unlock smithy.");
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
        quest.title = "Построй кузницу";
        quest.description = "Защитников станет больше, и каждому понадобится оружие. Построй кузницу. Стайк Пью приготовит две телеги для носильщиков и поможет наладить научную лабораторию.";
        quest.inProgressStatus = "Построй кузницу";
        quest.requirements = new() { new QuestDefinition.Requirement { buildingId = "blacksmith", amount = 1, buildingIcon = smithy.icon, purpose = "Кузница для защитников" } };
        quest.resourceRewards = new() { new QuestDefinition.ResourceReward { resource = Load<ResourceType>("Assets/Resources/ResourceTypes/Cart.asset"), amount = 2 } };
        quest.unlockRewards = new() { laboratory }; quest.unlockRewardsOnCompletion = false;
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
            d.lines = QuestNarrativeRevision.Lines(d.id, new[] { "Два лучника на страже – уже спокойнее. Только на старых луках далеко не уедешь. Нашим людям понадобится своё оружие, а для него – кузница." });
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
        Localize(table, "quest.blacksmith_build.title", quest.title, "A forge for our defenders");
        Localize(table, "quest.blacksmith_build.description", quest.description, "Our guard will grow, and every defender will need a weapon. Build the blacksmith. Stike Pew will provide two carts for the porters and help establish a scientific laboratory.");
        Localize(table, "quest.blacksmith_build.purpose", quest.requirements[0].purpose, "A forge for our defenders");
        Localize(table, "quest.blacksmith_build.dialogue", dialogue.lines[0], "Two archers on watch. That's a comfort. But old bows will only take us so far. Our people need weapons of their own, and that means a blacksmith.");
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
    }
}
#endif
