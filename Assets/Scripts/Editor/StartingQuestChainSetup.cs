#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Quests;
using GameFoundation.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartingQuestChainSetup
{
    public const string CraftPath="Assets/Resources/Quests/Build Smithy And Two Bows.asset";
    static void Set(Object o,string field,Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static ContentUnlockDefinition Unlock(string id,string title)
    {
        var path="Assets/Resources/Quests/BuildingUnlocks/"+id+".asset";
        var u=AssetDatabase.LoadAssetAtPath<ContentUnlockDefinition>(path);
        if(u==null){u=ScriptableObject.CreateInstance<ContentUnlockDefinition>();u.id="building."+id;AssetDatabase.CreateAsset(u,path);}
        u.title=title;EditorUtility.SetDirty(u);return u;
    }
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode");
        var smith=Unlock("blacksmith","Кузница");var range=Unlock("archery_range","Стрельбище");var housing=Unlock("housing","Жилой квартал");
        var first=AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestSetup.QuestPath);
        var archers=AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/Recruit Two Archers.asset");
        var craft=AssetDatabase.LoadAssetAtPath<QuestDefinition>(CraftPath);
        if(craft==null){craft=ScriptableObject.CreateInstance<QuestDefinition>();craft.id="build_smithy_two_bows";AssetDatabase.CreateAsset(craft,CraftPath);}
        craft.title="Оружие для защитников";craft.description="Постройте кузницу и изготовьте два лука.";craft.inProgressStatus="Подготовьте оружие для двух лучников";
        craft.requirements=new(){new QuestDefinition.Requirement{buildingId="blacksmith",amount=1,purpose="Построить кузницу"},new QuestDefinition.Requirement{resource=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Bow.asset"),amount=2,purpose="Изготовить два лука"}};
        craft.resourceRewards.Clear();craft.unlockRewards=new(){range};craft.unlockRewardsOnCompletion=false;
        first.resourceRewards.Clear();first.unlockRewards=new(){smith};first.unlockRewardsOnCompletion=false;
        foreach(var goal in first.requirements)if(goal.resource!=null && goal.resource.name=="Wood"){goal.amount=6;goal.purpose="Кузница, два лука и стрельбище";}
        archers.unlockRewardsOnCompletion=false;
        var catalog=AssetDatabase.LoadAssetAtPath<QuestCatalog>(QuestSetup.CatalogPath);catalog.quests=new[]{first,craft,archers};
        foreach(var o in new Object[]{first,craft,archers,catalog})EditorUtility.SetDirty(o);
        var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quests/02 Two Archers.prefab");
        var droot=Object.Instantiate(original);droot.name="02 Smithy And Bows";var d=droot.GetComponent<QuestDialogueDefinition>();d.id="pew_smithy_two_bows";d.quest=craft;d.prerequisite=first;d.lines=new[]{"Припасы собраны. Теперь построй кузницу и сделай два лука. Когда оружие будет готово, мы откроем стрельбище для обучения защитников."};
        var craftDialogue=PrefabUtility.SaveAsPrefabAsset(droot,"Assets/Prefabs/Quests/02 Smithy And Bows.prefab").GetComponent<QuestDialogueDefinition>();Object.DestroyImmediate(droot);
        var ar=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Quests/02 Two Archers.prefab");var ad=ar.GetComponent<QuestDialogueDefinition>();ad.prerequisite=craft;ad.lines=new[]{"Оружие готово. Построй стрельбище и найми двух лучников. За это ты получишь разрешение построить лагерь беженцев."};PrefabUtility.SaveAsPrefabAsset(ar,"Assets/Prefabs/Quests/02 Two Archers.prefab");PrefabUtility.UnloadPrefabContents(ar);
        var viewRoot=PrefabUtility.LoadPrefabContents(QuestDialogueSetup.ViewPath);var v=viewRoot.GetComponent<QuestDialogueView>();var so=new SerializedObject(v);var arr=so.FindProperty("conversations");arr.arraySize=3;arr.GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(QuestDialogueSetup.FirstPath).GetComponent<QuestDialogueDefinition>();arr.GetArrayElementAtIndex(1).objectReferenceValue=craftDialogue;arr.GetArrayElementAtIndex(2).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Quests/02 Two Archers.prefab").GetComponent<QuestDialogueDefinition>();so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(viewRoot,QuestDialogueSetup.ViewPath);PrefabUtility.UnloadPrefabContents(viewRoot);
        var active=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/Base.unity");bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/Base.unity",OpenSceneMode.Additive);
        var buildings=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BaseBuildingConstruction>(true)).ToArray();
        foreach(var b in buildings){var u=b.BuildingId=="blacksmith"?smith:b.BuildingId=="archery_range"?range:b.BuildingId=="housing"?housing:null;if(u!=null)Set(b,"requiredUnlock",u);}
        foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/UI"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset.GetComponentsInChildren<BaseBuildingConstruction>(true).Length==0)continue;
            var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
            foreach(var b in root.GetComponentsInChildren<BaseBuildingConstruction>(true)){var u=b.BuildingId=="blacksmith"?smith:b.BuildingId=="archery_range"?range:b.BuildingId=="housing"?housing:null;if(u!=null){Set(b,"requiredUnlock",u);changed=true;}}
            if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
        }
        var smithButton=buildings.First(b=>b.BuildingId=="blacksmith");var icon=smithButton.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(i=>i.sprite!=null && i.name.Contains("Icon"));if(icon!=null){smith.icon=icon.sprite;craft.requirements[0].buildingIcon=icon.sprite;EditorUtility.SetDirty(smith);EditorUtility.SetDirty(craft);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();
    }
}
#endif
