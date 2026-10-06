#if UNITY_EDITOR
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using GameFoundation.Base;
using GameFoundation.MetaProgression;
using GameFoundation.Bestiary;
using GameFoundation.UI;

public static class DirectReferenceMigration
{
    static readonly Dictionary<string,string> Buttons = new() { {"portalButton", "Base Panel/Portal"},{"laboratoryButton", "Base Panel/Laboratory"},{"nextDayButton", "Base Panel/Next Day"},{"confirmDayButton", "Next Day Confirmation/Confirm"},{"cancelDayButton", "Next Day Confirmation/Cancel"},{"closeDayButton", "Next Day Confirmation/Close"},{"warehouseButton", "Base Panel/Warehouse"},{"housingButton", "Base Panel/Housing"},{"refugeesButton", "Base Panel/Refugees"},{"squareButton", "Base Panel/Square"},{"blacksmithButton", "Base Panel/Blacksmith"},{"libraryButton", "Base Panel/Magic Library"},{"settingsButton", "Base Panel/Settings"},{"searchPortalsButton", "Global Map/Search Portals"},{"closeMapButton", "Global Map/Close"},{"closeLaboratoryButton", "Laboratory Popup/Window/Close"},{"closeWarehouseButton", "Warehouse Popup/Close"},{"buyCartButton", "Warehouse Popup/Buy Cart"},{"closeHousingButton", "Housing Popup/Close"},{"closeRefugeesButton", "Refugees Popup/Close"},{"admitRefugeeButton", "Refugees Popup/Admit"},{"closeSquareButton", "Square Popup/Close"},{"closeBlacksmithButton", "Blacksmith Popup/Close"},{"closeLibraryButton", "Magic Library Popup/Window/Close"} };
    static int changed;
    public static string Validate()
    {
        var refs=ProjectReferences.Instance;
        if(refs==null || !PlayerSettings.GetPreloadedAssets().Contains(refs)) throw new Exception("Project references are not preloaded.");
        if(refs.resources.Length==0 || refs.portalLocations.Length!=5) throw new Exception("Incomplete reference catalog.");
        if(refs.resources.Any(r=>r==null || string.IsNullOrEmpty(r.Id)) || refs.resources.Select(r=>r.Id).Distinct().Count()!=refs.resources.Length)
            throw new Exception("Missing or duplicate resource ID.");
        foreach(var resource in refs.resources)
        {
            string original=resource.name;
            try { resource.name="Renamed resource"; if(ResourceCatalog.Find(resource.Id)!=resource)throw new Exception("Resource rename failed."); }
            finally{resource.name=original;}
        }
        int bindings=0;
        foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
                var references=new List<(Component owner,string field,UnityEngine.Object value)>();
                foreach(var controller in all.SelectMany(t=>t.GetComponents<BaseUIController>()))
                    foreach(var field in Buttons.Keys)
                    {
                        var value=new SerializedObject(controller).FindProperty(field).objectReferenceValue;
                        if(value!=null) references.Add((controller,field,value));
                    }
                foreach(var controller in all.SelectMany(t=>t.GetComponents<WorldMilitaryDeploymentController>()))
                {
                    var value=new SerializedObject(controller).FindProperty("portal").objectReferenceValue;
                    if(value==null)throw new Exception("Deployment portal is not wired in "+path);
                    references.Add((controller,"portal",value));
                }
                foreach(var controller in all.SelectMany(t=>t.GetComponents<WorldEscapeController>()))
                {
                    var value=new SerializedObject(controller).FindProperty("portalTowerHealth").objectReferenceValue;
                    if(value==null)throw new Exception("Escape health is not wired in "+path);
                    references.Add((controller,"portalTowerHealth",value));
                }
                foreach(var controller in all.SelectMany(t=>t.GetComponents<WorldBuildingButton>()))
                {
                    var so=new SerializedObject(controller);
                    if(so.FindProperty("artworkReference").objectReferenceValue==null || so.FindProperty("artworkSprites").arraySize==0)
                        throw new Exception("Building artwork is not wired: "+controller.name);
                }
                for(int i=0;i<all.Length;i++) all[i].name="Renamed "+i;
                foreach(var link in references)
                    if(new SerializedObject(link.owner).FindProperty(link.field).objectReferenceValue!=link.value)
                        throw new Exception("Reference changed after rename: "+link.field);
                bindings+=references.Count;
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        return "PASS: resource rename, "+bindings+" scene bindings after renaming all scene objects, portal and building artwork references.";
    }
    public static void EnsureProjectReferences()
    {
        const string path="Assets/Resources/Project References.asset";
        var refs=AssetDatabase.LoadAssetAtPath<ProjectReferences>(path);
        if(refs==null){refs=ScriptableObject.CreateInstance<ProjectReferences>();AssetDatabase.CreateAsset(refs,path);}
        refs.resources=AssetDatabase.FindAssets("t:ResourceType").Select(g=>AssetDatabase.LoadAssetAtPath<ResourceType>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        refs.portalLocations=AssetDatabase.FindAssets("t:PortalLocationDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<PortalLocationDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        refs.audioLibrary=AssetDatabase.LoadAssetAtPath<GameFoundation.Audio.GameAudioLibrary>("Assets/Resources/GameAudioLibrary.asset");
        refs.buildingUpgrades=AssetDatabase.LoadAssetAtPath<BuildingUpgradeCatalog>("Assets/Resources/Base/Building Upgrades.asset");
        refs.crtSettings=AssetDatabase.LoadAssetAtPath<ThoseUnderHexCrtSettings>("Assets/Resources/CRT/ThoseUnderHex CRT Settings.asset");
        refs.crtOverlayMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CRT/ThoseUnderHex CRT Overlay.mat");
        if(refs.audioLibrary==null || refs.buildingUpgrades==null || refs.crtSettings==null || refs.crtOverlayMaterial==null)
            throw new Exception("Missing project catalog reference.");
        EditorUtility.SetDirty(refs);AssetDatabase.SaveAssets();
        PlayerSettings.SetPreloadedAssets(PlayerSettings.GetPreloadedAssets().Where(a=>a!=null).Append(refs).Distinct().ToArray());
        Debug.Log("Project references configured.");
    }
    static void Set(SerializedObject so, string field, UnityEngine.Object value)
    {
        var p=so.FindProperty(field);
        if(p==null || value==null || p.objectReferenceValue==value) return;
        p.objectReferenceValue=value; changed++;
    }
    public static void Wire(GameObject[] roots)
    {
        var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach(var c in all.SelectMany(t=>t.GetComponents<BaseUIController>()))
        {
            var so=new SerializedObject(c);
            var world=so.FindProperty("worldBuildingButtons").objectReferenceValue as Transform;
            foreach(var pair in Buttons)
            {
                var target=c.transform.Find(pair.Value);
                if(target==null && world!=null && pair.Value.StartsWith("Base Panel/")) target=world.Find(pair.Value.Substring(11));
                if(target!=null) Set(so,pair.Key,target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>());
            }
            Set(so,"refugeesAvailableText",c.transform.Find("Refugees Popup/Available")?.GetComponent<Text>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var c in all.SelectMany(t=>t.GetComponents<WorldBuildingButton>()))
        {
            var so=new SerializedObject(c);
            var rootName=so.FindProperty("artworkRoot").stringValue;
            var artwork=all.FirstOrDefault(t=>t.name==rootName);
            Set(so,"artworkReference",artwork);
            var layers=so.FindProperty("artworkLayers");
            if(artwork!=null)
            {
                var values=new List<SpriteRenderer>();
                for(int i=0;i<layers.arraySize;i++)
                {
                    var renderer=artwork.Find(layers.GetArrayElementAtIndex(i).stringValue)?.GetComponent<SpriteRenderer>();
                    if(renderer==null) throw new Exception("Missing artwork for "+c.name);
                    values.Add(renderer);
                }
                var field=so.FindProperty("artworkSprites"); field.arraySize=values.Count;
                for(int i=0;i<values.Count;i++) field.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
                changed++;
            }
            Set(so,"buildingLabel",(c.transform.Find("Label")??c.transform.Find("Button Visual/Label"))?.gameObject);
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach(var c in all.SelectMany(t=>t.GetComponents<SkillButton>()))
        {
            var so=new SerializedObject(c);
            Set(so,"costResourceIconImage",c.transform.Find("CostIconBacking/CostResourceIcon")?.GetComponent<Image>());
            Set(so,"upgradeIconImage",c.transform.Find("UpgradeIcon")?.GetComponent<Image>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        var tower=all.FirstOrDefault(t=>t.name=="PortalTower");
        if(tower!=null)
        {
            foreach(var c in all.SelectMany(t=>t.GetComponents<WorldMilitaryDeploymentController>()))
            { var so=new SerializedObject(c); Set(so,"portal",tower); so.ApplyModifiedPropertiesWithoutUndo(); }
            foreach(var c in all.SelectMany(t=>t.GetComponents<WorldEscapeController>()))
            { var so=new SerializedObject(c); Set(so,"portalTowerHealth",tower.GetComponent<Health>()??tower.GetComponentInChildren<Health>(true)); so.ApplyModifiedPropertiesWithoutUndo(); }
        }
        var stats=AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");
        foreach(var c in all.SelectMany(t=>t.GetComponents<BuildingActionAvailabilityIndicator>()))
        { var so=new SerializedObject(c); Set(so,"laboratoryStats",stats); so.ApplyModifiedPropertiesWithoutUndo(); }
    }
    public static string Run()
    {
        if(Application.isPlaying) throw new Exception("Exit Play Mode before migration.");
        changed=0;
        foreach(var guid in AssetDatabase.FindAssets("t:ResourceType"))
        {
            var asset=AssetDatabase.LoadAssetAtPath<ResourceType>(AssetDatabase.GUIDToAssetPath(guid));
            var so=new SerializedObject(asset); var id=so.FindProperty("persistentId");
            if(string.IsNullOrEmpty(id.stringValue)){id.stringValue=asset.name;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);}
        }
        var enemies=AssetDatabase.FindAssets("t:UnitDescriptionDefinition").Select(g=>AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(d=>d.isEnemy && d.unitPrefab!=null).Select(d=>AssetDatabase.GetAssetPath(d.unitPrefab)).ToHashSet();
        foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                int before=changed;
                if(enemies.Contains(path))
                {
                    var identity=root.GetComponent<EnemyIdentity>()??root.AddComponent<EnemyIdentity>();
                    var so=new SerializedObject(identity);var id=so.FindProperty("persistentId");
                    if(string.IsNullOrEmpty(id.stringValue)){id.stringValue=root.name;so.ApplyModifiedPropertiesWithoutUndo();changed++;}
                }
                Wire(new[]{root});
                if(changed>before) PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            const string folder="Assets/__DirectReferenceMigration";
            if(!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets","__DirectReferenceMigration");
            var copy=folder+"/"+Path.GetFileName(path);
            File.Copy(path,copy,true); AssetDatabase.ImportAsset(copy);
            var scene=EditorSceneManager.OpenScene(copy,OpenSceneMode.Additive);
            try
            {
                Wire(scene.GetRootGameObjects());
                if(!EditorSceneManager.SaveScene(scene,copy))throw new Exception("Cannot save "+path);
                // Copy Unity's serialized output, retaining the original scene GUID and open unsaved scene.
                File.Copy(copy,path,true);
            }
            finally { EditorSceneManager.CloseScene(scene,true); AssetDatabase.DeleteAsset(copy); }
        }
        if(AssetDatabase.IsValidFolder("Assets/__DirectReferenceMigration")) AssetDatabase.DeleteAsset("Assets/__DirectReferenceMigration");
        // Preserve the user's open, unsaved scene. Only update its references.
        for(int i=0;i<SceneManager.sceneCount;i++) Wire(SceneManager.GetSceneAt(i).GetRootGameObjects());
        AssetDatabase.SaveAssets();
        return "Wired "+changed+" references; migrated persistent IDs; open scene kept loaded.";
    }
}
#endif
