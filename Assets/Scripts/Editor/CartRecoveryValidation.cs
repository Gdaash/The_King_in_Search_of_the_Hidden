using System;
using System.Reflection;
using GameFoundation.Base;
using GameFoundation.Quests;
using GameFoundation.Saves;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class CartRecoveryValidation
{
    static readonly Type Saves=typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static void Set(object o,string field,object value)=>o.GetType().GetField(field,Private).SetValue(o,value);
    static void Memory(string key,int value)
    {
        SaveSlotPrefs.GetInt(key,0);var entry=Saves.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{SaveSlotPrefs.SelectedSlot,key});entry.GetType().GetField("value").SetValue(entry,value.ToString());
    }
    static void Write(Action action)
    {
        using(SaveSlotPrefs.Batch())
        {
            Saves.GetField("WritesSuspended",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
            try{action();}finally{Saves.GetField("WritesSuspended",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);}
        }
    }
    public static void Run()
    {
        SessionState.SetString("CartRecoveryQA","RUNNING");
        var disabled=typeof(GameFoundation.PlaytestSettings).GetField("disabledForRun",BindingFlags.Static|BindingFlags.NonPublic);bool previous=(bool)disabled.GetValue(null);disabled.SetValue(null,false);
        var originalScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        try
        {
            foreach(var p in Object.FindObjectsByType<Porter>(FindObjectsSortMode.None))p.gameObject.SetActive(false);
            var manager=GlobalResourceManager.Instance;var cart=Resources.Load<ResourceType>("ResourceTypes/Cart");var wood=Resources.Load<ResourceType>("ResourceTypes/Wood");
            Memory("foundation.building.blacksmith.built",0);Memory(RoyalTributeService.CartDebtKey,0);Memory(CartRecoveryService.RescuesKey,0);
            manager.SetResourceAmount(cart,0);manager.SetResourceAmount(wood,0);
            Check(CartRecoveryService.NeedsHelp(manager,cart,wood,10),"Zero carts and unavailable workshop must allow rescue");
            var carrier=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Porter.prefab"));
            Check(!CartRecoveryService.NeedsHelp(manager,cart,wood,10),"A deployed living carrier must prevent a false zero-cart rescue");carrier.SetActive(false);Object.Destroy(carrier);
            var service=new GameObject("Cart recovery QA").AddComponent<CartRecoveryService>();service.enabled=false;Set(service,"cart",cart);Set(service,"wood",wood);Set(service,"cartCost",10);
            var grant=typeof(CartRecoveryService).GetMethod("Grant",Private);
            Write(()=>grant.Invoke(service,null));Check(manager.GetResourceAmount(cart)==0&&RoyalTributeService.CartDebt==0,"Rescue must not grant carts or add debt outside Base");
            var baseScene=UnityEngine.SceneManagement.SceneManager.CreateScene("Base");UnityEngine.SceneManagement.SceneManager.SetActiveScene(baseScene);
            Write(()=>grant.Invoke(service,null));Check(manager.GetResourceAmount(cart)==2&&RoyalTributeService.CartDebt==2,"First rescue must give two carts and add two ore");
            Write(()=>grant.Invoke(service,null));Check(manager.GetResourceAmount(cart)==2&&RoyalTributeService.CartDebt==2,"Duplicate rescue confirmation must not grant or charge again");
            manager.SetResourceAmount(cart,0);Write(()=>grant.Invoke(service,null));Check(manager.GetResourceAmount(cart)==2&&RoyalTributeService.CartDebt==4,"Repeated total loss must accumulate four ore");
            manager.SetResourceAmount(cart,0);manager.SetResourceAmount(wood,10);Memory("foundation.building.blacksmith.built",1);
            Check(!CartRecoveryService.NeedsHelp(manager,cart,wood,10),"Affordable cart in a built smithy must not trigger rescue");Memory("foundation.building.blacksmith.built",0);
            Check(CartRecoveryService.NeedsHelp(manager,cart,wood,10),"Closed smithy must allow rescue even with enough wood");
            manager.SetResourceAmount(cart,2);
            var day=new GameObject("Royal day QA").AddComponent<DayCycleService>();day.enabled=false;Set(day,"<Day>k__BackingField",10);
            var quest=Object.Instantiate(AssetDatabase.LoadAssetAtPath<QuestDefinition>(RoyalTributeSetup.QuestPath));
            var catalog=ScriptableObject.CreateInstance<QuestCatalog>();catalog.quests=new[]{quest};string prefix="foundation.quest."+quest.id+".";
            Memory(prefix+"accepted",1);Memory(prefix+"claimed",0);Memory(prefix+"completed",0);Memory(prefix+"failed",0);Memory(prefix+"deadline",40);
            var ore=quest.requirements[0].resource;manager.SetResourceAmount(ore,33);
            Check(!quest.HasRequiredStock(manager.GetResourceAmount)&&RoyalTributeService.Required(quest,quest.requirements[0])==34,"Active quota must include four ore cart surcharge");
            manager.SetResourceAmount(ore,34);Check(quest.HasRequiredStock(manager.GetResourceAmount),"Full quota must be recognised");
            Write(()=>RoyalTributeService.Process(catalog,manager));Check(manager.GetResourceAmount(ore)==34&&QuestProgress.DeadlineDay(quest)==40,"Levy must not collect early");
            Check(!QuestProgress.TryClaim(quest,manager),"Royal tribute cannot be claimed early through manual quest rewards");
            manager.SetResourceAmount(quest.penaltyResource,100);Set(day,"<Day>k__BackingField",40);
            Write(()=>RoyalTributeService.Process(catalog,manager));
            Check(manager.GetResourceAmount(ore)==0&&manager.GetResourceAmount(quest.penaltyResource)==110&&RoyalTributeService.CartDebt==0&&QuestProgress.DeadlineDay(quest)==70&&QuestProgress.IsAccepted(quest),"Due levy must collect 34 ore, reward influence and renew for 30 days");
            Write(()=>RoyalTributeService.Process(catalog,manager));Check(manager.GetResourceAmount(quest.penaltyResource)==110,"Repeated evaluation must not settle twice");
            Set(day,"<Day>k__BackingField",70);Write(()=>RoyalTributeService.Process(catalog,manager));
            Check(manager.GetResourceAmount(quest.penaltyResource)==80&&manager.GetResourceAmount(cart)==2&&QuestProgress.DeadlineDay(quest)==100&&!QuestProgress.IsFailed(quest),"Failed levy must apply penalty, protect carts and immediately renew");
            Check(RoyalTributeService.CartDebt==0&&RoyalTributeService.Required(quest,quest.requirements[0])==30,"Next period must return to base quota after settlement");
            Memory(prefix+"claimed",1);Write(()=>RoyalTributeService.Process(catalog,manager));Check(!QuestProgress.IsClaimed(quest)&&QuestProgress.IsAccepted(quest),"Old terminal royal quest must migrate to recurring levy");
            var table=AssetDatabase.LoadAssetAtPath<GameFoundation.Localization.LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
            Check(table.Get("royal.cart_recovery","en")!="royal.cart_recovery"&&table.Get("royal.cart_debt","en").Contains("{0}"),"Recovery and variable debt dialogue must have English localization");
            var canvas=Object.FindAnyObjectByType<UnityEngine.Canvas>();
            var dialogueObject=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(QuestDialogueSetup.ViewPath),canvas.transform);
            var dialogue=dialogueObject.GetComponent<QuestDialogueView>();bool closed=false;
            Check(dialogue.TryShowMessage("Стайк Пью",new[]{CartRecoveryService.RescueText},()=>closed=true),"Base must open Pew recovery dialogue");
            Check(!dialogue.TryShowMessage("Стайк Пью",new[]{"QA"},()=>{}),"Recovery dialogue cannot overlap an existing conversation");
            if(dialogue.IsTyping)dialogue.Advance();Write(()=>dialogue.Advance());Check(closed,"Base dialogue must confirm its recovery callback");
            dialogueObject.SetActive(false);Object.Destroy(dialogueObject);
            SessionState.SetString("CartRecoveryQA","PASS: zero/deployed carts, purchase eligibility, two rescues and duplicate guard, dynamic 34 ore quota, monthly collection, penalty, renewal, migration, RU/EN");
            service.gameObject.SetActive(false);Object.Destroy(service.gameObject);
        }
        finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(originalScene);disabled.SetValue(null,previous);}
    }
}
