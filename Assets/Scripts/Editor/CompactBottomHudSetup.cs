#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using GameFoundation.UI;
using GameFoundation.Quests;
using GameFoundation.MetaProgression;

public static class CompactBottomHudSetup
{
    static Sprite S(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Alarm 12/"+name+".png");
    static void Configure(GameObject root)
    {
        foreach(var feed in root.GetComponentsInChildren<NotificationFeed>(true))((RectTransform)feed.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,400);
        foreach(var q in root.GetComponentsInChildren<QuestPanel>(true))
        {
            ((RectTransform)q.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,400);
            foreach(var row in q.GetComponentsInChildren<QuestObjectiveRow>(true))
            {
                var text=row.transform.Find("Purpose");if(!text)continue;
                var rect=(RectTransform)text;
                if(rect.sizeDelta.x>200)rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,208);
                var layout=text.GetComponent<UnityEngine.UI.LayoutElement>();if(layout&&layout.preferredWidth>200)layout.preferredWidth=208;
            }
        }
        foreach(var controls in root.GetComponentsInChildren<GameSpeedControls>(true))
        {
            ((RectTransform)controls.transform).sizeDelta=new Vector2(200,76.8f);
            int i=0;foreach(string name in new[]{"Pause","Speed 1","Speed 2","Speed 4"})
            {var button=controls.transform.Find(name) as RectTransform;if(button)button.anchoredPosition=new Vector2(-63+i++*42,4);}
        }
        foreach(var toggle in root.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))if(toggle.name=="Light Auto Repeat")((RectTransform)toggle.transform).anchoredPosition=new Vector2(412,22.4f);
        foreach(var alarm in root.GetComponentsInChildren<AlarmSystem>(true))
        {
            if(!alarm.GetComponent<UnityEngine.UI.Image>())continue;
            var s=new SerializedObject(alarm);s.FindProperty("thresholdSkullYOffset").floatValue=29;s.ApplyModifiedPropertiesWithoutUndo();alarm.RefreshThresholdMarkers();
        }
        foreach(var xp in root.GetComponentsInChildren<PortalTowerExperienceBar>(true))
        {
            var image=xp.GetComponent<UnityEngine.UI.Image>();image.sprite=S("Frame");image.type=UnityEngine.UI.Image.Type.Simple;image.rectTransform.sizeDelta=image.sprite.rect.size*2;
            var track=(RectTransform)xp.fill.transform.parent;track.anchorMin=track.anchorMax=track.pivot=new Vector2(.5f,.5f);track.anchoredPosition=new Vector2(-2,3);track.sizeDelta=new Vector2(354,16);
            xp.fill.sprite=S("Experience Fill");xp.fill.rectTransform.sizeDelta=xp.fill.sprite.rect.size*2;xp.fill.rectTransform.anchoredPosition=Vector2.zero;
            var crystal=xp.transform.Find("Experience Crystal") as RectTransform;if(crystal)crystal.anchoredPosition=new Vector2(-180,11);
            var start=xp.transform.Find("Start Divider");if(!start){start=new GameObject("Start Divider",typeof(RectTransform),typeof(UnityEngine.UI.Image)).transform;start.SetParent(xp.transform,false);}
            var si=start.GetComponent<UnityEngine.UI.Image>();si.sprite=S("Start Divider");si.raycastTarget=false;
            var sr=si.rectTransform;sr.anchorMin=sr.anchorMax=sr.pivot=new Vector2(.5f,.5f);sr.anchoredPosition=new Vector2(-180,2);sr.sizeDelta=si.sprite.rect.size*2;
            start.SetAsLastSibling();if(crystal)crystal.SetAsLastSibling();
        }
        var exp=root.GetComponentInChildren<PortalTowerExperienceBar>(true);var bar=root.GetComponentsInChildren<AlarmSystem>(true).FirstOrDefault(a=>a.GetComponent<UnityEngine.UI.Image>());
        var speed=root.GetComponentInChildren<GameSpeedControls>(true);var ret=root.GetComponentInChildren<WorldReturnPanel>(true);
        if(exp&&bar&&speed&&ret)
        {
            var layout=speed.GetComponent<BottomRunBarsLayout>();if(!layout)layout=speed.gameObject.AddComponent<BottomRunBarsLayout>();
            layout.experience=(RectTransform)exp.transform;layout.alarm=(RectTransform)bar.transform;layout.controls=(RectTransform)speed.transform;layout.returnPanel=(RectTransform)ret.transform;
            var toggle=root.GetComponentsInChildren<UnityEngine.UI.Toggle>(true).FirstOrDefault(t=>t.name=="Light Auto Repeat");if(toggle)layout.autoRepeat=(RectTransform)toggle.transform;
            layout.SendMessage("LateUpdate");
        }
        foreach(var c in root.GetComponentsInChildren<Component>(true))if(c)PrefabUtility.RecordPrefabInstancePropertyModifications(c);
    }
    public static void Apply()
    {
        string file="Assets/Sprites/Ui/Alarm 12/Experience Fill.png";AssetDatabase.ImportAsset(file);var importer=(TextureImporter)AssetImporter.GetAtPath(file);
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
        foreach(var id in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/UI"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(id);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset.GetComponentInChildren<QuestPanel>(true)&&!asset.GetComponentInChildren<NotificationFeed>(true)&&!asset.GetComponentInChildren<GameSpeedControls>(true)&&!asset.GetComponentInChildren<PortalTowerExperienceBar>(true)&&!asset.GetComponentInChildren<AlarmSystem>(true))continue;
            var root=PrefabUtility.LoadPrefabContents(path);try{Configure(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        string original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(string scene in new[]{"Base","World"})
        {
            EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");foreach(var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())Configure(root);
            var journal=Object.FindAnyObjectByType<JournalHudLayout>();if(journal)journal.SendMessage("LateUpdate");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        }
        if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);AssetDatabase.SaveAssets();
    }
}
#endif
