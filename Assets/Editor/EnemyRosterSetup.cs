#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Authors the enemy HUD without rebuilding existing World UI.</summary>
public static class EnemyRosterSetup
{
    public const string PanelPath = "Assets/Prefabs/UI/HUD/Enemy Roster.prefab";
    public const string RowPath = "Assets/Prefabs/UI/HUD/Enemy Roster Item.prefab";
    public const string CatalogPath = "Assets/Resources/UI/Unit Details/Enemy Description Catalog.asset";
    public const string HudPath = "Assets/Prefabs/UI/Screens/World Screen HUD.prefab";
    private static readonly Color Ink = new(.94f, .91f, .82f);

    [MenuItem("Tools/Game Foundation/UI/Setup Enemy Roster")]
    public static void Run()
    {
        AddLocalization();
        var catalog = CreateCatalog();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPath) == null) CreateRow(catalog.units[0]);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath) == null) CreatePanel(catalog);
        var hud = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            var panel = hud.GetComponentInChildren<EnemyRosterView>(true);
            if (panel == null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath), hud.transform);
                go.transform.SetSiblingIndex(2); // Behind modal windows; after the other persistent HUD panels.
                panel = go.GetComponent<EnemyRosterView>();
            }
            Set(panel, "alarm", hud.GetComponentInChildren<AlarmSystem>(true));
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(hud); }
        AssetDatabase.SaveAssets();
        Debug.Log("Enemy roster prefab installed in World Screen HUD.");
    }

    private static UnitDescriptionCatalog CreateCatalog()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>(CatalogPath);
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<UnitDescriptionCatalog>(); AssetDatabase.CreateAsset(catalog, CatalogPath); }
        var prefabs = new HashSet<GameObject>();
        var hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
        foreach (var alarm in hud.GetComponentsInChildren<AlarmSystem>(true))
            foreach (var threshold in alarm.ConfiguredThresholds)
                foreach (var enemy in threshold.enemies) if (enemy?.prefab != null) prefabs.Add(enemy.prefab);
        foreach (var guid in AssetDatabase.FindAssets("t:AlarmDifficultyTable"))
            foreach (var difficulty in AssetDatabase.LoadAssetAtPath<AlarmDifficultyTable>(AssetDatabase.GUIDToAssetPath(guid)).difficulties)
                foreach (var threshold in difficulty.thresholds)
                    foreach (var enemy in threshold.enemies) if (enemy?.prefab != null) prefabs.Add(enemy.prefab);
        // The bestiary can keep entries for any combat prefab, including types that
        // are enabled only by a later location configuration.
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Units" }))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null && prefab.CompareTag("Enemy1") && prefab.GetComponent<Health>() != null)
                prefabs.Add(prefab);
        }
        var definitions = new List<UnitDescriptionDefinition>();
        foreach (var prefab in prefabs.OrderBy(p => p.name))
        {
            string path = "Assets/Resources/UI/Unit Details/Enemy " + prefab.name + " Description.asset";
            var data = AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<UnitDescriptionDefinition>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.isEnemy = true; data.unitPrefab = prefab; data.fallbackTitle = prefab.name;
            data.titleKey = "unit.enemy." + prefab.name.ToLowerInvariant() + ".title";
            data.descriptionKey = "unit.enemy." + prefab.name.ToLowerInvariant() + ".description";
            data.roleKey = "unit.enemy.role"; data.fallbackRole = "ВРАГ";
            data.portraitIcon = UiIcon(prefab);
            EditorUtility.SetDirty(data);
            definitions.Add(data);
        }
        catalog.units = definitions.ToArray(); EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static Sprite UiIcon(GameObject prefab)
    {
        Sprite source = prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite;
        if (source == null || (source.rect.width <= 32 && source.rect.height <= 32)) return source;
        // Separate importer for UI: keep the original combat artwork and its import settings untouched.
        const string folder = "Assets/Sprites/UI/Portraits/Enemies";
        Directory.CreateDirectory(folder); AssetDatabase.Refresh();
        string destination = folder + "/" + prefab.name + ".png";
        if (!File.Exists(destination)) AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), destination);
        var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 32; importer.maxTextureSize = 32;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(destination);
    }

    private static void CreateRow(UnitDescriptionDefinition preview)
    {
        var root = Rect("Enemy Roster Item", null, new Vector2(276, 76));
        try
        {
            var background = root.AddComponent<UnityEngine.UI.Image>(); background.color = new Color(.25f,.20f,.28f,.65f);
            root.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 76;
            var view = root.AddComponent<EnemyRosterItemView>();
            var icon = Rect("Icon", root.transform, new Vector2(64,64)).AddComponent<UnityEngine.UI.Image>();
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0,.5f);
            icon.rectTransform.anchoredPosition = new Vector2(44,0); icon.raycastTarget = false;
            ResourceIconSizing.Apply(icon, preview.Portrait);
            var name = Text("Name", root.transform, preview.Title, 20, TextAnchor.MiddleLeft);
            Stretch(name.rectTransform,new Vector2(86,4),new Vector2(-56,-4));
            var count = Text("Count", root.transform,"0",24,TextAnchor.MiddleRight);
            count.rectTransform.anchorMin = new Vector2(1,0); count.rectTransform.anchorMax = Vector2.one;
            count.rectTransform.pivot = new Vector2(1,.5f); count.rectTransform.sizeDelta = new Vector2(44,0); count.rectTransform.anchoredPosition = new Vector2(-12,0);
            Set(view,"icon",icon); Set(view,"background",background); Set(view,"title",name); Set(view,"amount",count);
            PrefabUtility.SaveAsPrefabAsset(root,RowPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void CreatePanel(UnitDescriptionCatalog catalog)
    {
        var root = Rect("Enemy Roster",null,new Vector2(300,240));
        try
        {
            var rect = (RectTransform)root.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(24,-116);
            var background = root.AddComponent<UnityEngine.UI.Image>(); background.type = UnityEngine.UI.Image.Type.Sliced;
            background.sprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Building Construction Tooltip.prefab").GetComponent<UnityEngine.UI.Image>().sprite;
            var visibility = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<EnemyRosterView>();
            var title = Text("Title",root.transform,"ВРАЖЕСКИЕ ВОЙСКА",20,TextAnchor.MiddleLeft);
            At(title.rectTransform,16,-25,268,28); Localize(title,"world.enemies.title");
            var subtitle = Text("Subtitle",root.transform,"Сейчас на карте",16,TextAnchor.MiddleLeft);
            At(subtitle.rectTransform,16,-50,268,22); subtitle.color = new Color(.68f,.63f,.73f); Localize(subtitle,"world.enemies.subtitle");
            var scrollGo = Rect("Enemy Scroll",root.transform,Vector2.zero); Stretch((RectTransform)scrollGo.transform,new Vector2(12,12),new Vector2(-12,-72));
            var scroll = scrollGo.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal = false; scroll.inertia = false; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            var viewport = Rect("Viewport",scrollGo.transform,Vector2.zero); Stretch((RectTransform)viewport.transform,Vector2.zero,Vector2.zero);
            viewport.AddComponent<UnityEngine.UI.Image>(); viewport.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            var content = Rect("Rows",viewport.transform,Vector2.zero); var cr = (RectTransform)content.transform;
            cr.anchorMin = new Vector2(0,1); cr.anchorMax = Vector2.one; cr.pivot = new Vector2(.5f,1);
            var layout = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>(); layout.spacing = 4; layout.childControlHeight = layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
            content.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = cr; scroll.viewport = (RectTransform)viewport.transform;
            var template = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RowPath),content.transform); template.name = "Item Template";
            var tooltip = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UnitDescriptionSetup.TooltipPath),root.transform);
            tooltip.GetComponentInChildren<UnitDescriptionView>(true).Show(catalog.units[0]);
            Set(view,"descriptions",catalog); Set(view,"visibility",visibility); Set(view,"panel",rect); Set(view,"rows",cr);
            Set(view,"itemTemplate",template.GetComponent<EnemyRosterItemView>()); Set(view,"detailsTooltip",tooltip.GetComponent<UnitDescriptionTooltip>());
            Set(view,"fallbackPortrait",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/UnitStats/Eye.png"));
            PrefabUtility.SaveAsPrefabAsset(root,PanelPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void AddLocalization()
    {
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Add(table,"world.enemies.title","ВРАЖЕСКИЕ ВОЙСКА","ENEMY FORCES");
        Add(table,"world.enemies.subtitle","Сейчас на карте","Currently on the map");
        Add(table,"unit.enemy.role","ВРАГ","ENEMY");
        Add(table,"unit.details.enemy_type","Характеристики типа врага","Enemy type statistics");
        Add(table,"unit.details.enemy_footer","Базовые характеристики этого типа врагов.\nКолесо мыши — прокрутка характеристик.","Base statistics for this enemy type.\nMouse wheel — scroll statistics.");
        Add(table,"unit.enemy.octopus.title","Осьминог","Octopus");
        Add(table,"unit.enemy.cursedmage.title","Проклятый маг","Cursed mage");
        Add(table,"unit.enemy.cursedknight.title","Проклятый рыцарь","Cursed knight");
        Add(table,"unit.enemy.octopuswarrior.title","Воин-осьминог","Octopus warrior");
        Add(table,"unit.enemy.octopus.description","Враг ближнего боя. Сближается с целью и атакует её.","A melee enemy that closes in on its target to attack.");
        Add(table,"unit.enemy.cursedmage.description","Проклятый маг ведёт бой на расстоянии и выпускает магический снаряд.","A ranged enemy that fires a magical projectile.");
        Add(table,"unit.enemy.cursedknight.description","Тяжёлый воин ближнего боя с мощной атакой.","A heavy melee warrior with a powerful attack.");
        Add(table,"unit.enemy.octopuswarrior.description","Медленный и очень опасный воин ближнего боя.","A slow and extremely dangerous melee warrior.");
        EditorUtility.SetDirty(table);
    }
    private static void Add(LocalizationTable table,string key,string ru,string en)
    {
        var entry = table.entries.Find(e=>e.key==key);
        if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}
        while(entry.values.Count<table.languages.Count)entry.values.Add("");
        int ri=table.languages.IndexOf("ru"),ei=table.languages.IndexOf("en");
        if(ri>=0)entry.values[ri]=ru;if(ei>=0)entry.values[ei]=en;
    }
    private static GameObject Rect(string name,Transform parent,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);((RectTransform)go.transform).sizeDelta=size;return go;
    }
    private static UnityEngine.UI.Text Text(string name,Transform parent,string text,int size,TextAnchor alignment)
    {
        var label=Rect(name,parent,new Vector2(100,30)).AddComponent<UnityEngine.UI.Text>();
        label.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");label.fontSize=size;label.color=Ink;
        label.text=text;label.alignment=alignment;label.raycastTarget=false;label.supportRichText=false;return label;
    }
    private static void At(RectTransform rect,float x,float y,float w,float h){rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(w,h);}
    private static void Stretch(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=min;rect.offsetMax=max;}
    private static void Set(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Localize(UnityEngine.UI.Text label,string key){var so=new SerializedObject(label.gameObject.AddComponent<LocalizedText>());so.FindProperty("key").stringValue=key;so.ApplyModifiedPropertiesWithoutUndo();}
}
#endif
