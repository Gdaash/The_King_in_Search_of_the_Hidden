#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.UI;
using GameFoundation.MetaProgression;

public static class HudMotionAndRecallSetup
{
    const string Folder="Assets/Sprites/Ui/";
    static void Save(string name,int width,int height,System.Func<int,int,Color> pixel)
    {
        string path=Folder+name+".png";
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)texture.SetPixel(x,height-1-y,pixel(x,y));
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
    }
    static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+name+".png");
    static void Configure(GameObject root,Material glyph)
    {
        foreach(var enemy in root.GetComponentsInChildren<EnemyRosterView>(true))
        {var scroll=enemy.transform.Find("Enemy Scroll");if(scroll&&!scroll.GetComponent<CanvasGroup>())scroll.gameObject.AddComponent<CanvasGroup>();}
        foreach(var controls in root.GetComponentsInChildren<GameSpeedControls>(true))
        {
            foreach(var button in controls.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                var label=button.transform.Find("Pixel Label");if(label)label.GetComponent<UnityEngine.UI.Image>().material=glyph;
                var selected=button.transform.Find("Selected");if(!selected)continue;
                var image=selected.GetComponent<UnityEngine.UI.Image>();image.sprite=Sprite("Speed Selection Pixel");image.type=UnityEngine.UI.Image.Type.Simple;image.color=Color.white;image.raycastTarget=false;
                var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-4);rect.sizeDelta=image.sprite.rect.size*2;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
        }
        foreach(var toggle in root.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))
        {
            if(toggle.name!="Light Auto Repeat")continue;
            var rect=(RectTransform)toggle.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(452,22.4f);rect.sizeDelta=new Vector2(40,40);
            var canvas=toggle.GetComponentInParent<Canvas>().rootCanvas;
            if(toggle.transform.parent!=canvas.transform)toggle.transform.SetParent(canvas.transform,false);
            rect.anchoredPosition=new Vector2(452,22.4f);
            var frame=toggle.transform.Find("Checkbox").GetComponent<UnityEngine.UI.Image>();frame.sprite=Sprite("Auto Work Checkbox");frame.type=UnityEngine.UI.Image.Type.Simple;frame.color=Color.white;frame.rectTransform.sizeDelta=frame.sprite.rect.size*2;
            if(toggle.graphic && toggle.graphic.name=="Checkmark")toggle.graphic.gameObject.SetActive(false);
            var check=toggle.graphic && toggle.graphic.name=="Pixel Check" ? toggle.graphic.transform : frame.transform.Find("Pixel Check");
            if(!check){check=new GameObject("Pixel Check",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;check.SetParent(frame.transform,false);}
            foreach(var duplicate in toggle.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(duplicate.name=="Pixel Check")duplicate.gameObject.SetActive(duplicate.transform==check);
            check.gameObject.SetActive(true);
            var image=check.GetComponent<UnityEngine.UI.Image>();image.sprite=Sprite("Auto Work Check");image.raycastTarget=false;image.color=Color.white;
            var cr=image.rectTransform;cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(.5f,.5f);cr.anchoredPosition=Vector2.zero;cr.sizeDelta=image.sprite.rect.size*2;
            toggle.graphic=image;toggle.targetGraphic=frame;toggle.toggleTransition=UnityEngine.UI.Toggle.ToggleTransition.None;
            image.enabled=true;image.canvasRenderer.SetAlpha(toggle.isOn?1:0);
            foreach(var c in toggle.GetComponentsInChildren<Component>(true))if(c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);
        }
        foreach(var controller in root.GetComponentsInChildren<WorldEscapeController>(true))
        {
            var s=new SerializedObject(controller);var panel=(GameObject)s.FindProperty("progressPanel").objectReferenceValue;
            if(!panel)continue;
            foreach(string name in new[]{"Fill","Track","Human Icon"}){var child=panel.transform.Find(name);if(child)child.gameObject.SetActive(false);}
            var fill=s.FindProperty("progressFill").objectReferenceValue as UnityEngine.UI.Image;if(fill)fill.gameObject.SetActive(false);
            s.FindProperty("progressFill").objectReferenceValue=null;
            var label=s.FindProperty("progressLabel").objectReferenceValue as TMPro.TMP_Text;if(label)label.gameObject.SetActive(false);
            var view=panel.GetComponent<ReturnedPeopleView>()??panel.AddComponent<ReturnedPeopleView>();
            var vs=new SerializedObject(view);vs.FindProperty("countLabel").objectReferenceValue=label;vs.ApplyModifiedPropertiesWithoutUndo();
            s.FindProperty("returnedPeople").objectReferenceValue=view;s.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        }
    }
    public static void Apply()
    {
        Save("Speed Selection Pixel",15,1,(x,y)=>new Color32(244,199,83,255));
        Save("Auto Work Checkbox",18,18,(x,y)=>
        {
            if(x==0||x==17||y==0||y==17)return new Color32(9,7,12,255);
            if(x==1||y==1)return new Color32(29,23,36,255);
            if(x==16||y==16)return new Color32(98,83,111,255);
            if(x==2||y==2)return new Color32(13,10,18,255);
            if(x==15||y==15)return new Color32(61,50,72,255);
            return new Color32(26,20,33,255);
        });
        string[] pixels={".........h.","........hg.",".......hg..",".h....hg...",".gh..hg....","..ghhg.....","...hg......","...........","..........."};
        Save("Auto Work Check",11,9,(x,y)=>pixels[y][x]=='h'?new Color32(177,239,145,255):pixels[y][x]=='g'?new Color32(87,156,78,255):Color.clear);
        string matPath="Assets/Materials/UI Pixel Glyph.mat";
        var glyph=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(!glyph){glyph=new Material(Shader.Find("UI/Pixel Glyph Tint"));AssetDatabase.CreateAsset(glyph,matPath);}
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(id);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentInChildren<GameSpeedControls>(true)&&!asset.GetComponentInChildren<WorldEscapeController>(true)&&!asset.GetComponentInChildren<EnemyRosterView>(true))continue;
            var root=PrefabUtility.LoadPrefabContents(path);try{Configure(root,glyph);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        EditorSceneManager.OpenScene("Assets/Scenes/World.unity");
        foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())Configure(root,glyph);
        var journal=Object.FindAnyObjectByType<JournalHudLayout>();
        if(journal) { journal.SendMessage("LateUpdate"); EditorUtility.SetDirty(journal); }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        if(!string.IsNullOrEmpty(original)&&original!="Assets/Scenes/World.unity")EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();
    }
}
#endif
