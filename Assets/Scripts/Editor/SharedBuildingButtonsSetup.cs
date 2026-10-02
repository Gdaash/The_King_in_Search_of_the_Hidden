#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using GameFoundation.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SharedBuildingButtonsSetup
{
    public const string BuildPath="Assets/Prefabs/Base/Build Button.prefab";
    public const string UpgradePath="Assets/Prefabs/Base/Building Upgrade Button.prefab";
    const string HudPath="Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
    static void SaveTemplate(GameObject source,string path)
    {
        var clone=UnityEngine.Object.Instantiate(source);
        try {
            if(PrefabUtility.IsPartOfPrefabInstance(clone)) PrefabUtility.UnpackPrefabInstance(clone,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            clone.name=source.name;
            var upgrade=clone.GetComponent<BuildingUpgradeButton>();
            if(upgrade!=null){var s=new SerializedObject(upgrade);s.FindProperty("buildingId").stringValue="";s.ApplyModifiedPropertiesWithoutUndo();}
            var badge=clone.GetComponent<BuildingActionAvailabilityIndicator>();
            if(badge!=null){var s=new SerializedObject(badge);s.FindProperty("construction").objectReferenceValue=null;s.ApplyModifiedPropertiesWithoutUndo();}
            PrefabUtility.SaveAsPrefabAsset(clone,path);
        } finally {UnityEngine.Object.DestroyImmediate(clone);}
    }
    static void ClearVisualOverrides(GameObject button)
    {
        var mods=PrefabUtility.GetPropertyModifications(button);
        if(mods==null)return;
        var targets=new HashSet<UnityEngine.Object>();
        foreach(var t in button.GetComponentsInChildren<Transform>(true))
        foreach(var obj in t.GetComponents<Component>().Cast<UnityEngine.Object>().Append(t.gameObject))
        {
            var source=PrefabUtility.GetCorrespondingObjectFromSource(obj);
            while(source!=null){targets.Add(source);source=PrefabUtility.GetCorrespondingObjectFromSource(source);}
        }
        PrefabUtility.SetPropertyModifications(button,mods.Where(m=> !targets.Contains(m.target) ||
            m.target is BuildingUpgradeButton && m.propertyPath=="buildingId" ||
            m.target is BuildingActionAvailabilityIndicator && m.propertyPath=="construction").ToArray());
    }
    static void Configure(GameObject root, bool convert)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(BuildPath);
        foreach(var construction in root.GetComponentsInChildren<BaseBuildingConstruction>(true))
        {
            var button=construction.transform.Find("Build Button").gameObject;
            if(convert && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(button)!=BuildPath)
                PrefabUtility.ConvertToPrefabInstance(button,asset,new ConvertToPrefabInstanceSettings {
                    objectMatchMode=ObjectMatchMode.ByName,recordPropertyOverridesOfMatches=false,
                    componentsNotMatchedBecomesOverride=false,gameObjectsNotMatchedBecomesOverride=false,
                    changeRootNameToAssetName=false },InteractionMode.AutomatedAction);
            ClearVisualOverrides(button);
            var s=new SerializedObject(construction);
            s.FindProperty("buildButton").objectReferenceValue=button.GetComponent<UnityEngine.UI.Button>();
            s.FindProperty("buildButtonLabel").objectReferenceValue=button.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(construction);
            var indicator=button.GetComponent<BuildingActionAvailabilityIndicator>();var si=new SerializedObject(indicator);
            si.FindProperty("construction").objectReferenceValue=construction;si.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(indicator);
            var view=construction.GetComponent<WorldBuildingButton>();if(view==null)continue;var sv=new SerializedObject(view);
            sv.FindProperty("constructionButton").objectReferenceValue=button.GetComponent<RectTransform>();sv.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        }
        foreach(var upgrade in root.GetComponentsInChildren<BuildingUpgradeButton>(true)) {
            var s=new SerializedObject(upgrade);string id=s.FindProperty("buildingId").stringValue;
            ClearVisualOverrides(upgrade.gameObject);
            s.Update();s.FindProperty("buildingId").stringValue=id;s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.RecordPrefabInstancePropertyModifications(upgrade);
        }
    }
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop play mode");
        var root=PrefabUtility.LoadPrefabContents(HudPath);
        try {
            if(AssetDatabase.LoadAssetAtPath<GameObject>(BuildPath)==null)
                SaveTemplate(root.GetComponentsInChildren<BaseBuildingConstruction>(true)[0].transform.Find("Build Button").gameObject,BuildPath);
            SaveTemplate(root.GetComponentsInChildren<BuildingUpgradeButton>(true)[0].gameObject,UpgradePath);
        } finally {PrefabUtility.UnloadPrefabContents(root);}
        root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Base/Base Panel.prefab");
        try {Configure(root,true);PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Base/Base Panel.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}
        root=PrefabUtility.LoadPrefabContents(HudPath);
        try {Configure(root,true);PrefabUtility.SaveAsPrefabAsset(root,HudPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Configure(go,false);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();
        Debug.Log("Shared building buttons linked; only building-specific bindings remain as overrides.");
    }
}
#endif

