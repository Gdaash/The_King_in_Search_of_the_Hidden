#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class UnitDescriptionSetup
{
    public const string Folder = "Assets/Prefabs/UI/Unit Details";
    public const string Data = "Assets/Resources/UI/Unit Details";
    public const string Art = "Assets/Sprites/Ui/Unit Stats";
    public const string CardPath = Folder + "/Unit Description Card.prefab";
    public const string TooltipPath = Folder + "/Unit Description Tooltip.prefab";
    private static readonly Color Ink = new(.94f, .91f, .82f);
    private static readonly Color Muted = new(.68f, .63f, .73f);
    private static Font Font => AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");
    private static Sprite Panel => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Building Construction Tooltip.prefab").GetComponent<UnityEngine.UI.Image>().sprite;

    [MenuItem("Tools/Game Foundation/UI/Setup Unit Descriptions")]
    public static void Run()
    {
        Directory.CreateDirectory(Folder); Directory.CreateDirectory(Data); Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        ImportArt();
        AddLocalization();
        var sword = Definition("Swordsman", "RedSwordsman", "swordsman", "Боец ближнего боя. Сближается с противником и защищает подступы к порталу.");
        var archer = Definition("Archer", "RedArcher", "archer", "Боец дальнего боя. Стреляет с расстояния и старается удерживать дистанцию.");
        var catalog = AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>(Data + "/Unit Description Catalog.asset");
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<UnitDescriptionCatalog>(); AssetDatabase.CreateAsset(catalog, Data + "/Unit Description Catalog.asset"); }
        catalog.units = new[] { sword, archer }; EditorUtility.SetDirty(catalog);
        CreateRow(); CreateCard(sword); CreateTooltip();
        ConfigurePopupAsset("Assets/Prefabs/Base/Fort Popup.prefab", sword);
        ConfigurePopupAsset("Assets/Prefabs/Base/Archery Range Popup.prefab", archer);
        ConfigureRoster("Assets/Prefabs/UI/HUD/Military Controls.prefab", catalog);
        ConfigureRoster("Assets/Prefabs/UI/HUD/Base Military Overview.prefab", catalog);
        string screenPath = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
        var root = PrefabUtility.LoadPrefabContents(screenPath);
        try
        {
            foreach (var view in root.GetComponentsInChildren<MilitaryTrainingView>(true))
            {
                var resource = new SerializedObject(view).FindProperty("warrior").objectReferenceValue as ResourceType;
                ConfigurePopup(view.gameObject, resource == sword.resource ? sword : archer);
            }
            PrefabUtility.SaveAsPrefabAsset(root, screenPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        ApplyCompactLayout();
        AssetDatabase.SaveAssets();
        Debug.Log("Unit descriptions: shared card, tooltip, 2 unit definitions, 2 recruitment windows and both troop panels configured.");
    }

    private static UnitDescriptionDefinition Definition(string resource, string prefab, string key, string fallback)
    {
        string path = Data + "/" + resource + " Description.asset";
        var definition = AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(path);
        if (definition == null) { definition = ScriptableObject.CreateInstance<UnitDescriptionDefinition>(); AssetDatabase.CreateAsset(definition, path); }
        definition.resource = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/" + resource + ".asset");
        definition.unitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/" + prefab + ".prefab");
        definition.food = AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset");
        definition.scientificStats = AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");
        definition.titleKey = "unit." + key + ".title"; definition.roleKey = "unit." + key + ".role";
        definition.fallbackRole = resource == "Archer" ? "ДАЛЬНИЙ БОЙ" : "БЛИЖНИЙ БОЙ";
        definition.descriptionKey = "unit." + key + ".description"; definition.fallbackDescription = fallback;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void CreateRow()
    {
        GameObject root = Rect("Unit Stat Row", null, Vector2.zero, new Vector2(584, 34));
        var bg = root.AddComponent<UnityEngine.UI.Image>(); bg.color = new Color(.3f, .25f, .35f, .22f); bg.raycastTarget = false;
        root.AddComponent<LayoutElement>().preferredHeight = 34f;
        var view = root.AddComponent<UnitStatRowView>();
        var icon = Rect("Icon", root.transform, new Vector2(22, 0), new Vector2(32, 32)).AddComponent<UnityEngine.UI.Image>();
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0, .5f); icon.color = new Color(.78f, .7f, .88f); icon.raycastTarget = false;
        var label = Text("Label", root.transform, "Характеристика", 20, TextAnchor.MiddleLeft);
        Stretch(label.rectTransform, new Vector2(50, 0), new Vector2(-194, 0));
        var value = Text("Value", root.transform, "0", 20, TextAnchor.MiddleRight);
        var vr = value.rectTransform; vr.anchorMin = new Vector2(1, 0); vr.anchorMax = Vector2.one; vr.pivot = new Vector2(1, .5f); vr.sizeDelta = new Vector2(190, 0); vr.anchoredPosition = new Vector2(-12, 0);
        Set(view, "icon", icon); Set(view, "label", label); Set(view, "value", value); Set(view, "background", bg);
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Unit Stat Row.prefab"); Object.DestroyImmediate(root);
    }

    private static void CreateCard(UnitDescriptionDefinition preview)
    {
        GameObject root = Rect("Unit Description Card", null, Vector2.zero, new Vector2(640, 804));
        var background = root.AddComponent<UnityEngine.UI.Image>(); background.sprite = Panel; background.type = UnityEngine.UI.Image.Type.Sliced; background.raycastTarget = false;
        var view = root.AddComponent<UnitDescriptionView>();
        var portrait = Rect("Portrait", root.transform, new Vector2(50, -54), new Vector2(48, 48)).AddComponent<UnityEngine.UI.Image>();
        TopLeft(portrait.rectTransform); portrait.raycastTarget = false; ResourceIconSizing.Apply(portrait, preview.resource.resourceIcon);
        var role = Text("Role", root.transform, "БЛИЖНИЙ БОЙ", 16, TextAnchor.MiddleLeft); At(role.rectTransform, 90, -25, 510, 24); role.color = Muted;
        var title = Text("Unit Name", root.transform, "Мечник", 30, TextAnchor.MiddleLeft); At(title.rectTransform, 90, -63, 510, 40);
        var progress = Text("Experience", root.transform, "Новобранец · без звёзд", 16, TextAnchor.MiddleLeft); At(progress.rectTransform, 28, -105, 584, 26); progress.color = new Color(.84f, .73f, .47f);
        var description = Text("Description", root.transform, preview.fallbackDescription, 20, TextAnchor.UpperLeft); At(description.rectTransform, 28, -162, 584, 62);
        var heading = Text("Table Heading", root.transform, "ХАРАКТЕРИСТИКИ", 16, TextAnchor.MiddleLeft); At(heading.rectTransform, 28, -216, 380, 24); heading.color = Muted; Localize(heading, "unit.details.heading");
        var columns = Text("Value Heading", root.transform, "ЗНАЧЕНИЕ", 16, TextAnchor.MiddleRight); At(columns.rectTransform, 420, -216, 180, 24); columns.color = Muted; Localize(columns, "unit.details.value_heading");
        var line = Rect("Rule", root.transform, Vector2.zero, Vector2.zero).AddComponent<UnityEngine.UI.Image>(); line.color = new Color(.49f,.4f,.54f,.7f); line.raycastTarget = false; At(line.rectTransform, 28, -235, 584, 2);
        var scroller = Rect("Stats Scroll", root.transform, Vector2.zero, Vector2.zero);
        Stretch((RectTransform)scroller.transform, new Vector2(26, 94), new Vector2(-26, -248));
        var scroll = scroller.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40; scroll.inertia = false;
        var viewport = Rect("Viewport", scroller.transform, Vector2.zero, Vector2.zero); Stretch((RectTransform)viewport.transform, Vector2.zero, new Vector2(-12, 0));
        var viewportImage = viewport.AddComponent<UnityEngine.UI.Image>(); viewportImage.color = Color.white;
        var mask = viewport.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var content = Rect("Rows", viewport.transform, Vector2.zero, Vector2.zero); var contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0, 1); contentRect.anchorMax = Vector2.one; contentRect.pivot = new Vector2(.5f, 1);
        var layout = content.AddComponent<VerticalLayoutGroup>(); layout.spacing = 2; layout.childControlWidth = true; layout.childControlHeight = true; layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var template = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Unit Stat Row.prefab"), content.transform); template.name = "Row Template"; template.SetActive(false);
        scroll.content = contentRect; scroll.viewport = (RectTransform)viewport.transform;
        var scrollbarGo = Rect("Scrollbar", scroller.transform, Vector2.zero, Vector2.zero);
        var barRect = (RectTransform)scrollbarGo.transform; barRect.anchorMin = new Vector2(1,0); barRect.anchorMax = Vector2.one; barRect.pivot = new Vector2(1,.5f); barRect.sizeDelta = new Vector2(6,0);
        scrollbarGo.AddComponent<UnityEngine.UI.Image>().color = new Color(.15f,.1f,.18f);
        var handle = Rect("Handle", scrollbarGo.transform, Vector2.zero, Vector2.zero).AddComponent<UnityEngine.UI.Image>(); Stretch(handle.rectTransform,Vector2.zero,Vector2.zero); handle.color = new Color(.55f,.46f,.62f);
        var scrollbar = scrollbarGo.AddComponent<Scrollbar>(); scrollbar.handleRect = handle.rectTransform; scrollbar.targetGraphic = handle; scrollbar.direction = Scrollbar.Direction.BottomToTop; scroll.verticalScrollbar = scrollbar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        var footer = Text("Rules", root.transform, "", 16, TextAnchor.UpperLeft); var footerRect=footer.rectTransform; footerRect.anchorMin=footerRect.anchorMax=new Vector2(0,0); footerRect.pivot=new Vector2(0,0); footerRect.anchoredPosition=new Vector2(28,22); footerRect.sizeDelta=new Vector2(584,58); footer.color=Muted;
        Set(view,"definition",preview); Set(view,"title",title); Set(view,"role",role); Set(view,"description",description); Set(view,"progress",progress); Set(view,"footer",footer); Set(view,"portrait",portrait); Set(view,"scroll",scroll); Set(view,"rows",contentRect); Set(view,"rowTemplate",template.GetComponent<UnitStatRowView>());
        var so=new SerializedObject(view); var list=so.FindProperty("stats"); list.arraySize=Enum.GetValues(typeof(UnitStat)).Length;
        for(int i=0;i<list.arraySize;i++)
        {
            var row=list.GetArrayElementAtIndex(i); var stat=(UnitStat)i;
            row.FindPropertyRelative("stat").enumValueIndex=i; row.FindPropertyRelative("labelKey").stringValue="unit.stat."+stat;
            row.FindPropertyRelative("fallbackLabel").stringValue=Labels[i].Item1;
            row.FindPropertyRelative("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+IconNames[i]+".png");
            row.FindPropertyRelative("hideWhenZero").boolValue=stat==UnitStat.FireDamage||stat==UnitStat.IceDamage||stat==UnitStat.MagicDamage;
        }
        so.ApplyModifiedPropertiesWithoutUndo(); view.Show(preview); LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        PrefabUtility.SaveAsPrefabAsset(root, CardPath); Object.DestroyImmediate(root);
    }

    private static void CreateTooltip()
    {
        var root=Rect("Unit Description Tooltip",null,Vector2.zero,new Vector2(640,880)); ((RectTransform)root.transform).pivot=Vector2.zero;
        var group=root.AddComponent<CanvasGroup>(); group.alpha=0; group.blocksRaycasts=false; group.interactable=false;
        var canvas=root.AddComponent<Canvas>(); canvas.overrideSorting=true; canvas.sortingOrder=32000;
        var card=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CardPath),root.transform); Stretch((RectTransform)card.transform,Vector2.zero,Vector2.zero);
        var tooltip=root.AddComponent<UnitDescriptionTooltip>(); Set(tooltip,"card",card.GetComponent<UnitDescriptionView>());
        var so=new SerializedObject(tooltip);so.FindProperty("preferredSize").vector2Value=new Vector2(640,880);so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root,TooltipPath);Object.DestroyImmediate(root);
    }

    private static void ConfigurePopupAsset(string path, UnitDescriptionDefinition definition)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try { ConfigurePopup(root,definition);PrefabUtility.SaveAsPrefabAsset(root,path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void ConfigurePopup(GameObject root, UnitDescriptionDefinition definition)
    {
        var window = root.transform.Find("Window");
        if (window == null)
        {
            var children = root.transform.Cast<Transform>().ToArray();
            window = Rect("Window", root.transform, Vector2.zero, new Vector2(1120,936)).transform;
            foreach (var child in children) child.SetParent(window, false);
        }
        Size(window,new Vector2(1120,936),Vector2.zero);
        if (window.GetComponent<CanvasWindowFit>() == null) window.gameObject.AddComponent<CanvasWindowFit>();
        Size(window.Find("Artwork Frame"),new Vector2(1120,936),Vector2.zero);
        Size(window.Find("Title"),new Vector2(970,50),new Vector2(0,416));
        Size(window.Find("Close"),new Vector2(34,34),new Vector2(518,432));
        var card=window.Find("Unit Description Card");
        if(card==null) card=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CardPath),window)).transform;
        Size(card,new Vector2(640,804),new Vector2(196,-28));
        card.GetComponent<UnitDescriptionView>().Show(definition);Set(card.GetComponent<UnitDescriptionView>(),"definition",definition);
        Size(window.Find("Stock"),new Vector2(342,64),new Vector2(-356,298));
        Size(window.Find("Stock/Label"),new Vector2(176,45),new Vector2(-78,0));
        Size(window.Find("Stock/Resource"),new Vector2(128,64),new Vector2(110,0));
        var icon=window.Find("Stock/Resource/Icon").GetComponent<UnityEngine.UI.Image>(); ResourceIconSizing.Apply(icon,definition.resource.resourceIcon); icon.rectTransform.anchoredPosition=new Vector2(-34,0);
        Localize(window.Find("Stock/Label").GetComponent<UnityEngine.UI.Text>(),"unit.training.stock");
        Size(window.Find("Arm Cost"),new Vector2(342,64),new Vector2(-356,174));
        Size(window.Find("Arm Cost/Human"),new Vector2(132,64),new Vector2(-90,0));
        Size(window.Find("Arm Cost/Weapon"),new Vector2(132,64),new Vector2(90,0));
        Label(window,"Available Caption","ДОСТУПНЫЕ РЕСУРСЫ",new Vector2(-356,226),new Vector2(340,30),"unit.training.available");
        Label(window,"Cost Caption","ЦЕНА ОДНОГО БОЙЦА",new Vector2(-356,102),new Vector2(340,30),"unit.training.price");
        var training=root.GetComponent<MilitaryTrainingView>(); var so=new SerializedObject(training);
        var human=so.FindProperty("human").objectReferenceValue as ResourceType;var weapon=so.FindProperty("weapon").objectReferenceValue as ResourceType;
        var recipe=window.Find("Recruitment Recipe");if(recipe==null)recipe=Rect("Recruitment Recipe",window,new Vector2(-356,50),new Vector2(340,58)).transform;
        Recipe(recipe,"Human",human,-90);Recipe(recipe,"Weapon",weapon,90);
        var plus=recipe.Find("Plus");if(plus==null)plus=Text("Plus",recipe,"+",22,TextAnchor.MiddleCenter).transform;Size(plus,new Vector2(32,48),Vector2.zero);
        Set(training,"humanCostAmount",recipe.Find("Human/Amount").GetComponent<UnityEngine.UI.Text>());Set(training,"weaponCostAmount",recipe.Find("Weapon/Amount").GetComponent<UnityEngine.UI.Text>());
        Size(window.Find("Arm"),new Vector2(342,64),new Vector2(-356,-38));
        Size(window.Find("Disarm"),new Vector2(342,64),new Vector2(-356,-126));
        Label(window,"Disarm Hint","При разоружении житель и оружие возвращаются в запас.",new Vector2(-356,-218),new Vector2(330,94),"unit.training.disarm_hint",20);
        Label(window,"Deployment Hint","В начале забега весь отряд автоматически выходит из портала.",new Vector2(-356,-350),new Vector2(330,96),"unit.training.deploy_hint",18);
    }
    private static void Recipe(Transform parent,string name,ResourceType resource,float x)
    {
        var row=parent.Find(name);if(row==null)row=Rect(name,parent,new Vector2(x,0),new Vector2(136,56)).transform;
        var icon=row.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();if(icon==null)icon=Rect("Icon",row,new Vector2(-34,0),new Vector2(48,48)).AddComponent<UnityEngine.UI.Image>();ResourceIconSizing.Apply(icon,resource.resourceIcon);icon.raycastTarget=false;
        var amount=row.Find("Amount")?.GetComponent<UnityEngine.UI.Text>();if(amount==null)amount=Text("Amount",row,"1",24,TextAnchor.MiddleLeft);Size(amount.transform,new Vector2(48,48),new Vector2(25,0));
    }
    private static void ConfigureRoster(string path,UnitDescriptionCatalog catalog)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var tooltip=root.GetComponentInChildren<UnitDescriptionTooltip>(true);
            if(tooltip==null)tooltip=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TooltipPath),root.transform)).GetComponent<UnitDescriptionTooltip>();
            foreach(var item in root.GetComponentsInChildren<WorldMilitaryRosterItemView>(true))
            {
                Set(item,"descriptions",catalog);Set(item,"detailsTooltip",tooltip);
                var icon=item.transform.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();if(icon!=null)icon.raycastTarget=true;
            }
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    private static void ImportArt()
    {
        const string source="Temp/CodexUnitDetails/stat-icons-source.png";
        string[] names={"Heart","Sword","Fire","Magic","Hourglass","Range","Boot","Shield","Cross","Clock","Eye","Star","Retreat","Healing","Target","Sun"};
        if(File.Exists(source))
        {
            var atlas=new Texture2D(2,2);atlas.LoadImage(File.ReadAllBytes(source));
            for(int i=0;i<16;i++)
            {
                var result=new Texture2D(16,16,TextureFormat.RGBA32,false);var pixels=new Color32[256];
                int cellW=atlas.width/4,cellH=atlas.height/4;
                for(int y=0;y<16;y++)for(int x=0;x<16;x++)
                {
                    float alpha=0;int samples=0;
                    int x0=(i%4)*cellW+x*cellW/16,x1=(i%4)*cellW+(x+1)*cellW/16;
                    int y0=(3-i/4)*cellH+y*cellH/16,y1=(3-i/4)*cellH+(y+1)*cellH/16;
                    for(int sy=y0;sy<y1;sy++)for(int sx=x0;sx<x1;sx++){var p=atlas.GetPixel(sx,sy);alpha+=p.a*Mathf.Min(p.r,p.g,p.b);samples++;}
                    pixels[y*16+x]=new Color32(255,255,255,(byte)(alpha/Mathf.Max(1,samples)>.72f?255:0));
                }
                result.SetPixels32(pixels);result.Apply();File.WriteAllBytes(Art+"/"+names[i]+".png",result.EncodeToPNG());Object.DestroyImmediate(result);
            }
            Object.DestroyImmediate(atlas);
        }
        AssetDatabase.Refresh();foreach(string name in names)ImportSprite(Art+"/"+name+".png");
        // UI copies keep the original artwork, while avoiding multi-thousand-unit Canvas icons.
        foreach(string name in new[]{"Swordsman","Archer"})
        {
            var resource=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+name+".asset");
            if(resource==null||resource.resourceIcon==null||resource.resourceIcon.rect.width<=32)continue;
            var original=resource.resourceIcon;var input=new Texture2D(2,2);input.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(original.texture)));
            var output=new Texture2D(24,24,TextureFormat.RGBA32,false);
            Rect region=original.rect;
            for(int y=0;y<24;y++)for(int x=0;x<24;x++)output.SetPixel(x,y,input.GetPixel((int)(region.x+(x+.5f)*region.width/24),(int)(region.y+(y+.5f)*region.height/24)));
            output.Apply();string path=Art+"/"+name+" UI.png";File.WriteAllBytes(path,output.EncodeToPNG());Object.DestroyImmediate(input);Object.DestroyImmediate(output);AssetDatabase.ImportAsset(path);ImportSprite(path);
            resource.resourceIcon=AssetDatabase.LoadAssetAtPath<Sprite>(path);EditorUtility.SetDirty(resource);
        }
    }
    private static void ImportSprite(string path)
    {
        if(!File.Exists(path))throw new FileNotFoundException("Stat icon missing",path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
    }

    private static readonly (string,string)[] Labels={
        ("Здоровье","Health"),("Физический урон","Physical damage"),("Огненный урон","Fire damage"),("Урон холодом","Ice damage"),("Магический урон","Magic damage"),
        ("Частота атак","Attack rate"),("Интервал атак","Attack interval"),("Дальность атаки","Attack range"),("Скорость движения","Movement speed"),("Радиус обнаружения","Detection range"),
        ("Защита от физ. урона","Physical resistance"),("Защита от огня","Fire resistance"),("Защита от холода","Ice resistance"),("Защита от магии","Magic resistance"),
        ("Регенерация вне боя","Out-of-combat regen"),("Задержка регенерации","Regeneration delay"),("Лечение у портала","Healing at the portal"),("Отступление","Retreat"),("Пища на новый день","Food each new day"),("Приоритет цели","Target priority"),("Защита","Defense")};
    private static readonly string[] IconNames={"Heart","Sword","Fire","Magic","Magic","Hourglass","Clock","Range","Boot","Eye","Shield","Shield","Shield","Shield","Cross","Clock","Healing","Retreat","Sun","Target","Shield"};

    [MenuItem("Tools/Game Foundation/UI/Compact Unit Descriptions")]
    public static void ApplyCompactLayout()
    {
        // Upgrade existing prefab objects in place, preserving references and manual edits.
        EditPrefab(Folder + "/Unit Stat Row.prefab", root => ConfigureDefenseCells(root.GetComponent<UnitStatRowView>()));
        EditPrefab(CardPath, root => CompactCard(root.GetComponent<UnitDescriptionView>()));
        EditPrefab(TooltipPath, root =>
        {
            var so = new SerializedObject(root.GetComponent<UnitDescriptionTooltip>());
            so.FindProperty("preferredSize").vector2Value = new Vector2(640,780); so.ApplyModifiedPropertiesWithoutUndo();
            ((RectTransform)root.transform).sizeDelta = new Vector2(640,780);
        });
        foreach (string path in new[]{"Assets/Prefabs/Base/Fort Popup.prefab", "Assets/Prefabs/Base/Archery Range Popup.prefab", "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab"})
            EditPrefab(path, root => { foreach (var training in root.GetComponentsInChildren<MilitaryTrainingView>(true)) CompactTraining(training); });
        // These local size overrides were authored with the original taller tooltip.
        foreach (string path in new[]{"Assets/Prefabs/UI/HUD/Military Controls.prefab", "Assets/Prefabs/UI/HUD/Base Military Overview.prefab"})
            EditPrefab(path, root => { foreach (var tip in root.GetComponentsInChildren<UnitDescriptionTooltip>(true)) ((RectTransform)tip.transform).sizeDelta = new Vector2(640,780); });
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        CompactTranslation(table,"unit.stat.Defense","Защита","Defense");
        CompactTranslation(table,"unit.training.food_per_unit","Пища на бойца","Food per warrior");
        CompactTranslation(table,"unit.training.food_total","Пища отряда","Squad food");
        CompactTranslation(table,"unit.training.food_daily","{0} / день","{0} / day");
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
    }

    private static void EditPrefab(string path, Action<GameObject> edit)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try { edit(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CompactTranslation(LocalizationTable table,string key,string ru,string en)
    {
        var entry=table.entries.Find(e=>e.key==key);
        if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}
        while(entry.values.Count<table.languages.Count)entry.values.Add("");
        int r=table.languages.IndexOf("ru"),e=table.languages.IndexOf("en");if(r>=0)entry.values[r]=ru;if(e>=0)entry.values[e]=en;
    }

    private static void ConfigureDefenseCells(UnitStatRowView row)
    {
        var group = row.transform.Find("Defense Values") as RectTransform;
        if(group==null)group=(RectTransform)Rect("Defense Values",row.transform,Vector2.zero,new Vector2(384,38)).transform;
        group.anchorMin=new Vector2(1,0); group.anchorMax=Vector2.one; group.pivot=new Vector2(1,.5f); group.sizeDelta=new Vector2(384,0); group.anchoredPosition=new Vector2(-12,0);
        var so=new SerializedObject(row);so.FindProperty("defenseGroup").objectReferenceValue=group;
        var cells=so.FindProperty("defenseCells");cells.arraySize=4;
        string[] sprites={Art+"/Sword.png",Art+"/Fire.png","Assets/Sprites/Evolution adventure/Sprites/CharactersAndItems/IceState.png",Art+"/Magic.png"};
        for(int i=0;i<4;i++)
        {
            string name=((DamageType)i).ToString();var cell=group.Find(name) as RectTransform;
            if(cell==null)cell=(RectTransform)Rect(name,group,Vector2.zero,new Vector2(96,38)).transform;
            cell.anchorMin=cell.anchorMax=cell.pivot=new Vector2(0,.5f);cell.anchoredPosition=new Vector2(i*96,0);cell.sizeDelta=new Vector2(96,38);
            var icon=cell.Find("Icon")?.GetComponent<UnityEngine.UI.Image>();
            if(icon==null)icon=Rect("Icon",cell,Vector2.zero,new Vector2(32,32)).AddComponent<UnityEngine.UI.Image>();
            icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,.5f);icon.rectTransform.anchoredPosition=new Vector2(16,0);icon.raycastTarget=false;
            ResourceIconSizing.Apply(icon,AssetDatabase.LoadAssetAtPath<Sprite>(sprites[i]));
            icon.color=i==2?Color.white:new Color(.78f,.7f,.88f);
            var amount=cell.Find("Amount")?.GetComponent<UnityEngine.UI.Text>()??Text("Amount",cell,"0%",18,TextAnchor.MiddleRight);
            var rect=amount.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f);rect.anchoredPosition=new Vector2(34,0);rect.sizeDelta=new Vector2(56,34);amount.horizontalOverflow=HorizontalWrapMode.Overflow;
            var prop=cells.GetArrayElementAtIndex(i);prop.FindPropertyRelative("type").enumValueIndex=i;prop.FindPropertyRelative("icon").objectReferenceValue=icon;prop.FindPropertyRelative("amount").objectReferenceValue=amount;
        }
        so.ApplyModifiedPropertiesWithoutUndo();group.gameObject.SetActive(false);
    }

    private static void CompactCard(UnitDescriptionView view)
    {
        var root=view.transform;
        ((RectTransform)root).sizeDelta=new Vector2(640,712);
        Size(root.Find("Portrait"),new Vector2(48,48),new Vector2(50,-44)); TopLeft((RectTransform)root.Find("Portrait"));
        At((RectTransform)root.Find("Role"),90,-20,510,20);
        At((RectTransform)root.Find("Unit Name"),90,-49,510,36);
        At((RectTransform)root.Find("Experience"),28,-83,584,24);
        At((RectTransform)root.Find("Description"),28,-125,584,44);
        At((RectTransform)root.Find("Table Heading"),28,-164,380,24);
        At((RectTransform)root.Find("Value Heading"),420,-164,180,24);
        At((RectTransform)root.Find("Rule"),28,-182,584,2);
        Stretch((RectTransform)root.Find("Stats Scroll"),new Vector2(26,76),new Vector2(-26,-194));
        var footer=(RectTransform)root.Find("Rules");footer.anchoredPosition=new Vector2(28,16);footer.sizeDelta=new Vector2(584,50);
        var so=new SerializedObject(view);var list=so.FindProperty("stats");
        bool hasDefense=Enumerable.Range(0,list.arraySize).Any(i=>(UnitStat)list.GetArrayElementAtIndex(i).FindPropertyRelative("stat").enumValueIndex==UnitStat.Defense);
        for(int i=list.arraySize-1;i>=0;i--)
        {
            var entry=list.GetArrayElementAtIndex(i);var stat=(UnitStat)entry.FindPropertyRelative("stat").enumValueIndex;
            if(stat==UnitStat.AttackInterval||stat==UnitStat.FireResistance||stat==UnitStat.IceResistance||stat==UnitStat.MagicResistance||(stat==UnitStat.PhysicalResistance&&hasDefense)){list.DeleteArrayElementAtIndex(i);continue;}
            if(stat!=UnitStat.PhysicalResistance)continue;
            entry.FindPropertyRelative("stat").enumValueIndex=(int)UnitStat.Defense;
            entry.FindPropertyRelative("labelKey").stringValue="unit.stat.Defense";entry.FindPropertyRelative("fallbackLabel").stringValue="Защита";
            entry.FindPropertyRelative("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/Shield.png");
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        foreach(var row in root.GetComponentsInChildren<UnitStatRowView>(true))ConfigureDefenseCells(row);
        view.Show(view.Definition);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.Find("Stats Scroll/Viewport/Rows"));
    }

    private static void CompactTraining(MilitaryTrainingView training)
    {
        var window=training.transform.Find("Window");
        if(window==null)throw new InvalidOperationException("Recruitment Window missing: "+training.name);
        Size(window,new Vector2(1120,832),Vector2.zero);
        Size(window.Find("Artwork Frame"),new Vector2(1120,832),Vector2.zero);
        Size(window.Find("Title"),new Vector2(970,50),new Vector2(0,364));
        Size(window.Find("Close"),new Vector2(34,34),new Vector2(518,380));
        Set(training,"closeButton",window.Find("Close").GetComponent<UnityEngine.UI.Button>());
        var fit=new SerializedObject(window.GetComponent<CanvasWindowFit>());fit.FindProperty("margin").floatValue=80;fit.ApplyModifiedPropertiesWithoutUndo();
        var card=window.GetComponentInChildren<UnitDescriptionView>(true);
        CompactCard(card);Size(card.transform,new Vector2(640,712),new Vector2(196,-28));
        var cardSO=new SerializedObject(card);cardSO.FindProperty("showFoodStat").boolValue=false;cardSO.ApplyModifiedPropertiesWithoutUndo();card.Refresh();
        Size(window.Find("Stock"),new Vector2(342,64),new Vector2(-356,294));
        var one=FoodLine(window,"Food Per Warrior","Пища на бойца",246,"unit.training.food_per_unit",card.Definition.food,1);
        var total=FoodLine(window,"Squad Food","Пища отряда",194,"unit.training.food_total",card.Definition.food,0);
        Set(training,"unitDescription",card.Definition);Set(training,"foodPerUnitIcon",one.Find("Icon").GetComponent<UnityEngine.UI.Image>());Set(training,"totalFoodIcon",total.Find("Icon").GetComponent<UnityEngine.UI.Image>());
        Set(training,"foodPerUnitAmount",one.Find("Amount").GetComponent<UnityEngine.UI.Text>());Set(training,"totalFoodAmount",total.Find("Amount").GetComponent<UnityEngine.UI.Text>());
        Move(window,"Available Caption",142);Move(window,"Arm Cost",90);Move(window,"Cost Caption",38);Move(window,"Recruitment Recipe",-14);Move(window,"Arm",-92);Move(window,"Disarm",-172);
        Size(window.Find("Disarm Hint"),new Vector2(330,74),new Vector2(-356,-254));
        Size(window.Find("Deployment Hint"),new Vector2(330,80),new Vector2(-356,-344));
    }

    private static void Move(Transform window,string name,float y){var r=(RectTransform)window.Find(name);r.anchoredPosition=new Vector2(r.anchoredPosition.x,y);}
    private static Transform FoodLine(Transform window,string name,string caption,float y,string key,ResourceType food,int preview)
    {
        var row=window.Find(name)??Rect(name,window,new Vector2(-356,y),new Vector2(342,48)).transform;
        var label=row.Find("Label")?.GetComponent<UnityEngine.UI.Text>()??Text("Label",row,caption,20,TextAnchor.MiddleLeft);
        label.text=caption;Size(label.transform,new Vector2(188,42),new Vector2(-74,0));Localize(label,key);
        var icon=row.Find("Icon")?.GetComponent<UnityEngine.UI.Image>()??Rect("Icon",row,new Vector2(40,0),Vector2.zero).AddComponent<UnityEngine.UI.Image>();
        ResourceIconSizing.Apply(icon,food.resourceIcon);icon.raycastTarget=false;
        var amount=row.Find("Amount")?.GetComponent<UnityEngine.UI.Text>()??Text("Amount",row,preview+" / день",20,TextAnchor.MiddleRight);
        Size(amount.transform,new Vector2(104,42),new Vector2(116,0));
        return row;
    }

    private static void AddLocalization()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        void Entry(string key,string ru,string en)
        {
            var entry=table.entries.Find(e=>e.key==key);if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}
            while(entry.values.Count<table.languages.Count)entry.values.Add("");
            int r=table.languages.IndexOf("ru"),e=table.languages.IndexOf("en");if(r>=0)entry.values[r]=ru;if(e>=0)entry.values[e]=en;
        }
        for(int i=0;i<Labels.Length;i++)Entry("unit.stat."+(UnitStat)i,Labels[i].Item1,Labels[i].Item2);
        Entry("unit.swordsman.title","Мечник","Swordsman");Entry("unit.archer.title","Лучник","Archer");
        Entry("unit.swordsman.role","БЛИЖНИЙ БОЙ","MELEE");Entry("unit.archer.role","ДАЛЬНИЙ БОЙ","RANGED");
        Entry("unit.swordsman.description","Боец ближнего боя. Сближается с противником и защищает подступы к порталу.","A melee fighter. Closes in on enemies and defends the approaches to the portal.");
        Entry("unit.archer.description","Боец дальнего боя. Стреляет с расстояния и старается удерживать дистанцию.","A ranged fighter. Fires from afar and tries to keep a safe distance.");
        Entry("unit.details.recruit","Новобранец · без звёзд","Recruit · no stars");Entry("unit.details.progress","Уровень {0}/10 · опыт {1}","Level {0}/10 · XP {1}");Entry("unit.details.maximum","максимум","maximum");
        Entry("unit.details.footer","За убийство: {0} опыта · за помощь: {1}\nНовый день полностью восстанавливает здоровье.\nКолесо мыши — прокрутка характеристик.","Kill: {0} XP · assist: {1} XP\nA new day restores health completely.\nMouse wheel — scroll statistics.");
        Entry("unit.details.weakest","Меньше % HP","Lowest % HP"); Entry("unit.details.nearest","Ближайший","Nearest");
        Entry("unit.details.heading","ХАРАКТЕРИСТИКИ","STATISTICS");Entry("unit.details.value_heading","ЗНАЧЕНИЕ","VALUE");
        Entry("unit.details.per_second"," / с"," / s");Entry("unit.details.seconds"," с"," s");Entry("unit.details.distance"," ед."," u");Entry("unit.details.speed_unit"," ед./с"," u/s");Entry("unit.details.hp_per_second"," HP/с"," HP/s");Entry("unit.details.percent_per_second","% HP/с","% HP/s");
        Entry("unit.details.none","Нет","None");Entry("unit.details.not_researched","Не изучено","Not researched");Entry("unit.details.retreat_threshold","При HP ≤ 10%","At HP ≤ 10%");Entry("unit.details.decree_off","Указ выключен","Decree disabled");Entry("unit.details.food_amount","1 / день","1 / day");
        Entry("unit.training.stock","В запасе","In reserve");Entry("unit.training.available","ДОСТУПНЫЕ РЕСУРСЫ","AVAILABLE RESOURCES");Entry("unit.training.price","ЦЕНА ОДНОГО БОЙЦА","COST PER WARRIOR");
        Entry("unit.training.disarm_hint","При разоружении житель и оружие возвращаются в запас.","Disarming returns the resident and their weapon to your reserves.");
        Entry("unit.training.deploy_hint","В начале забега весь отряд автоматически выходит из портала.","The whole squad leaves the portal automatically at the start of a run.");
        EditorUtility.SetDirty(table);
    }
    private static GameObject Rect(string name,Transform parent,Vector2 position,Vector2 size){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);go.layer=5;Size(go.transform,size,position);return go;}
    private static void Size(Transform t,Vector2 size,Vector2 position){if(t is not RectTransform r)return;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;}
    private static UnityEngine.UI.Text Text(string name,Transform parent,string value,int size,TextAnchor alignment){var text=Rect(name,parent,Vector2.zero,new Vector2(100,30)).AddComponent<UnityEngine.UI.Text>();text.font=Font;text.fontSize=size;text.fontStyle=FontStyle.Normal;text.text=value;text.alignment=alignment;text.color=Ink;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.supportRichText=false;return text;}
    private static void TopLeft(RectTransform r){r.anchorMin=r.anchorMax=new Vector2(0,1);}
    private static void At(RectTransform r,float x,float y,float w,float h){TopLeft(r);r.pivot=new Vector2(0,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;}
    private static void Set(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Localize(UnityEngine.UI.Text text,string key){var component=text.GetComponent<LocalizedText>()??text.gameObject.AddComponent<LocalizedText>();var so=new SerializedObject(component);so.FindProperty("key").stringValue=key;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Label(Transform parent,string name,string value,Vector2 pos,Vector2 size,string key,int fontSize=16){var label=parent.Find(name)?.GetComponent<UnityEngine.UI.Text>();if(label==null)label=Text(name,parent,value,fontSize,TextAnchor.MiddleCenter);label.text=value;label.color=Muted;Size(label.transform,size,pos);Localize(label,key);}
}
#endif
