#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.MetaProgression;
using Image = UnityEngine.UI.Image;

public static class RunBarsPsdSetup
{
    const string Art = "Assets/Sprites/Ui/Run Bars/";
    static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");
    static void Place(RectTransform t, Vector2 pos, Vector2 size, bool bottom = false)
    {
        t.anchorMin = t.anchorMax = new Vector2(.5f, bottom ? 0 : .5f);
        t.pivot = new Vector2(.5f,.5f); t.anchoredPosition = pos; t.sizeDelta = size; t.localScale = Vector3.one;
    }
    static void ArtImage(Image i, string name)
    {
        i.sprite = S(name); i.color = Color.white; i.type = Image.Type.Simple;
        i.material = null; i.raycastTarget = false; i.preserveAspect = false;
        i.rectTransform.sizeDelta = i.sprite.rect.size * 2;
    }
    static void Edit(string path, Action<GameObject> action)
    {
        var g = PrefabUtility.LoadPrefabContents(path);
        try { action(g); PrefabUtility.SaveAsPrefabAsset(g,path); }
        finally { PrefabUtility.UnloadPrefabContents(g); }
    }
    static void Alarm(AlarmSystem a)
    {
        var so = new SerializedObject(a);
        so.FindProperty("thresholdSkullSprite").objectReferenceValue = S("Skull Active");
        so.FindProperty("inactiveThresholdSkullSprite").objectReferenceValue = S("Skull Inactive");
        so.FindProperty("thresholdSkullYOffset").floatValue = 17;
        so.FindProperty("thresholdDividerYOffset").floatValue = 0;
        var div = so.FindProperty("thresholdDividerSprites"); div.arraySize = 1;
        div.GetArrayElementAtIndex(0).objectReferenceValue = S("Divider");
        so.ApplyModifiedPropertiesWithoutUndo();
        if (a.GetComponent<Image>() == null) return;
        ArtImage(a.GetComponent<Image>(),"Alarm Frame");
        Place((RectTransform)a.transform,new Vector2(269,50),new Vector2(442,30),true);
        foreach (string f in new[]{"fillImage","previewImage"})
        {
            var image = (Image)so.FindProperty(f).objectReferenceValue;
            ArtImage(image,"Alarm Fill"); Place(image.rectTransform,new Vector2(2,-1),new Vector2(354,16));
            image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = 0;
            if(f == "previewImage") image.color = new Color(1,1,1,.4f);
        }
        var ticks = (RectTransform)so.FindProperty("tickContainer").objectReferenceValue;
        // Marker centers follow the same fill width; the last marker stays inside the frame.
        Place(ticks,new Vector2(2,-2),new Vector2(354,42));
        var start = a.transform.Find("Start Divider"); if(start != null) start.gameObject.SetActive(false);
        a.RefreshThresholdMarkers();
    }
    static void Experience(PortalTowerExperienceBar xp)
    {
        var root = (RectTransform)xp.transform;
        ArtImage(root.GetComponent<Image>(),"Experience Frame");
        Place(root,new Vector2(-310,49),new Vector2(360,32),true);
        var track = (RectTransform)xp.fill.transform.parent;
        Place(track,new Vector2(1,0),new Vector2(270,16));
        var trackImage = track.GetComponent<Image>(); trackImage.enabled = false;
        ArtImage(xp.fill,"Experience Fill"); Place(xp.fill.rectTransform,Vector2.zero,new Vector2(270,16));
        xp.fill.type = Image.Type.Filled; xp.fill.fillMethod = Image.FillMethod.Horizontal; xp.fill.fillOrigin = 0;
        xp.label.gameObject.SetActive(false);
        var crystal = root.Find("Experience Crystal");
        if(crystal == null) { var g = new GameObject("Experience Crystal",typeof(RectTransform),typeof(Image));g.transform.SetParent(root,false);crystal=g.transform; }
        var image = crystal.GetComponent<Image>(); ArtImage(image,"Experience Crystal");
        Place(image.rectTransform,new Vector2(-153,11),image.sprite.rect.size*2);
        crystal.SetAsLastSibling();
    }
    static void Layout(GameObject root)
    {
        foreach(var a in root.GetComponentsInChildren<AlarmSystem>(true)) Alarm(a);
        foreach(var xp in root.GetComponentsInChildren<PortalTowerExperienceBar>(true)) Experience(xp);
        foreach(var t in root.GetComponentsInChildren<RectTransform>(true))
        {
            if(t.name == "EscapeUI") t.anchoredPosition = Vector2.zero;
            if(t.name == "Escape Controls")
            {
                var layout=t.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();if(layout!=null)layout.enabled=false;
                var fitter=t.GetComponent<UnityEngine.UI.ContentSizeFitter>();if(fitter!=null)fitter.enabled=false;
                Place(t,new Vector2(0,50),new Vector2(980,100),true);
            }
            if(t.name == "EscapeButton")
            {
                Place(t,new Vector2(-41,0),new Vector2(166,48));
                foreach(var text in t.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.fontSize = 24;
            }
            if(t.name == "Light Auto Repeat")
            {
                Place(t,new Vector2(-520,0),new Vector2(40,40));
                foreach(var text in t.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.gameObject.SetActive(false);
                var label=t.Find("Label");if(label!=null)label.gameObject.SetActive(false);
                var checkbox = t.Find("Checkbox");
                if(checkbox != null) Place((RectTransform)checkbox,Vector2.zero,new Vector2(36,36));
            }
        }
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        AssetDatabase.Refresh();
        foreach(var path in Directory.GetFiles(Art,"*.png"))
        {
            var i=(TextureImporter)AssetImporter.GetAtPath(path);
            i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Single;
            i.spritePixelsPerUnit=32;i.filterMode=FilterMode.Point;i.textureCompression=TextureImporterCompression.Uncompressed;
            i.mipmapEnabled=false;i.alphaIsTransparency=true;i.npotScale=TextureImporterNPOTScale.None;i.SaveAndReimport();
        }
        Edit(AlarmBarPsdSetup.Bar,Layout);
        Edit(PortalTowerProgressionSetup.PrefabPath,Layout);
        Edit("Assets/Prefabs/UI/Screens/World Screen HUD.prefab",Layout);
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/World.unity")throw new Exception("Open World");
        foreach(var root in scene.GetRootGameObjects()) Layout(root);
        // Record overrides on the scene's existing prefab instances without replacing their references.
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Component>(true))
            if(PrefabUtility.IsPartOfPrefabInstance(t))PrefabUtility.RecordPrefabInstancePropertyModifications(t);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("PSD run bars installed with original pixel sizes, bottom-centered layout.");
    }
}
#endif
