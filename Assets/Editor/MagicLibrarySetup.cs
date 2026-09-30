#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Bestiary;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Authors the Base building, popup and editable entry prefabs for the magic library.</summary>
public static class MagicLibrarySetup
{
    public const string EntryPath = "Assets/Prefabs/Base/Bestiary Entry.prefab";
    public const string PopupPath = "Assets/Prefabs/Base/Magic Library Popup.prefab";
    private const string HudPath = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
    private static readonly Color Ink = new(.94f, .91f, .82f);
    private static readonly Color Muted = new(.68f, .63f, .73f);

    [MenuItem("Tools/Game Foundation/Base/Setup Magic Library")]
    public static void Run()
    {
        EnemyRosterSetup.Run();
        AddLocalization();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EntryPath) == null) CreateEntry();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath) == null) CreatePopup();
        EnsureCloseButton();
        InstallInBaseHud();
        AssetDatabase.SaveAssets();
        Debug.Log("Magic Library: Base building, persistent bestiary and popup configured.");
    }

    private static void InstallInBaseHud()
    {
        var root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform baseUi = root.GetComponentsInChildren<Transform>(true).First(x => x.name == "Base UI");
            Transform panel = root.GetComponentsInChildren<Transform>(true).First(x => x.name == "Base Panel");
            Transform libraryButton = panel.Find("Magic Library");
            if (libraryButton == null)
            {
                Transform source = panel.Find("Castle");
                libraryButton = Object.Instantiate(source.gameObject, panel).transform;
                libraryButton.name = "Magic Library";
                var rect = (RectTransform)libraryButton;
                rect.anchoredPosition = new Vector2(-200f, -325f);
                var label = libraryButton.GetComponentsInChildren<Text>(true).FirstOrDefault();
                if (label != null)
                {
                    label.text = "Магическая библиотека";
                    Localize(label, "base.base_panel.magic_library.label");
                }

                var construction = libraryButton.GetComponent<BaseBuildingConstruction>();
                var so = new SerializedObject(construction);
                so.FindProperty("buildingId").stringValue = "magic_library";
                so.FindProperty("nameKey").stringValue = "base.base_panel.magic_library.label";
                so.FindProperty("fallbackName").stringValue = "Магическая библиотека";
                so.FindProperty("descriptionKey").stringValue = "base.building.magic_library.description";
                so.FindProperty("fallbackDescription").stringValue = "Хранит бестиарий существ, встреченных в боях.";
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Transform popup = baseUi.Find("Magic Library Popup");
            if (popup == null)
            {
                GameObject dimmer = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Full Screen Popup Dimmer.prefab"), baseUi);
                dimmer.name = "Magic Library Popup Dimmer";
                dimmer.SetActive(false);
                GameObject popupObject = (GameObject)PrefabUtility.InstantiatePrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>(PopupPath), baseUi);
                popupObject.name = "Magic Library Popup";
                popup = popupObject.transform;
                var dimmerLink = popupObject.GetComponent<PopupDimmerLink>();
                Set(dimmerLink, "dimmer", dimmer);
                popupObject.SetActive(false);
            }

            var controller = baseUi.GetComponent<BaseUIController>();
            Set(controller, "magicLibrary", popup.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CreateEntry()
    {
        var root = Rect("Bestiary Entry", null, Vector2.zero, new Vector2(352, 84));
        try
        {
            var background = root.AddComponent<Image>();
            background.sprite = PanelSprite(); background.type = Image.Type.Sliced;
            background.color = new Color(.25f, .20f, .28f, .65f);
            root.AddComponent<LayoutElement>().preferredHeight = 84f;
            var view = root.AddComponent<BestiaryEntryView>();
            var icon = ImageNode("Icon", root.transform, new Vector2(46, 0), new Vector2(64, 64));
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, .5f);
            icon.rectTransform.pivot = new Vector2(.5f, .5f);
            icon.raycastTarget = false;
            var title = TextNode("Name", root.transform, "Гоблин", 22, TextAnchor.MiddleLeft, Ink);
            Stretch(title.rectTransform, new Vector2(88, 30), new Vector2(-18, -4));
            var role = TextNode("Role", root.transform, "ВРАГ", 16, TextAnchor.MiddleLeft, Muted);
            Stretch(role.rectTransform, new Vector2(88, 6), new Vector2(-18, -34));
            Set(view, "icon", icon); Set(view, "background", background); Set(view, "title", title); Set(view, "role", role);
            PrefabUtility.SaveAsPrefabAsset(root, EntryPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void CreatePopup()
    {
        var root = Rect("Magic Library Popup", null, Vector2.zero, Vector2.zero);
        try
        {
            var rt = (RectTransform)root.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var transparent = root.AddComponent<Image>(); transparent.color = Color.clear; transparent.raycastTarget = true;
            var view = root.AddComponent<MagicLibraryView>();
            root.AddComponent<PopupDimmerLink>();

            var window = Rect("Window", root.transform, Vector2.zero, new Vector2(880, 650));
            var wr = (RectTransform)window.transform; wr.anchorMin = wr.anchorMax = wr.pivot = new Vector2(.5f, .5f);
            var windowImage = window.AddComponent<Image>(); windowImage.sprite = PanelSprite(); windowImage.type = Image.Type.Sliced;
            window.AddComponent<CanvasWindowFit>();

            var title = TextNode("Title", window.transform, "МАГИЧЕСКАЯ БИБЛИОТЕКА", 30, TextAnchor.MiddleCenter, Ink);
            At(title.rectTransform, 0, 274, 720, 46, new Vector2(.5f, .5f));
            var subtitle = TextNode("Subtitle", window.transform, "Бестиарий встреченных существ", 18, TextAnchor.MiddleCenter, Muted);
            At(subtitle.rectTransform, 0, 236, 720, 28, new Vector2(.5f, .5f));
            var count = TextNode("Discovered Count", window.transform, "Открыто: 0", 18, TextAnchor.MiddleLeft, new Color(.84f, .73f, .47f));
            At(count.rectTransform, -180, 185, 360, 28, new Vector2(.5f, .5f));
            var rule = ImageNode("Rule", window.transform, Vector2.zero, new Vector2(780, 2)); rule.color = new Color(.49f,.4f,.54f,.7f);
            At(rule.rectTransform, 0, 165, 780, 2, new Vector2(.5f,.5f));

            var scroll = Rect("Entries Scroll", window.transform, Vector2.zero, new Vector2(800, 480));
            At((RectTransform)scroll.transform, 0, -80, 800, 480, new Vector2(.5f,.5f));
            var scrollRect = scroll.AddComponent<ScrollRect>(); scrollRect.horizontal = false; scrollRect.inertia = false; scrollRect.movementType = ScrollRect.MovementType.Clamped; scrollRect.scrollSensitivity = 42;
            var viewport = Rect("Viewport", scroll.transform, Vector2.zero, Vector2.zero);
            Stretch((RectTransform)viewport.transform, Vector2.zero, new Vector2(-12f, 0f));
            var viewportImage = viewport.AddComponent<Image>(); viewportImage.color = Color.white; viewportImage.raycastTarget = false;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var rows = Rect("Entries", viewport.transform, Vector2.zero, Vector2.zero);
            var rowsRect = (RectTransform)rows.transform; rowsRect.anchorMin = new Vector2(0f,1f); rowsRect.anchorMax = Vector2.one; rowsRect.pivot = new Vector2(.5f,1f);
            var layout = rows.AddComponent<VerticalLayoutGroup>(); layout.spacing = 6f; layout.padding = new RectOffset(6,6,6,6); layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            rows.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = (RectTransform)viewport.transform; scrollRect.content = rowsRect;

            var template = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EntryPath), rows.transform);
            template.name = "Entry Template"; template.SetActive(false);
            var empty = TextNode("Empty State", viewport.transform, "В бестиарии пока нет записей.", 20, TextAnchor.MiddleCenter, Muted);
            Stretch(empty.rectTransform, new Vector2(40, 90), new Vector2(-40, -90));
            var close = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Popup Close Icon.prefab"), window.transform);
            close.name = "Close";
            close.AddComponent<Button>();
            var closeRect = (RectTransform)close.transform; closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f,1f); closeRect.pivot = new Vector2(.5f,.5f); closeRect.anchoredPosition = new Vector2(-24f,-24f);

            var tooltip = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UnitDescriptionSetup.TooltipPath), root.transform);
            tooltip.name = "Unit Description Tooltip";
            var catalog = AssetDatabase.LoadAssetAtPath<UnitDescriptionCatalog>(EnemyRosterSetup.CatalogPath);
            Set(view, "enemyDescriptions", catalog); Set(view, "entryTemplate", template.GetComponent<BestiaryEntryView>());
            Set(view, "entriesRoot", rowsRect); Set(view, "detailsTooltip", tooltip.GetComponent<UnitDescriptionTooltip>());
            Set(view, "title", title); Set(view, "subtitle", subtitle); Set(view, "discoveredCount", count); Set(view, "emptyState", empty.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // Popup Close Icon is intentionally only a visual prefab.  The actual Button
    // belongs to each concrete popup so BaseUIController can bind its action.
    private static void EnsureCloseButton()
    {
        var popup = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            var close = popup.transform.Find("Window/Close");
            if (close != null)
            {
                var button = close.GetComponent<Button>() ?? close.gameObject.AddComponent<Button>();
                button.targetGraphic = close.GetComponent<Image>();

                var feedback = close.GetComponent<UnifiedButtonFeedback>() ??
                               close.gameObject.AddComponent<UnifiedButtonFeedback>();
                var properties = new SerializedObject(feedback);
                properties.FindProperty("theme").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset");
                properties.FindProperty("hoverScaleMultiplier").floatValue = 1.3f;
                properties.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(popup, PopupPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(popup); }
    }

    private static void AddLocalization()
    {
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        Add(table,"base.base_panel.magic_library.label","Магическая библиотека","Magic library");
        Add(table,"base.building.magic_library.description","Хранит бестиарий существ, встреченных в боях.","Keeps a bestiary of creatures encountered in battle.");
        Add(table,"base.magic_library.title","МАГИЧЕСКАЯ БИБЛИОТЕКА","MAGIC LIBRARY");
        Add(table,"base.magic_library.subtitle","Бестиарий встреченных существ","Bestiary of encountered creatures");
        Add(table,"base.magic_library.count","Открыто: {0}","Discovered: {0}");
        Add(table,"base.magic_library.empty","В бестиарии пока нет записей.\nВстретьте монстра в бою, чтобы добавить его сюда.","The bestiary has no entries yet.\nEncounter a monster in battle to add it here.");
        EditorUtility.SetDirty(table);
    }
    private static void Add(LocalizationTable table,string key,string ru,string en){var entry=table.entries.Find(x=>x.key==key);if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}while(entry.values.Count<table.languages.Count)entry.values.Add("");int ri=table.languages.IndexOf("ru"),ei=table.languages.IndexOf("en");if(ri>=0)entry.values[ri]=ru;if(ei>=0)entry.values[ei]=en;}
    private static Sprite PanelSprite() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Building Construction Tooltip.prefab").GetComponent<Image>().sprite;
    private static GameObject Rect(string name,Transform parent,Vector2 position,Vector2 size){var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rt=(RectTransform)go.transform;rt.anchoredPosition=position;rt.sizeDelta=size;return go;}
    private static Image ImageNode(string name,Transform parent,Vector2 position,Vector2 size){var image=Rect(name,parent,position,size).AddComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;return image;}
    private static Text TextNode(string name,Transform parent,string value,int fontSize,TextAnchor alignment,Color color){var text=Rect(name,parent,Vector2.zero,new Vector2(100,30)).AddComponent<Text>();text.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");text.fontSize=fontSize;text.alignment=alignment;text.color=color;text.text=value;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.supportRichText=false;return text;}
    private static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;}
    private static void At(RectTransform r,float x,float y,float w,float h,Vector2 anchor){r.anchorMin=r.anchorMax=r.pivot=anchor;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private static void Set(Object target,string field,Object value){var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
    private static void Localize(Text text,string key){var local=text.GetComponent<LocalizedText>()??text.gameObject.AddComponent<LocalizedText>();var so=new SerializedObject(local);so.FindProperty("key").stringValue=key;so.ApplyModifiedPropertiesWithoutUndo();}
}
#endif
