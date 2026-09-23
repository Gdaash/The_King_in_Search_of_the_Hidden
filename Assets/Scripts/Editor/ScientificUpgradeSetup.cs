using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

internal static class ScientificUpgradeSetup
{
    private const string TablePath = "Assets/Resources/ScientificUpgradeTable.asset";
    private const string TreePath = "Assets/Prefabs/Base/Laboratory Skill Tree.prefab";
    private const string ButtonPath = "Assets/Prefabs/UI/SkillButton.prefab";
    private const string ArrowPath = "Assets/Prefabs/UI/ArrowSkills.prefab";

    [MenuItem("Tools/Таблицы/Улучшения/Обновить дерево на сцене")]
    private static void RebuildFromMenu() => Run();

    private static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        try
        {
            EnsureTable();
            RebuildTree();
            ConfigurePrefab("Assets/Prefabs/Buildings/Башня-портал.prefab", root =>
            {
                root.name = "Башня-портал";
            });
            RenamePortalPrefabAndSceneInstance();
            AssetDatabase.SaveAssets();
            Debug.Log("[ScientificUpgradeSetup] Scientific upgrade tree configured.");
        }
        catch (Exception error) { Debug.LogException(error); }
    }

    private static void EnsureTable()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var table = AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>(TablePath);
        if (table == null) { table = ScriptableObject.CreateInstance<ScientificUpgradeTable>(); AssetDatabase.CreateAsset(table, TablePath); }
        var ore = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Prefabs/Resources/MagicOre.asset");
        var stone = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Prefabs/Resources/Stone.asset");
        AddIfMissing(table, ScientificUpgrades.PortalArrows, "", "Магические стрелы", "Кристалл на башне портала теперь может стрелять.", ore, 1, 1f);
        AddIfMissing(table, ScientificUpgrades.StrongWalls, ScientificUpgrades.PortalArrows, "Крепкие стены", "Повышает здоровье башни портала на 20%.", stone, 2, 1.2f);
        AddIfMissing(table, ScientificUpgrades.FastHex, ScientificUpgrades.PortalArrows, "Уменьшение времени на открытие гекса на 50%", "Уменьшает время открытия гекса на 50%.", ore, 5, 0.5f);
        AddIfMissing(table, ScientificUpgrades.SharpAxes, ScientificUpgrades.PortalArrows, "Заточить топоры", "Уменьшает время рубки леса на 20%.", stone, 2, 0.8f);
        AddIfMissing(table, ScientificUpgrades.QuietScouting, ScientificUpgrades.PortalArrows, "Бесшумная разведка", "Уменьшает на 1 количество тревоги при открытии гекса.", ore, 1, 1f);
        for (int i = 0; i < ScientificUpgrades.Flashlights.Length; i++)
            AddIfMissing(table, ScientificUpgrades.Flashlights[i], i == 0 ? ScientificUpgrades.StrongWalls : ScientificUpgrades.Flashlights[i - 1],
                "Фонарь " + (i + 2), "Открывает " + (i + 2) + "-й фонарь для работы с гексами.", ore, (i + 1) * 5, 1f);
        EditorUtility.SetDirty(table);
    }

    private static void AddIfMissing(ScientificUpgradeTable table, string id, string parentId, string title,
        string description, ResourceType resource, int cost, float value)
    {
        if (table.Find(id) != null) return;
        table.entries.Add(new ScientificUpgradeTable.Entry { id = id, parentId = parentId, title = title,
            description = description, costResource = resource, cost = cost, effectValue = value });
    }

    private static void RebuildTree()
    {
        var root = PrefabUtility.LoadPrefabContents(TreePath);
        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
            if (source == null) throw new FileNotFoundException(ButtonPath);
            string[] ids = { ScientificUpgrades.PortalArrows, ScientificUpgrades.StrongWalls, ScientificUpgrades.FastHex,
                ScientificUpgrades.SharpAxes, ScientificUpgrades.QuietScouting,
                ScientificUpgrades.Flashlights[0], ScientificUpgrades.Flashlights[1], ScientificUpgrades.Flashlights[2],
                ScientificUpgrades.Flashlights[3], ScientificUpgrades.Flashlights[4] };
            Vector2[] places = { Vector2.zero, new(0, 230), new(270, 0), new(0, -230), new(-270, 0),
                new(0, 460), new(0, 690), new(0, 920), new(0, 1150), new(0, 1380) };
            int[] parentIndexes = { -1, 0, 0, 0, 0, 1, 5, 6, 7, 8 };
            var buttons = new SkillButton[ids.Length];
            var table = AssetDatabase.LoadAssetAtPath<ScientificUpgradeTable>(TablePath);
            var arrowSource = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowPath);
            if (arrowSource == null) throw new FileNotFoundException(ArrowPath);
            var arrows = new SkillLine[ids.Length];
            for (int i = 1; i < ids.Length; i++)
            {
                var arrow = (GameObject)PrefabUtility.InstantiatePrefab(arrowSource, root.transform);
                arrow.name = "Стрелка → " + ids[i];
                var line = arrow.GetComponent<RectTransform>();
                line.anchorMin = line.anchorMax = line.pivot = new Vector2(.5f, .5f);
                Vector2 from = places[parentIndexes[i]];
                Vector2 direction = places[i] - from;
                line.anchoredPosition = (from + places[i]) * .5f;
                line.localScale = Vector3.one;
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f);
                var image = arrow.GetComponent<UnityEngine.UI.Image>();
                image.preserveAspect = true;
                line.sizeDelta = image.sprite.rect.size;
                image.raycastTarget = false;
                arrows[i] = arrow.GetComponent<SkillLine>();
            }
            for (int i = 0; i < ids.Length; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                instance.name = ids[i] + " " + table.Find(ids[i]).title;
                var rect = instance.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = places[i];
                rect.localScale = Vector3.one * 1.5f;
                var skill = instance.GetComponent<SkillButton>();
                var entry = table.Find(ids[i]);
                skill.skillID = ids[i]; skill.title = entry.title; skill.description = entry.description;
                skill.cost = entry.cost; skill.isUnlocked = i == 0; skill.isPurchased = false;
                var serialized = new SerializedObject(skill);
                serialized.FindProperty("purchaseResourceType").objectReferenceValue = entry.costResource;
                serialized.FindProperty("upgradeStats").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var icon = instance.transform.Find("CostIconBacking/CostResourceIcon")?.GetComponent<UnityEngine.UI.Image>();
                if (icon != null && entry.costResource != null)
                {
            ResourceIconSizing.Apply(icon, entry.costResource.resourceIcon);
                }
                buttons[i] = skill;
            }
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].nextSkills = Enumerable.Range(1, ids.Length - 1)
                    .Where(child => parentIndexes[child] == i).Select(child => buttons[child]).ToArray();
            for (int i = 1; i < arrows.Length; i++)
            {
                var serialized = new SerializedObject(arrows[i]);
                serialized.FindProperty("parentSkill").objectReferenceValue = buttons[parentIndexes[i]];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var treeRect = root.GetComponent<RectTransform>();
            treeRect.pivot = new Vector2(.5f, .5f);
            treeRect.anchoredPosition = Vector2.zero;
            treeRect.sizeDelta = new Vector2(1900, 3300);
            PrefabUtility.SaveAsPrefabAsset(root, TreePath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ConfigurePrefab(string path, Action<GameObject> configure)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { configure(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void RenamePortalPrefabAndSceneInstance()
    {
        const string oldPath = "Assets/Prefabs/Buildings/Castle.prefab";
        const string newPath = "Assets/Prefabs/Buildings/Башня-портал.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(oldPath) != null)
        {
            string error = AssetDatabase.MoveAsset(oldPath, newPath);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
        }
        var world = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/World.unity");
        bool opened = !world.IsValid() || !world.isLoaded;
        if (opened) world = EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Additive);
        foreach (var root in world.GetRootGameObjects())
            if (root.name == "Castle") { root.name = "Башня-портал"; EditorUtility.SetDirty(root); }
        EditorSceneManager.SaveScene(world);
        if (opened && UnityEngine.SceneManagement.SceneManager.sceneCount > 1)
            EditorSceneManager.CloseScene(world, true);
    }
}
