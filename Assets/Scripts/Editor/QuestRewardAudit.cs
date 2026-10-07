using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Quests;
using GameFoundation.Base;
using Object = UnityEngine.Object;

public static class QuestRewardAudit
{
    static int checks;
    static void Check(bool ok,string text) { if(!ok)throw new Exception(text); checks++;File.AppendAllText("Temp/QuestRewardAudit.txt","PASS "+text+"\n"); }
    static void Set(Object target,string field,Object value) { var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
    public static async Task RunPlay()
    {
        checks=0;File.WriteAllText("Temp/QuestRewardAudit.txt","Quest reward Play Mode audit\n");
        var resources=new GameObject("Reward inventory").AddComponent<GlobalResourceManager>();
        var wood=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");
        var food=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Berry.asset");
        resources.SetResourceAmount(wood,0);resources.SetResourceAmount(food,0);
        var unlock=ScriptableObject.CreateInstance<ContentUnlockDefinition>();unlock.id=Guid.NewGuid().ToString();unlock.title="Новый указ";
        var quest=ScriptableObject.CreateInstance<QuestDefinition>();quest.id=Guid.NewGuid().ToString();quest.title="Проверка награды";
        quest.requirements.Add(new QuestDefinition.Requirement{resource=wood,amount=1});
        quest.resourceRewards.Add(new QuestDefinition.ResourceReward{resource=wood,amount=3});
        quest.resourceRewards.Add(new QuestDefinition.ResourceReward{resource=wood,amount=2});
        quest.resourceRewards.Add(new QuestDefinition.ResourceReward{resource=food,amount=4});quest.unlockRewards.Add(unlock);
        var next=ScriptableObject.CreateInstance<QuestDefinition>();next.id=Guid.NewGuid().ToString();next.title="Следующее";
        var catalog=ScriptableObject.CreateInstance<QuestCatalog>();catalog.quests=new[]{quest,next};
        var gate=new GameObject("Unlock observer").AddComponent<ContentUnlockGate>();var target=new GameObject("Locked content");gate.targets=new[]{target};gate.requiredUnlock=unlock;gate.Refresh();
        Check(!target.activeSelf&&!ContentUnlocks.IsUnlocked(unlock),"content hidden before claim");
        var decree=ScriptableObject.CreateInstance<RoyalDecreeDefinition>();decree.id=Guid.NewGuid().ToString();decree.requiredUnlock=unlock;
        Check(!decree.IsAvailable&&!RoyalDecreeService.TryEnable(decree),"locked decree cannot be enabled");
        var table=ScriptableObject.CreateInstance<ScientificUpgradeTable>();var entry=new ScientificUpgradeTable.Entry{id=Guid.NewGuid().ToString(),level=1,requiredUnlock=unlock};table.entries.Add(entry);
        var stats=ScriptableObject.CreateInstance<GlobalStats>();Set(stats,"scientificUpgradeTable",table);
        Check(!stats.CanPurchaseUpgrade(entry),"hidden research cannot be purchased");
        Check(!QuestProgress.TryClaim(quest,resources),"unfinished quest cannot claim");
        resources.SetResourceAmount(wood,1);Check(QuestProgress.TryComplete(quest,resources),"quest completes");
        Check(!QuestProgress.IsClaimed(quest)&&resources.GetResourceAmount(food)==0,"completion grants nothing automatically");
        Check(QuestProgress.Current(catalog)==quest,"completed quest stays until reward collected");
        resources.SetResourceAmount(wood,0);Check(QuestProgress.IsComplete(quest),"completion remains after spending objectives");
        var canvas=new GameObject("Reward UI",typeof(Canvas));var panel=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(QuestSetup.PanelPath),canvas.transform).GetComponent<QuestPanel>();
        Set(panel,"catalog",catalog);panel.gameObject.SetActive(true);panel.Refresh();await Task.Delay(100);
        var button=(Button)new SerializedObject(panel).FindProperty("claimButton").objectReferenceValue;
        Check(button.gameObject.activeSelf&&button.IsInteractable(),"claim button visible and interactive");
        Check(button.GetComponent<GameFoundation.UI.UnifiedButtonFeedback>()!=null,"button has hover feedback");
        var hud = new GameObject("Flight resource HUD", typeof(RectTransform)).AddComponent<ResourceUI>();
        hud.transform.SetParent(canvas.transform, false); hud.enabled = false;
        var hudData = new SerializedObject(hud); hudData.FindProperty("showAllGlobalResources").boolValue = true;
        var cells = hudData.FindProperty("globalCells"); cells.arraySize = 2;
        int cellIndex = 0;
        foreach (var resource in new[] { wood, food })
        {
            var icon = new GameObject("Destination", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.transform.SetParent(hud.transform, false); ResourceIconSizing.Apply(icon, resource.resourceIcon);
            icon.rectTransform.anchoredPosition = new Vector2(250 + cellIndex * 100, 200);
            var cell = cells.GetArrayElementAtIndex(cellIndex++);
            cell.FindPropertyRelative("resource").objectReferenceValue = resource;
            cell.FindPropertyRelative("icon").objectReferenceValue = icon;
        }
        hudData.ApplyModifiedPropertiesWithoutUndo(); hud.enabled = true;
        bool reentrant=false;Action<ResourceType,int> listener=(r,n)=>{if(r==wood)reentrant|=QuestProgress.TryClaim(quest,resources);};GlobalResourceManager.OnResourceChanged+=listener;
        button.onClick.Invoke();GlobalResourceManager.OnResourceChanged-=listener;
        Check(!reentrant&&QuestProgress.IsClaimed(quest),"reentrant claim rejected");
        Check(resources.GetResourceAmount(wood)==5&&resources.GetResourceAmount(food)==4,"multiple rewards and duplicates combined once");
        Check(target.activeSelf&&decree.IsAvailable,"unlock activates content immediately");
        Check(stats.CanPurchaseUpgrade(entry)&&!stats.HasUpgrade(entry.id),"research revealed but not bought");
        Check(QuestProgress.Current(catalog)==next,"sequence advances after claim");
        Check(!QuestProgress.TryClaim(quest,resources),"second claim rejected");
        Check(!button.gameObject.activeSelf,"next unfinished quest hides claim button");
        var flight = canvas.GetComponentInChildren<QuestRewardFlight>();
        Check(flight != null && flight.GetComponentsInChildren<Image>().Length == 9,"claim emits icons for each rewarded resource");
        Check(flight.GetComponentsInChildren<Image>().All(i => !i.raycastTarget && i.rectTransform.rect.size == i.sprite.rect.size * 2),"flying icons keep pixel scale and do not block input");
        panel.gameObject.SetActive(false); Time.timeScale = 0;
        await Task.Delay(250);
        Check(flight != null && flight.gameObject.activeInHierarchy,"flight survives hidden quest panel while paused");
        await Task.Delay(1600); Time.timeScale = 1;
        Check(flight == null && resources.GetResourceAmount(wood) == 5 && resources.GetResourceAmount(food) == 4,"paused flight finishes without a second resource grant");
        panel.gameObject.SetActive(false);panel.gameObject.SetActive(true);Check(QuestProgress.IsClaimed(quest),"claim persists across UI reopen");
        var invalid=ScriptableObject.CreateInstance<QuestDefinition>();invalid.id=Guid.NewGuid().ToString();invalid.requirements.Add(new QuestDefinition.Requirement{resource=wood,amount=1});invalid.resourceRewards.Add(new QuestDefinition.ResourceReward{resource=wood,amount=2});invalid.unlockRewards.Add(null);QuestProgress.TryComplete(invalid,resources);
        Check(!QuestProgress.TryClaim(invalid,resources)&&resources.GetResourceAmount(wood)==5&&!QuestProgress.IsClaimed(invalid),"invalid reward rejected before any mutation");
        Object.Destroy(panel.gameObject);Object.Destroy(canvas);Object.Destroy(gate.gameObject);Object.Destroy(target);Object.Destroy(resources.gameObject);
        foreach(var o in new Object[]{quest,next,catalog,unlock,decree,stats,table,invalid})Object.Destroy(o);
        File.AppendAllText("Temp/QuestRewardAudit.txt","PASSED "+checks+" checks\n");
    }
}

