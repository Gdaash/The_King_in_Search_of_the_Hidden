#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Base;
using GameFoundation.UI;
using GameFoundation.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BaseBuildingButtonLayoutSetup
{
    const string HudPath = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
    static Vector2 Position(string name) => name switch {
        "Magic Library" => new(218,126), "Laboratory" => new(120,174),
        "Castle" => new(325,172), "Blacksmith" => new(193,235),
        "Portal" => new(324,282), "Fort" => new(480,158),
        "Archery Range" => new(480,258), "Warehouse" => new(444,375),
        "Housing" => new(120,353), "Square" => new(265,386),
        "Refugees" => new(310,452), _ => Vector2.zero };
    static void Record(Object o) { EditorUtility.SetDirty(o); if(PrefabUtility.IsPartOfPrefabInstance(o)) PrefabUtility.RecordPrefabInstancePropertyModifications(o); }
    static void Rect(RectTransform r, Vector2 size, Vector2 position) {
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f); r.localScale=Vector3.one; r.localRotation=Quaternion.identity;
        r.sizeDelta=size; r.anchoredPosition=position; Record(r);
    }
    static void Style(Button b, Sprite background) {
        var image=b.GetComponent<Image>(); image.sprite=background; image.type=Image.Type.Sliced; image.color=Color.white; image.enabled=true;image.raycastTarget=true;
        b.targetGraphic=image;b.transition=Selectable.Transition.None;
        var f=b.GetComponent<UnifiedButtonFeedback>(); if(f!=null){ f.enabled=true; var s=new SerializedObject(f);s.FindProperty("animateScale").boolValue=true;s.ApplyModifiedPropertiesWithoutUndo();Record(f); }
        var outline=b.GetComponent<Outline>();if(outline!=null){outline.enabled=true;Record(outline);}
        Record(image);Record(b);
    }
    public static void Configure(GameObject root) {
        var background=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Evolution/Popups/Sprites/Shared/PopupButton9Slice.png");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Icons/Construction Icon.png");
        foreach(var view in root.GetComponentsInChildren<WorldBuildingButton>(true)) {
            var so=new SerializedObject(view);so.FindProperty("constructionPixelPosition").vector2Value=Position(view.name);
            so.FindProperty("buttonSize").vector2Value=new Vector2(236,64);
            so.FindProperty("upgradeOffset").vector2Value=new Vector2(0,-86);so.ApplyModifiedPropertiesWithoutUndo();Record(view);
            Rect((RectTransform)view.transform,new Vector2(236,64),Vector2.zero);
            Style(view.GetComponent<Button>(),background);
            var label=view.transform.Find("Label");
            if(label!=null){label.gameObject.SetActive(true);Record(label.gameObject);var t=label.GetComponent<Text>();if(t!=null){t.fontSize=20;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=16;t.resizeTextMaxSize=20;Record(t);}Rect((RectTransform)label,new Vector2(212,52),Vector2.zero);}
            var build=view.transform.Find("Build Button");
            if(build!=null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(build)!="Assets/Prefabs/Base/Build Button.prefab"){
                Rect((RectTransform)build,new Vector2(236,64),Vector2.zero);Style(build.GetComponent<Button>(),background);
                var buildLabel=build.Find("Label");buildLabel.gameObject.SetActive(true);Record(buildLabel.gameObject);
                var t=buildLabel.GetComponent<Text>();t.text="Построить";t.fontSize=20;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=false;Record(t);
                Rect((RectTransform)buildLabel,new Vector2(164,52),new Vector2(23,0));
                var child=build.Find("Construction Icon");
                if(child==null){var go=new GameObject("Construction Icon",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(build,false);child=go.transform;}
                var im=child.GetComponent<Image>();im.sprite=icon;im.color=Color.white;im.preserveAspect=true;im.raycastTarget=false;Record(im);
                Rect((RectTransform)child,icon.rect.size*2,new Vector2(-87,0));
            }
            var upgrade=view.GetComponentInChildren<BuildingUpgradeButton>(true);
            if(upgrade!=null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(upgrade)!="Assets/Prefabs/Base/Building Upgrade Button.prefab"){Rect((RectTransform)upgrade.transform,new Vector2(216,48),new Vector2(0,-86));Style(upgrade.GetComponent<Button>(),background);}
            foreach(var marker in view.GetComponentsInChildren<RectTransform>(true).Where(x=>x.name=="Action Available Indicator")) {
                if(marker.parent.name=="Build Button" || marker.GetComponentInParent<BuildingUpgradeButton>()!=null)continue;
                marker.anchorMin=marker.anchorMax=new Vector2(1,1);marker.pivot=new Vector2(.5f,.5f);marker.anchoredPosition=new Vector2(8,8);marker.sizeDelta=new Vector2(32,32);marker.localScale=Vector3.one;Record(marker);
                foreach(var g in marker.GetComponentsInChildren<Graphic>(true)){g.raycastTarget=false;Record(g);}
            }
        }
    }
    public static void Run() {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop play first");
        var root=PrefabUtility.LoadPrefabContents(HudPath);
        try {Configure(root);PrefabUtility.SaveAsPrefabAsset(root,HudPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        const string artPath="Assets/Art/BaseScene/Base Scene Artwork.prefab";
        root=PrefabUtility.LoadPrefabContents(artPath);
        try {foreach(var ray in root.GetComponentsInChildren<WorldBuildingRaycaster>(true))ray.enabled=false;PrefabUtility.SaveAsPrefabAsset(root,artPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");table.entries.Single(x=>x.key=="base.building.build").values[table.languages.IndexOf("ru")]="Построить";EditorUtility.SetDirty(table);
        foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Configure(go);
        foreach(var ray in Object.FindObjectsByType<WorldBuildingRaycaster>(FindObjectsInactive.Include,FindObjectsSortMode.None)){ray.enabled=false;Record(ray);}
        foreach(var v in Object.FindObjectsByType<WorldBuildingButton>(FindObjectsInactive.Include,FindObjectsSortMode.None)){v.Resolve();v.UpdatePosition();Record(v.transform);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log("Base building buttons configured: 11 opening buttons, 9 construction buttons, 3 upgrades.");
    }
}
#endif
