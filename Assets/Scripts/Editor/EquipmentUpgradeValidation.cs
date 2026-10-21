using System;
using System.Linq;
using System.Reflection;
using GameFoundation.Base;
using GameFoundation.Combat;
using GameFoundation.Localization;
using GameFoundation.Saves;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class EquipmentUpgradeValidation
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    static readonly Type Saves=typeof(SaveSlotPrefs).Assembly.GetType("GameFoundation.Saves.GameSaveService");
    static void Memory(string key,int value)
    {
        SaveSlotPrefs.GetInt(key,0);
        var entry=Saves.GetMethod("Find",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{SaveSlotPrefs.SelectedSlot,key});
        entry.GetType().GetField("value").SetValue(entry,value.ToString());
    }
    public static void Run(Transform canvas)
    {
        SessionState.SetString("EquipmentQAResult","RUNNING");
        var manager=new GameObject("Equipment QA resources").AddComponent<GlobalResourceManager>();
        var language=new GameObject("Equipment QA language").AddComponent<LocalizationService>();
        language.SetTable(AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset"));
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Blacksmith Popup.prefab");
        foreach(var kind in new[]{EquipmentKind.Sword,EquipmentKind.Bow,EquipmentKind.Cart})Memory(EquipmentUpgrades.Key(kind),0);
        var popup=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas);popup.SetActive(true);
        var view=popup.GetComponent<BlacksmithUpgradeView>();
        var craft = new SerializedObject(popup.GetComponent<BlacksmithProductionView>());
        foreach (string name in new[]{"swordRecipe","bowRecipe"})
        {
            var recipe=craft.FindProperty(name);var input=(ResourceType)recipe.FindPropertyRelative("input").objectReferenceValue;
            var output=(ResourceType)recipe.FindPropertyRelative("output").objectReferenceValue;
            int cost=recipe.FindPropertyRelative("inputAmount").intValue, produced=recipe.FindPropertyRelative("outputAmount").intValue;
            manager.SetResourceAmount(input,100);int before=manager.GetResourceAmount(output);
            ((Button)recipe.FindPropertyRelative("produceButton").objectReferenceValue).onClick.Invoke();
            Check(manager.GetResourceAmount(input)==100-cost&&manager.GetResourceAmount(output)==before+produced,"Original crafting button binding must remain functional");
        }
        var cartView=popup.GetComponent<WarehouseCartPurchaseView>();var cartFields=new SerializedObject(cartView);
        var controls=new GameObject("Equipment QA base controls");controls.SetActive(false);
        var controller=controls.AddComponent<BaseUIController>();
        var controllerFields=new SerializedObject(controller);
        controllerFields.FindProperty("buyCartButton").objectReferenceValue=cartFields.FindProperty("buyButton").objectReferenceValue;
        controllerFields.FindProperty("cartWorkshop").objectReferenceValue=cartView;
        controllerFields.ApplyModifiedPropertiesWithoutUndo();controls.SetActive(true);
        var cartWood=(ResourceType)cartFields.FindProperty("wood").objectReferenceValue;var cartResource=(ResourceType)cartFields.FindProperty("cart").objectReferenceValue;
        manager.SetResourceAmount(cartWood,100);int cartsBefore=manager.GetResourceAmount(cartResource);
        ((Button)cartFields.FindProperty("buyButton").objectReferenceValue).onClick.Invoke();
        Check(manager.GetResourceAmount(cartWood)==100-cartView.WoodCost&&manager.GetResourceAmount(cartResource)==cartsBefore+1,"Original cart creation binding must remain functional");
        foreach(var row in view.rows)
        {
            string unit=row.equipment==EquipmentKind.Sword?"Barbarian":row.equipment==EquipmentKind.Bow?"RedArcher":"Porter";
            var combat=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/"+unit+".prefab").GetComponent<Combatant>();
            float initial=row.equipment==EquipmentKind.Cart?combat.SpeedBeforeLevel:combat.DamageBeforeLevel;
            int max=EquipmentUpgrades.Maximum(row.equipment);
            manager.SetResourceAmount(row.resource,0); view.Refresh();
            Check(!row.button.interactable&&!EquipmentUpgrades.TryBuy(row.equipment,row.resource),"Unaffordable upgrade must reject purchase");
            manager.SetResourceAmount(row.resource,10000);
            for(int level=0;level<max;level++)
            {
                int expected=row.equipment==EquipmentKind.Sword?5+3*level:row.equipment==EquipmentKind.Bow?10+5*level:10+10*level;
                Check(EquipmentUpgrades.Cost(row.equipment)==expected,"Wrong equipment price");
                int before=manager.GetResourceAmount(row.resource);
                // Batch holds all persistence until writes are suspended again; only cached values change.
                using(SaveSlotPrefs.Batch())
                {
                    Saves.GetField("WritesSuspended",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,false);
                    try{row.button.onClick.Invoke();}
                    finally{Saves.GetField("WritesSuspended",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,true);}
                }
                Check(EquipmentUpgrades.Level(row.equipment)==level+1&&manager.GetResourceAmount(row.resource)==before-expected,"Upgrade click must spend exact price and increase saved level");
                float actual=row.equipment==EquipmentKind.Cart?combat.SpeedBeforeLevel:combat.DamageBeforeLevel;
                Check(Mathf.Abs(actual-initial*(1+.1f*(level+1)))<.001f,"Equipment modifier is not applied to real unit stats");
            }
            int stock=manager.GetResourceAmount(row.resource);
            Check(!row.button.interactable&&!EquipmentUpgrades.TryBuy(row.equipment,row.resource)&&manager.GetResourceAmount(row.resource)==stock,"Maximum level must reject purchase without spending");
            Memory(EquipmentUpgrades.Key(row.equipment),0);
        }
        view.Refresh();
        foreach(var code in new[]{"ru","en"})
        {
            typeof(LocalizationService).GetField("<Language>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(language,code);
            language.SetTable(language.Table);view.Refresh();
            Canvas.ForceUpdateCanvases();
            foreach(var row in view.rows)
            {
                Check(row.label.text==(code=="ru"?"Улучшить":"Upgrade"),"Missing equipment localization");
                Check(row.label.preferredWidth<=row.label.rectTransform.rect.width,"Upgrade label does not fit");
                Check(row.icon.rectTransform.sizeDelta==row.icon.sprite.rect.size*2,"Resource icon must use native 2x size");
            }
            Capture(canvas.GetComponent<Canvas>(),"blacksmith-"+code);
        }
        popup.SetActive(false);Object.Destroy(popup);
        typeof(LocalizationService).GetField("<Language>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(language,"ru");
        var lab=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Laboratory Popup.prefab"),canvas);lab.SetActive(true);
        var list=lab.GetComponent<LaboratoryUpgradeList>();
        var stats=ScriptableObject.CreateInstance<GlobalStats>();var table=ScriptableObject.CreateInstance<ScientificUpgradeTable>();
        table.entries.Add(new ScientificUpgradeTable.Entry{id="qa.fixed.first",groupId="qa.fixed",level=1,requiredPurchases=1,title="QA"});
        table.entries.Add(new ScientificUpgradeTable.Entry{id="qa.fixed.second",groupId="qa.fixed",level=2,requiredPurchases=5,title="QA"});
        table.entries.Add(new ScientificUpgradeTable.Entry{id="qa.later.first",groupId="qa.later",level=1,requiredPurchases=2,title="Later"});
        typeof(GlobalStats).GetField("scientificUpgradeTable",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(stats,table);list.stats=stats;
        Memory("qa.fixed.first_Purchased",0);Memory("qa.fixed.second_Purchased",0);Memory("qa.later.first_Purchased",0);
        list.Refresh();var rows=list.content.GetComponentsInChildren<LaboratoryUpgradeRow>(true);var fixedRow=rows.Single(r=>r.groupId=="qa.fixed");
        int beforeIndex=fixedRow.transform.GetSiblingIndex();Memory("qa.fixed.first_Purchased",1);list.Refresh();
        Check(fixedRow.transform.GetSiblingIndex()==beforeIndex,"Laboratory row must stay in first-level requirement order after purchase");
        var squares=fixedRow.levelPips.GetComponentsInChildren<Image>();
        Check(squares.Length==2&&squares[0].color.r>.8f&&squares[1].color.r<.1f,"Laboratory pips must show purchased and total levels");
        list.stats=AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");list.Refresh();
        Capture(canvas.GetComponent<Canvas>(),"laboratory");lab.SetActive(false);Object.Destroy(lab);
        var variant=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Variants/Base Location Variant.prefab");
        var variantRow=System.Linq.Enumerable.First(variant.GetComponentsInChildren<LaboratoryUpgradeRow>(true),r=>r.groupId==ScientificUpgrades.QuietScouting);
        var duplicate=Object.Instantiate(variantRow.gameObject,canvas).GetComponent<LaboratoryUpgradeRow>();duplicate.gameObject.SetActive(true);
        var entry=list.stats.UpgradeTable.entries.First(e=>e.GroupId==ScientificUpgrades.QuietScouting);
        Memory(entry.id+"_Purchased",1);duplicate.Bind(list);duplicate.Refresh(entry,1,1,list.stats);
        var activePips=duplicate.GetComponentsInChildren<UpgradeLevelPips>();
        Check(activePips.Length==1&&activePips[0].GetComponentsInChildren<Image>().All(i=>i.color.r>.8f),"Studied upgrade in Base Location Variant must show a single yellow indicator without dark overlay");
        duplicate.gameObject.SetActive(false);Object.Destroy(duplicate.gameObject);
        Debug.Log("Equipment QA PASS: all 25 purchases, prices, caps, damage/speed modifiers, RU/EN, icons, laboratory fixed order and level squares.");
        SessionState.SetString("EquipmentQAResult","PASS: 25 purchases, prices, caps, real unit modifiers, RU/EN, icons, fixed laboratory order and pips");
    }
    static void Capture(Canvas canvas,string name)
    {
        var camera=new GameObject("Equipment QA camera").AddComponent<Camera>();camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.03f,.06f);
        var rt=new RenderTexture(1920,1080,24);var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        try{camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;foreach(var graphic in canvas.GetComponentsInChildren<Graphic>(true))graphic.SetAllDirty();Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();System.IO.Directory.CreateDirectory("Temp/EquipmentQA");System.IO.File.WriteAllBytes("Temp/EquipmentQA/"+name+".png",image.EncodeToPNG());}
        finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;RenderTexture.active=previous;camera.targetTexture=null;rt.Release();Object.Destroy(rt);Object.Destroy(image);Object.Destroy(camera.gameObject);}
    }
}

