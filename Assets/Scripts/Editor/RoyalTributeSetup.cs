#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Quests;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RoyalTributeSetup
{
    public const string QuestPath="Assets/Resources/Quests/Royal Tribute.asset";
    public const string DialoguePath="Assets/Prefabs/Quests/00 Royal Tribute.prefab";
    public const string PanelPath="Assets/Prefabs/UI/HUD/Royal Quest Panel.prefab";
    static void Set(Object o,string field,Object value){var s=new SerializedObject(o);s.FindProperty(field).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Flag(Object o,string field,bool value){var s=new SerializedObject(o);s.FindProperty(field).boolValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static ResourceType Resource(string id)=>AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+id+".asset");
    public static bool IsRoyal(QuestPanel p)=>new SerializedObject(p).FindProperty("showParallelQuest").boolValue;
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new System.Exception("Exit Play Mode first");
        var q=AssetDatabase.LoadAssetAtPath<QuestDefinition>(QuestPath);
        if(q==null){q=ScriptableObject.CreateInstance<QuestDefinition>();q.id="royal_magic_tribute";AssetDatabase.CreateAsset(q,QuestPath);}
        q.title="Королевская дань";q.description="Приказ короля: передать магическую руду в казну до истечения срока. При провале будут конфискованы еда, материалы и оружие. Люди, воины и телеги останутся.";q.inProgressStatus="Соберите руду и сдайте её в королевство";
        q.parallelQuest=true;q.deadlineDays=30;q.consumeRequirementsOnClaim=true;q.unlockRewardsOnCompletion=false;
        q.requirements=new(){new QuestDefinition.Requirement{resource=Resource("MagicOre"),amount=30,purpose="Передать в королевскую казну"}};
        q.resourceRewards=new(){new QuestDefinition.ResourceReward{resource=Resource("Crown"),amount=10}};q.unlockRewards.Clear();
        q.penaltyResource=Resource("Crown");q.penaltyAmount=30;q.confiscateResourcesOnFailure=true;q.protectedResources=new(){Resource("Human"),Resource("Archer"),Resource("Swordsman"),Resource("Cart")};EditorUtility.SetDirty(q);
        var catalog=AssetDatabase.LoadAssetAtPath<QuestCatalog>(QuestSetup.CatalogPath);if(!catalog.quests.Contains(q))catalog.quests=catalog.quests.Concat(new[]{q}).ToArray();EditorUtility.SetDirty(catalog);
        var go=new GameObject("00 Royal Tribute");var d=go.AddComponent<QuestDialogueDefinition>();d.id="scribe_royal_magic_tribute";d.speakerName="Королевский писарь";d.portrait=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Dialogue/Royal Scribe.png");d.quest=q;
        d.lines=new[]{"Слушай внимательно, рыцарь. Королю нет дела до твоих жалких трудностей. Через тридцать дней ты сдашь в казну тридцать единиц магической руды. Не вздумай заставлять нас ждать.","Исполнишь приказ — получишь десять влияния. Опоздаешь — лишишься всех припасов, материалов и оружия, а твоё влияние уменьшится на тридцать. Людей и телеги пока оставим. Считай это королевской милостью."};
        var dialogue=PrefabUtility.SaveAsPrefabAsset(go,DialoguePath).GetComponent<QuestDialogueDefinition>();Object.DestroyImmediate(go);
        var firstRoot=PrefabUtility.LoadPrefabContents(QuestDialogueSetup.FirstPath);var first=firstRoot.GetComponent<QuestDialogueDefinition>();first.prerequisite=q;first.prerequisiteAccepted=true;PrefabUtility.SaveAsPrefabAsset(firstRoot,QuestDialogueSetup.FirstPath);PrefabUtility.UnloadPrefabContents(firstRoot);
        var viewRoot=PrefabUtility.LoadPrefabContents(QuestDialogueSetup.ViewPath);var view=viewRoot.GetComponent<QuestDialogueView>();var so=new SerializedObject(view);var list=so.FindProperty("conversations");var existing=new System.Collections.Generic.List<Object>();for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue!=dialogue)existing.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);list.arraySize=existing.Count+1;list.GetArrayElementAtIndex(0).objectReferenceValue=dialogue;for(int i=0;i<existing.Count;i++)list.GetArrayElementAtIndex(i+1).objectReferenceValue=existing[i];so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(viewRoot,QuestDialogueSetup.ViewPath);PrefabUtility.UnloadPrefabContents(viewRoot);
        var panel=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(QuestSetup.PanelPath));panel.name="Royal Quest Panel";var p=panel.GetComponent<QuestPanel>();Flag(p,"showParallelQuest",true);var align=panel.GetComponent<BesideMilitaryPanel>();if(align!=null)Object.DestroyImmediate(align);panel.AddComponent<QuestPanelStackBelow>();
        var title=(Text)new SerializedObject(p).FindProperty("title").objectReferenceValue;
        var countdownGo=new GameObject("Days Remaining",typeof(RectTransform),typeof(Text),typeof(LayoutElement));countdownGo.transform.SetParent(panel.transform,false);countdownGo.transform.SetSiblingIndex(1);var text=countdownGo.GetComponent<Text>();text.font=title.font;text.fontSize=20;text.text="Осталось дней: 30 / 30";text.color=new Color(.94f,.86f,.62f);text.raycastTarget=false;countdownGo.GetComponent<LayoutElement>().minHeight=countdownGo.GetComponent<LayoutElement>().preferredHeight=30;Set(p,"deadlineLabel",text);
        var desc=(Text)new SerializedObject(p).FindProperty("description").objectReferenceValue;desc.text=q.description;desc.GetComponent<LayoutElement>().preferredHeight=150;desc.GetComponent<LayoutElement>().minHeight=150;
        title.text=q.title;
        var details=panel.transform.Find("Hover Details/Details Content");var penalty=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HUD/Quest Objective Row.prefab"),details);penalty.name="Failure Penalty";penalty.GetComponent<QuestObjectiveRow>().PresentReward(q.penaltyResource.resourceIcon,"−30","Штраф");Set(p,"penaltyRow",penalty.GetComponent<QuestObjectiveRow>());
        var prefab=PrefabUtility.SaveAsPrefabAsset(panel,PanelPath);Object.DestroyImmediate(panel);
        var active=SceneManager.GetActiveScene();
        foreach(var path in new[]{"Assets/Scenes/Base.unity","Assets/Scenes/World.unity"})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            var panels=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<QuestPanel>(true)).ToArray();var primary=panels.First(x=>!IsRoyal(x));var royal=panels.FirstOrDefault(IsRoyal);
            if(royal==null)royal=((GameObject)PrefabUtility.InstantiatePrefab(prefab,primary.transform.parent)).GetComponent<QuestPanel>();
            Set(royal.GetComponent<QuestPanelStackBelow>(),"primaryPanel",primary);
            foreach(var v in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<QuestDialogueView>(true)))Set(v,"parallelQuestPanel",royal);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(DialoguePath);
    }
}
#endif
