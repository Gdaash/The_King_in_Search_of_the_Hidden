#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BaseBuildingIslandsSetup
{
    const string HudPath="Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
    const string Folder="Assets/Prefabs/Base/Islands";
    class Binding { public Component component; public string property; public UnityEngine.Object value; }
    static Vector2 Position(string name) => name switch {
        "Fort"=>new(-720,90),"Castle"=>new(-280,90),"Archery Range"=>new(160,90),"Magic Library"=>new(600,90),
        "Housing"=>new(-500 ,-175),"Portal"=>new(-60 ,-175),"Warehouse"=>new(380 ,-175),
        "Refugees"=>new(-720 ,-430),"Square"=>new(-280 ,-430),"Blacksmith"=>new(160 ,-430),"Laboratory"=>new(550 ,-430),_=>Vector2.zero};
    static string Art(string name) => name switch {
        "Castle"=>"Assets/Art/BaseScene/Layers/07_Castle.png", "Housing"=>"Assets/Art/BaseScene/Layers/26_House_6.png",
        "Portal"=>"Assets/Art/BaseScene/Layers/13_Portal.png", "Warehouse"=>"Assets/Art/BaseScene/Layers/19_Warehouse.png",
        "Fort"=>"Assets/Art/BaseScene/Layers/32_Fort.png", "Archery Range"=>"Assets/Art/BaseScene/Layers/31_Archery_Range.png",
        "Magic Library"=>"Assets/Art/BaseScene/Layers/22_Magic_Library.png", "Laboratory"=>"Assets/Art/BaseScene/Layers/23_Laboratory.png",
        "Refugees"=>"Assets/Art/BaseScene/Layers/28_Refugee_Camp.png", "Square"=>"Assets/Art/BaseScene/Layers/29_Market.png",
        "Blacksmith"=>"Assets/Art/BaseScene/Layers/25_Blacksmith.png", _=>throw new Exception(name)};
    static List<Binding> ExternalBindings(GameObject root)
    {
        var list=new List<Binding>();
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true)){
            if(c==null)continue;var s=new SerializedObject(c);var p=s.GetIterator();
            while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue!=null && !EditorUtility.IsPersistent(p.objectReferenceValue)){
                var target=p.objectReferenceValue is Component component?component.gameObject:p.objectReferenceValue as GameObject;
                if(target!=null && target!=root && !target.transform.IsChildOf(root.transform))list.Add(new Binding{component=c,property=p.propertyPath,value=p.objectReferenceValue});
            }
        }
        return list;
    }
    static Image Image(string name,Transform parent,Sprite sprite,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(UnityEngine.UI.Image));go.transform.SetParent(parent,false);
        var image=go.GetComponent<UnityEngine.UI.Image>();image.sprite=sprite;image.raycastTarget=false;image.useSpriteMesh=true;image.preserveAspect=true;
        var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=sprite.rect.size*2;rect.anchoredPosition=position;
        return image;
    }
    static Sprite Crop(string name,Sprite source,Rect rect)
    {
        var path=Folder+"/"+name+" Sprite.asset";var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(existing!=null)return existing;
        var sprite=Sprite.Create(source.texture,rect,new Vector2(.5f,.5f),32,0,SpriteMeshType.FullRect);sprite.name=name;
        AssetDatabase.CreateAsset(sprite,path);return sprite;
    }
    public static void Refine()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{Folder}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);
            try {
                string name=System.IO.Path.GetFileNameWithoutExtension(path).Replace(" Island","");
                ((RectTransform)root.transform).anchoredPosition=Position(name);
                if(name=="Castle"){
                    var image=root.transform.Find("Building").GetComponent<UnityEngine.UI.Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art(name));image.rectTransform.sizeDelta=image.sprite.rect.size*2;image.rectTransform.anchoredPosition=new Vector2(0,95+image.sprite.rect.height);
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var hud=PrefabUtility.LoadPrefabContents(HudPath);
        try {foreach(var island in hud.GetComponentsInChildren<CanvasBuildingIsland>(true)){var rect=(RectTransform)island.transform;rect.anchoredPosition=Position(island.name);PrefabUtility.RecordPrefabInstancePropertyModifications(rect);}PrefabUtility.SaveAsPrefabAsset(hud,HudPath);}finally{PrefabUtility.UnloadPrefabContents(hud);}
        foreach(var island in UnityEngine.Object.FindObjectsByType<CanvasBuildingIsland>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var rect=(RectTransform)island.transform;rect.anchoredPosition=Position(island.name);PrefabUtility.RecordPrefabInstancePropertyModifications(rect);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode");
        System.IO.Directory.CreateDirectory("Temp/BaseIslandBackup");System.IO.File.Copy(HudPath,"Temp/BaseIslandBackup/HUD.prefab",true);System.IO.File.Copy("Assets/Scenes/Base.unity","Temp/BaseIslandBackup/Base.unity",true);
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Prefabs/Base","Islands");
        var root=PrefabUtility.LoadPrefabContents(HudPath);
        try {
            var panel=root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Base Panel");
            if(PrefabUtility.IsAnyPrefabInstanceRoot(panel.gameObject))PrefabUtility.UnpackPrefabInstance(panel.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            var bg=panel.GetComponent<UnityEngine.UI.Image>();bg.enabled=true;bg.sprite=null;bg.type=UnityEngine.UI.Image.Type.Simple;bg.color=new Color32(16,16,22,255);bg.raycastTarget=false;
            var oldCastle=panel.Find("Castle Grounds");if(oldCastle!=null)oldCastle.gameObject.SetActive(false);
            var hex=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Builds/Hex 1.png");
            foreach(var view in panel.GetComponentsInChildren<WorldBuildingButton>(true)){
                var go=view.gameObject;var s=new SerializedObject(view);var tint=(Material)s.FindProperty("unbuiltMaterial").objectReferenceValue;
                var art=AssetDatabase.LoadAssetAtPath<Sprite>(Art(go.name));if(art==null)throw new Exception("Missing sprite: "+Art(go.name));
                if(go.name=="Square")art=Crop("Market",art,new Rect(132,24,72,78));
                if(go.name=="Warehouse")art=Crop("Warehouse",art,new Rect(14,0,119,49));
                var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(236,64);rect.localScale=Vector3.one;rect.anchoredPosition=Position(go.name);
                var hexImage=Image("Hex",go.transform,hex,new Vector2(0,145));hexImage.transform.SetAsFirstSibling();
                var artImage=Image("Building",go.transform,art,new Vector2(0,155+art.rect.height-60));artImage.transform.SetSiblingIndex(1);
                var construction=go.GetComponent<BaseBuildingConstruction>();
                var island=go.AddComponent<CanvasBuildingIsland>();var si=new SerializedObject(island);
                si.FindProperty("construction").objectReferenceValue=construction;si.FindProperty("buttonBackground").objectReferenceValue=go.GetComponent<UnityEngine.UI.Image>();
                si.FindProperty("buttonLabel").objectReferenceValue=go.transform.Find("Label").gameObject;
                si.FindProperty("unbuiltTint").objectReferenceValue=tint;var images=si.FindProperty("buildingImages");images.arraySize=1;images.GetArrayElementAtIndex(0).objectReferenceValue=artImage;si.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Object.DestroyImmediate(view);
                var bindings=ExternalBindings(go);var path=Folder+"/"+go.name+" Island.prefab";
                var clone=UnityEngine.Object.Instantiate(go);clone.name=go.name;
                var asset=PrefabUtility.SaveAsPrefabAsset(clone,path);UnityEngine.Object.DestroyImmediate(clone);
                PrefabUtility.ConvertToPrefabInstance(go,asset,new ConvertToPrefabInstanceSettings{objectMatchMode=ObjectMatchMode.ByName,recordPropertyOverridesOfMatches=false,componentsNotMatchedBecomesOverride=false,gameObjectsNotMatchedBecomesOverride=false,changeRootNameToAssetName=false},InteractionMode.AutomatedAction);
                foreach(var binding in bindings){var so=new SerializedObject(binding.component);so.FindProperty(binding.property).objectReferenceValue=binding.value;so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(binding.component);}
                go.name=path.Substring(path.LastIndexOf('/')+1).Replace(" Island.prefab","");rect=(RectTransform)go.transform;rect.anchoredPosition=Position(go.name);PrefabUtility.RecordPrefabInstancePropertyModifications(rect);PrefabUtility.RecordPrefabInstancePropertyModifications(go);
            }
            PrefabUtility.SaveAsPrefabAsset(root,HudPath);
        } finally{PrefabUtility.UnloadPrefabContents(root);}
        var basePanel=UnityEngine.Object.FindObjectsByType<CanvasBuildingIsland>(FindObjectsInactive.Include,FindObjectsSortMode.None).First().transform.parent;
        var background=basePanel.GetComponent<UnityEngine.UI.Image>();background.enabled=true;background.color=new Color32(16,16,22,255);background.sprite=null;background.raycastTarget=false;PrefabUtility.RecordPrefabInstancePropertyModifications(background);
        var castle=basePanel.Find("Castle Grounds");if(castle!=null){castle.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(castle.gameObject);}
        foreach(var island in basePanel.GetComponentsInChildren<CanvasBuildingIsland>(true)){var rect=(RectTransform)island.transform;rect.anchoredPosition=Position(island.name);PrefabUtility.RecordPrefabInstancePropertyModifications(rect);}
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(basePanel.gameObject.scene);EditorSceneManager.SaveOpenScenes();
        Debug.Log("Eleven scene-authored Canvas building islands created.");
    }
}
#endif


