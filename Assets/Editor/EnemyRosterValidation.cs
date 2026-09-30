#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Isolated Play Mode checks: no player resource manager, save clock or progression.</summary>
public static class EnemyRosterValidation
{
    private const string ScenePath = "Assets/EnemyRosterPreview.unity";
    private static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/"+name+".prefab");
    private static void Check(bool condition,string message) { if (!condition) throw new Exception(message); }
    public static void Begin()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play Mode before validation.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var cameraGo = new GameObject("QA Camera",typeof(Camera),typeof(AudioListener)); SceneManager.MoveGameObjectToScene(cameraGo,scene);
            var camera=cameraGo.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.045f,.038f,.055f);camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
            var canvasGo=new GameObject("QA Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));SceneManager.MoveGameObjectToScene(canvasGo,scene);
            canvasGo.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));SceneManager.MoveGameObjectToScene(events,scene);
            var language=new GameObject("Localization",typeof(LocalizationService));SceneManager.MoveGameObjectToScene(language,scene);
            var ls=new SerializedObject(language.GetComponent<LocalizationService>());ls.FindProperty("table").objectReferenceValue=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");ls.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ResourcesUI.prefab"),canvasGo.transform);
            var alarmGo=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HUD/Alarm Bar.prefab"),canvasGo.transform);
            var alarm=alarmGo.GetComponent<AlarmSystem>();var so=new SerializedObject(alarm);
            so.FindProperty("difficultyTable").objectReferenceValue=null;so.FindProperty("dangerLevelNotificationPrefab").objectReferenceValue=null;
            var thresholds=so.FindProperty("thresholds");thresholds.arraySize=2;
            for(int i=0;i<2;i++)
            {
                var threshold=thresholds.GetArrayElementAtIndex(i);threshold.FindPropertyRelative("alarmValue").floatValue=25+i*25;
                threshold.FindPropertyRelative("minSpawnInterval").floatValue=10000;threshold.FindPropertyRelative("maxSpawnInterval").floatValue=10000;
                threshold.FindPropertyRelative("minEnemiesPerWave").intValue=1;threshold.FindPropertyRelative("maxEnemiesPerWave").intValue=1;
                var enemies=threshold.FindPropertyRelative("enemies");enemies.arraySize=1;
                enemies.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue=Prefab(i==0?"Octopus":"CursedMage");
                enemies.GetArrayElementAtIndex(0).FindPropertyRelative("weight").intValue=1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var panel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyRosterSetup.PanelPath),canvasGo.transform);
            so=new SerializedObject(panel.GetComponent<EnemyRosterView>());so.FindProperty("alarm").objectReferenceValue=alarm;so.ApplyModifiedPropertiesWithoutUndo();
            var hex=new GameObject("Blocked hex for wave test",typeof(HexBlocker));SceneManager.MoveGameObjectToScene(hex,scene);hex.transform.position=new Vector3(4,0,0);
            // Keep this isolated fixture blocked: it has no surrounding hexes to hold it closed.
            hex.GetComponent<HexBlocker>().enabled=false;
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        finally {EditorSceneManager.CloseScene(scene,true);}
        SessionState.SetString("EnemyRosterPreviousStart",AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);EditorApplication.isPlaying=true;
    }
    public static void StartWave()
    {
        var panel=Object.FindAnyObjectByType<EnemyRosterView>();
        Check(panel.GetComponent<CanvasGroup>().alpha==0,"Panel must be hidden before the first spawn.");
        Check(panel.GetComponentsInChildren<EnemyRosterItemView>().Length==2,"Both future spawn types must have rows.");
        AlarmSystem.Instance.SetAlarm(25);
        Check(panel.GetComponent<CanvasGroup>().alpha==0,"A warning effect is not yet a living enemy.");
        Debug.Log("PASS: spawn-list types prepared, panel hidden before and during initial warning.");
    }
    public static void CheckLifecycle()
    {
        var alarm=AlarmSystem.Instance;var octopus=Prefab("Octopus");var mage=Prefab("CursedMage");
        Check(alarm.GetAliveEnemyCount(octopus)==1,"Real AlarmSystem wave did not register its Octopus.");
        var panel=Object.FindAnyObjectByType<EnemyRosterView>();
        Check(panel.GetComponent<CanvasGroup>().alpha==1,"Panel must appear with the first real spawn.");
        Check(alarm.GetAliveEnemyCount(mage)==0,"Future waves must not inflate live counts.");
        var first=Object.FindAnyObjectByType<AlarmSpawnedEnemy>().gameObject;
        var second=Object.Instantiate(octopus,new Vector3(4,2,0),Quaternion.identity);alarm.RegisterSpawnedEnemy(octopus,second);alarm.RegisterSpawnedEnemy(octopus,second);
        Check(alarm.GetAliveEnemyCount(octopus)==2,"Duplicate registration changed the count.");
        second.GetComponent<Health>().TakeDamage(100000,DamageType.Physical);
        Check(alarm.GetAliveEnemyCount(octopus)==1,"Dead enemies must disappear from counts immediately.");
        first.SetActive(false);Check(alarm.GetAliveEnemyCount(octopus)==0,"Inactive enemy still counted.");
        first.SetActive(true);Check(alarm.GetAliveEnemyCount(octopus)==1,"Re-enabled living enemy missing.");
        Object.DestroyImmediate(first);Check(alarm.GetAliveEnemyCount(octopus)==0,"Destroyed enemy still counted.");
        var spawner=new GameObject("QA Object Spawner").AddComponent<ObjectSpawner>();var so=new SerializedObject(spawner);so.FindProperty("prefabToSpawn").objectReferenceValue=mage;so.ApplyModifiedPropertiesWithoutUndo();spawner.SpawnObject();
        Check(alarm.GetAliveEnemyCount(mage)==1,"ObjectSpawner must also register enemies on the map.");
        foreach(var member in Object.FindObjectsByType<AlarmSpawnedEnemy>())if(!member.GetComponent<Health>().IsDead)member.GetComponent<Health>().TakeDamage(100000,DamageType.Physical);
        Check(alarm.GetAliveEnemyCount(mage)==0 && alarm.GetAliveEnemyCount(octopus)==0,"Counts did not reach zero.");
        Check(panel.GetComponent<CanvasGroup>().alpha==1,"Panel should retain zero counts between waves.");
        for(int i=0;i<3;i++){var unit=Object.Instantiate(octopus,new Vector3(3+i,0,0),Quaternion.identity);alarm.RegisterSpawnedEnemy(octopus,unit);}spawner.SpawnObject();
        panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);
        Check(panel.GetComponentsInChildren<EnemyRosterItemView>().Length==2,"Reopening duplicated rows.");
        var row=panel.GetComponentsInChildren<EnemyRosterItemView>().First(x=>x.name=="Enemy Octopus");
        Check(row.transform.Find("Count").GetComponent<UnityEngine.UI.Text>().text=="3","Displayed count is stale.");
        Time.timeScale=0;row.OnSelect(new BaseEventData(EventSystem.current));
        Debug.Log("PASS: real wave; future type zero; duplicate registration; immediate death; disable/re-enable; destroy; secondary spawner; zero state; panel reopening. Tooltip opened while paused.");
    }
    public static void CheckTooltip()
    {
        var panel=Object.FindAnyObjectByType<EnemyRosterView>();var tooltip=panel.GetComponentInChildren<UnitDescriptionTooltip>(true);
        Check(tooltip.GetComponent<CanvasGroup>().alpha==1,"Tooltip does not open while paused.");
        var card=tooltip.GetComponentInChildren<UnitDescriptionView>(true);
        Check(card.Definition.isEnemy && card.Definition.unitPrefab==Prefab("Octopus"),"Wrong tooltip data.");
        var texts=card.GetComponentsInChildren<UnityEngine.UI.Text>().Select(t=>t.text).ToArray();
        Check(!texts.Any(t=>t.Contains("Новобранец") || t.Contains("Указ выключен") || t.Contains("Новый день")),"Friendly-only rules leaked into an enemy tooltip.");
        foreach(var item in panel.GetComponentsInChildren<EnemyRosterItemView>())
        {
            var icon=item.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>();
            Check(Vector2.Distance(icon.rectTransform.rect.size,icon.sprite.rect.size*2)<.01f,"Enemy icon violates native 2x sizing.");
        }
        Debug.Log("PASS: shared tooltip opens at pause, enemy definition and icon sizes correct, no player-only rules.");
    }
    public static void Capture(string path,int width,int height)
    {
        var canvas=GameObject.Find("QA Canvas").GetComponent<Canvas>();var camera=GameObject.Find("QA Camera").GetComponent<Camera>();
        var rt=new RenderTexture(width,height,24);var pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();foreach(var group in canvas.GetComponentsInChildren<UnityEngine.UI.VerticalLayoutGroup>())UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,pixels.EncodeToPNG());
        }
        finally{RenderTexture.active=previous;camera.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(pixels);}
    }
    public static void End()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play Mode first.");
        EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("EnemyRosterPreviousStart",""));
        SessionState.EraseString("EnemyRosterPreviousStart");AssetDatabase.DeleteAsset(ScenePath);
    }
}
#endif
