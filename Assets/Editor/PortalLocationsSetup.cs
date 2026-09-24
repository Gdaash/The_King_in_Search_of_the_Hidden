#if UNITY_EDITOR
using System.Collections.Generic;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PortalLocationsSetup
{
    private const string LocationsFolder = "Assets/Resources/PortalLocations";
    private const string MapPath = "Assets/Prefabs/Base/Global Map.prefab";
    private const string TooltipPath = "Assets/Prefabs/Base/Portal Location Tooltip.prefab";

    private sealed class Definition
    {
        public string id, ruName, enName, ruDescription, enDescription;
        public int difficulty, cost;
        public (string resource, string abundance)[] resources;
    }

    private static readonly Definition[] Definitions =
    {
        New("forest", 1, 0, "Лес", "Forest", "Базовая лесная локация.", "The basic forest location.",
            ("Wood", "many"), ("Berry", "many"), ("Stone", "few"), ("MagicOre", "almost_none")),
        New("mountains", 2, 10, "Горы", "Mountains", "Каменистая горная локация с залежами руды.", "A rocky mountain location rich in ore.",
            ("Stone", "many"), ("IronOre", "many"), ("Wood", "few"), ("MagicOre", "rare")),
        New("steppes", 3, 15, "Степи", "Steppes", "Открытая степная локация с редкими рощами.", "An open steppe with sparse groves.",
            ("Berry", "many"), ("Wood", "average"), ("Stone", "few"), ("MagicOre", "rare")),
        New("swamps", 4, 20, "Болота", "Swamps", "Опасная болотистая локация с вязкими тропами.", "A dangerous swamp crossed by treacherous paths.",
            ("Berry", "many"), ("Wood", "average"), ("Stone", "few"), ("MagicOre", "sometimes")),
        New("magic_mountains", 5, 25, "Магические горы", "Magic Mountains", "Самая опасная локация, насыщенная магической рудой.", "The most dangerous location, rich in magic ore.",
            ("MagicOre", "many"), ("Stone", "many"), ("IronOre", "average"), ("Wood", "almost_none"))
    };

    [MenuItem("Tools/Game Setup/Rebuild Fixed Portal Locations")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(LocationsFolder)) AssetDatabase.CreateFolder("Assets/Resources", "PortalLocations");
        foreach (Definition definition in Definitions) CreateLocation(definition);
        ConfigureMap();
        AddLocalization();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Definition New(string id, int difficulty, int cost, string ruName, string enName,
        string ruDescription, string enDescription, params (string resource, string abundance)[] resources) =>
        new() { id = id, difficulty = difficulty, cost = cost, ruName = ruName, enName = enName,
            ruDescription = ruDescription, enDescription = enDescription, resources = resources };

    private static void CreateLocation(Definition definition)
    {
        string path = $"{LocationsFolder}/{definition.difficulty:00} {definition.id}.asset";
        PortalLocationDefinition asset = AssetDatabase.LoadAssetAtPath<PortalLocationDefinition>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<PortalLocationDefinition>();
            AssetDatabase.CreateAsset(asset, path);
        }
        SerializedObject serialized = new(asset);
        serialized.FindProperty("locationId").stringValue = definition.id;
        serialized.FindProperty("difficulty").intValue = definition.difficulty;
        serialized.FindProperty("activationCost").intValue = definition.cost;
        serialized.FindProperty("nameKey").stringValue = $"base.portal.location.{definition.id}.name";
        serialized.FindProperty("fallbackName").stringValue = definition.ruName;
        serialized.FindProperty("descriptionKey").stringValue = $"base.portal.location.{definition.id}.description";
        serialized.FindProperty("fallbackDescription").stringValue = definition.ruDescription;
        SerializedProperty resources = serialized.FindProperty("resources");
        resources.arraySize = definition.resources.Length;
        for (int i = 0; i < definition.resources.Length; i++)
        {
            SerializedProperty row = resources.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("resource").objectReferenceValue = LoadResource(definition.resources[i].resource);
            row.FindPropertyRelative("abundanceKey").stringValue = $"base.portal.abundance.{definition.resources[i].abundance}";
            row.FindPropertyRelative("fallbackAbundance").stringValue = AbundanceRu(definition.resources[i].abundance);
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    private static void ConfigureMap()
    {
        GameObject tooltipPrefab = CreateTooltipPrefab();
        GameObject root = PrefabUtility.LoadPrefabContents(MapPath);
        try
        {
            Sprite portalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Builds/PortalAnimation/PortalFrame_11.png");
            Transform sites = root.transform.Find("Portal Sites");
            Vector2[] positions =
            {
                new(-560f, 185f), new(0f, 255f), new(560f, 185f), new(-370f, -205f), new(370f, -205f)
            };
            for (int i = 0; i < Definitions.Length; i++)
            {
                Transform portal = sites.Find($"Portal Site {i + 1}");
                if (portal == null) continue;
                portal.gameObject.SetActive(true);
                portal.name = $"Portal {Definitions[i].ruName}";
                RectTransform rect = (RectTransform)portal;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = positions[i];
                PortalSiteButtonView view = portal.GetComponent<PortalSiteButtonView>();
                SerializedObject viewObject = new(view);
                viewObject.FindProperty("locationId").stringValue = Definitions[i].id;
                Image portalIcon = viewObject.FindProperty("portalIcon").objectReferenceValue as Image;
                if (portalIcon != null)
                {
                    SerializedObject iconObject = new(portalIcon);
                    iconObject.FindProperty("m_Sprite").objectReferenceValue = portalSprite;
                    iconObject.FindProperty("m_PreserveAspect").boolValue = true;
                    iconObject.FindProperty("m_RaycastTarget").boolValue = false;
                    iconObject.ApplyModifiedPropertiesWithoutUndo();
                    if (portalSprite != null) portalIcon.rectTransform.sizeDelta = portalSprite.rect.size * 2f;
                    EditorUtility.SetDirty(portalIcon);
                }
                viewObject.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform search = root.transform.Find("Search Portals");
            if (search != null) search.gameObject.SetActive(false);
            Transform oldTooltip = root.transform.Find("Portal Location Tooltip");
            if (oldTooltip != null) Object.DestroyImmediate(oldTooltip.gameObject);
            GameObject tooltip = PrefabUtility.InstantiatePrefab(tooltipPrefab) as GameObject;
            tooltip.name = "Portal Location Tooltip";
            tooltip.transform.SetParent(root.transform, false);
            tooltip.transform.SetAsLastSibling();
            PrefabUtility.SaveAsPrefabAsset(root, MapPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static GameObject CreateTooltipPrefab()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
        Sprite backgroundSprite = FindSprite("PopupPanel9Slice");
        Sprite skull = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/DangerSkull.png");
        GameObject root = NewRect("Portal Location Tooltip", null, Vector2.zero, new Vector2(620f, 370f));
        Image background = root.AddComponent<Image>();
        background.sprite = backgroundSprite;
        background.type = Image.Type.Sliced;
        background.raycastTarget = false;
        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        Text title = CreateText(root.transform, "Title", new Vector2(0f, 145f), new Vector2(550f, 48f), font, 27, TextAnchor.MiddleCenter);
        Text description = CreateText(root.transform, "Description", new Vector2(0f, 82f), new Vector2(540f, 62f), font, 18, TextAnchor.UpperCenter);
        description.horizontalOverflow = HorizontalWrapMode.Wrap;
        Text danger = CreateText(root.transform, "Danger", new Vector2(-150f, 24f), new Vector2(210f, 42f), font, 19, TextAnchor.MiddleRight);

        var skulls = new List<Image>();
        for (int i = 0; i < 5; i++)
        {
            GameObject skullObject = NewRect($"Danger Skull {i + 1}", root.transform, new Vector2(-15f + i * 50f, 24f), new Vector2(48f, 48f));
            Image image = skullObject.AddComponent<Image>();
            image.sprite = skull;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            if (skull != null) image.rectTransform.sizeDelta = skull.rect.size * 2f;
            skulls.Add(image);
        }

        var rows = new List<(GameObject root, Image icon, Text text)>();
        Vector2[] rowPositions = { new(-155f, -55f), new(155f, -55f), new(-155f, -125f), new(155f, -125f) };
        for (int i = 0; i < rowPositions.Length; i++)
        {
            GameObject row = NewRect($"Resource {i + 1}", root.transform, rowPositions[i], new Vector2(280f, 58f));
            GameObject iconObject = NewRect("Icon", row.transform, new Vector2(-92f, 0f), new Vector2(48f, 48f));
            Image icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Text amount = CreateText(row.transform, "Abundance", new Vector2(32f, 0f), new Vector2(185f, 48f), font, 18, TextAnchor.MiddleLeft);
            rows.Add((row, icon, amount));
        }

        PortalLocationTooltip tooltip = root.AddComponent<PortalLocationTooltip>();
        SerializedObject serialized = new(tooltip);
        serialized.FindProperty("group").objectReferenceValue = group;
        serialized.FindProperty("panel").objectReferenceValue = root.GetComponent<RectTransform>();
        serialized.FindProperty("title").objectReferenceValue = title;
        serialized.FindProperty("description").objectReferenceValue = description;
        serialized.FindProperty("dangerLabel").objectReferenceValue = danger;
        serialized.FindProperty("offset").vector2Value = new Vector2(430f, 0f);
        SerializedProperty skullArray = serialized.FindProperty("dangerSkulls");
        skullArray.arraySize = skulls.Count;
        for (int i = 0; i < skulls.Count; i++) skullArray.GetArrayElementAtIndex(i).objectReferenceValue = skulls[i];
        SerializedProperty rowArray = serialized.FindProperty("resourceRows");
        rowArray.arraySize = rows.Count;
        for (int i = 0; i < rows.Count; i++)
        {
            SerializedProperty row = rowArray.GetArrayElementAtIndex(i);
            row.FindPropertyRelative("root").objectReferenceValue = rows[i].root;
            row.FindPropertyRelative("icon").objectReferenceValue = rows[i].icon;
            row.FindPropertyRelative("abundance").objectReferenceValue = rows[i].text;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, TooltipPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void AddLocalization()
    {
        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Prefabs/Base/Base Localization.asset");
        SetEntry(table, "base.portal.free", "Бесплатно", "Free");
        SetEntry(table, "base.portal.tooltip.danger", "Опасность", "Danger");
        SetEntry(table, "base.portal.abundance.many", "Много", "Plentiful");
        SetEntry(table, "base.portal.abundance.average", "Средне", "Average");
        SetEntry(table, "base.portal.abundance.few", "Мало", "Scarce");
        SetEntry(table, "base.portal.abundance.rare", "Редко", "Rare");
        SetEntry(table, "base.portal.abundance.sometimes", "Иногда", "Occasional");
        SetEntry(table, "base.portal.abundance.almost_none", "Почти не встречается", "Almost never found");
        foreach (Definition definition in Definitions)
        {
            SetEntry(table, $"base.portal.location.{definition.id}.name", definition.ruName, definition.enName);
            SetEntry(table, $"base.portal.location.{definition.id}.description", definition.ruDescription, definition.enDescription);
        }
        EditorUtility.SetDirty(table);
    }

    private static string AbundanceRu(string key) => key switch
    {
        "many" => "Много", "average" => "Средне", "few" => "Мало", "rare" => "Редко",
        "sometimes" => "Иногда", _ => "Почти не встречается"
    };

    private static void SetEntry(LocalizationTable table, string key, string ru, string en)
    {
        LocalizationTable.Entry entry = table.entries.Find(item => item.key == key);
        if (entry == null) { entry = new LocalizationTable.Entry { key = key }; table.entries.Add(entry); }
        entry.values = new List<string> { ru, en };
    }

    private static ResourceType LoadResource(string name) =>
        AssetDatabase.LoadAssetAtPath<ResourceType>($"Assets/Prefabs/Resources/{name}.asset");

    private static Sprite FindSprite(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            if (sprite != null && sprite.name == name) return sprite;
        }
        return null;
    }

    private static GameObject NewRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject go = new(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    private static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size,
        Font font, int fontSize, TextAnchor alignment)
    {
        GameObject go = NewRect(name, parent, position, size);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.91f, 0.82f, 1f);
        text.raycastTarget = false;
        return text;
    }
}
#endif
