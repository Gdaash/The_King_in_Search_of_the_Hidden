using System.Linq;
using GameFoundation.Base;
using GameFoundation.Combat;
using GameFoundation.Localization;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class EquipmentUpgradeSetup
{
    static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
    static void Pose(Transform t, float w, float h, float x, float y)
    {
        var r = (RectTransform)t; r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.sizeDelta = new Vector2(w,h); r.anchoredPosition = new Vector2(x,y);
    }
    static Text Text(Transform parent, string name, Font font, int size, float w, float h, float x, float y)
    {
        var child = parent.Find(name);
        if (child == null) { child = new GameObject(name, typeof(RectTransform), typeof(Text)).transform; child.SetParent(parent, false); }
        Pose(child,w,h,x,y); var t = child.GetComponent<Text>(); t.font = font; t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter; t.color = new Color(.94f,.91f,.82f); t.raycastTarget = false;
        return t;
    }
    public static void Configure(GameObject root)
    {
        var craft = root.GetComponent<BlacksmithProductionView>();
        if (craft == null) return;
        var view = root.GetComponent<BlacksmithUpgradeView>() ?? root.AddComponent<BlacksmithUpgradeView>();
        var title = Find(root.transform,"Title").GetComponent<Text>(); var font = title.font;
        Pose(Find(root.transform,"Artwork Frame"),1320,616,0,0);
        Pose(title.transform,1160,52,0,256); Pose(Find(root.transform,"Close"),34,34,621,270);
        Pose(Find(root.transform,"Craft Header Divider"),1232,2,0,222);
        foreach (var column in new[]{"Product Column","Price Column","Stock Column"})
        {
            var r=(RectTransform)Find(root.transform,column); r.anchoredPosition = new Vector2(column=="Product Column"?-492:column=="Price Column"?-282:-126,190);
        }
        var wood=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Wood.asset");
        var iron=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/Iron.asset");
        // Resource asset is named IronOre in some earlier project versions.
        if(iron==null)iron=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/IronOre.asset");
        view.rows = new BlacksmithUpgradeView.Row[3];
        var so = new SerializedObject(craft);
        var cart = new SerializedObject(root.GetComponent<WarehouseCartPurchaseView>());
        for(int i=0;i<3;i++)
        {
            var row=Find(root.transform,i==0?"Sword Recipe":i==1?"Bow Recipe":"Cart Recipe");
            Pose(row,1232,120,0,95-i*140);
            Button button;
            if(i<2)
            {
                var recipe=so.FindProperty(i==0?"swordRecipe":"bowRecipe");
                button=(Button)recipe.FindPropertyRelative("produceButton").objectReferenceValue;
                foreach(var field in new[]{"inputIcon","inputAmountText","outputIcon","outputAmountText"})
                {
                    var t=((Component)recipe.FindPropertyRelative(field).objectReferenceValue).transform;
                    float x=field=="inputIcon"?-308:field=="inputAmountText"?-244:field=="outputIcon"?-162:-98;
                    ((RectTransform)t).anchoredPosition=new Vector2(x,0);
                }
            }
            else
            {
                button=(Button)cart.FindProperty("buyButton").objectReferenceValue;
                foreach(var field in new[]{"woodIcon","woodCostText","cartIcon","cartAmountText"})
                {
                    var t=((Component)cart.FindProperty(field).objectReferenceValue).transform;
                    ((RectTransform)t).anchoredPosition=new Vector2(field=="woodIcon"?-308:field=="woodCostText"?-244:field=="cartIcon"?-162:-98,0);
                }
            }
            var name=Find(row,i==2?"Cart Title":"Title"); Pose(name,194,48,-492,0);
            Pose(Find(row,"Accent"),2,68,-614,0);
            Pose(button.transform,240,76,102,0);
            var craftText=button.GetComponentInChildren<Text>(true); Pose(craftText.transform,224,60,0,0); craftText.fontSize=20;
            var upgrade=row.Find("Upgrade Equipment");
            if(upgrade==null){upgrade=Object.Instantiate(button.gameObject,row).transform;upgrade.name="Upgrade Equipment";}
            var buy=upgrade.GetComponent<Button>();
            while(buy.onClick.GetPersistentEventCount()>0)UnityEventTools.RemovePersistentListener(buy.onClick,0);
            foreach(var localized in upgrade.GetComponentsInChildren<LocalizedText>(true))Object.DestroyImmediate(localized);
            Pose(upgrade,320,96,432,0);
            var label=upgrade.GetComponentInChildren<Text>(true); Pose(label.transform,300,32,0,26); label.fontSize=22;label.text="Улучшить";
            var amount=Text(upgrade,"Upgrade Cost",font,22,65,36,28,-16);
            var level=Text(row,"Equipment Level",font,18,300,24,432,65); level.text="0/"+(i==2?5:10)+"  ·  +0%";
            var iconTransform=upgrade.Find("Upgrade Resource");
            if(iconTransform==null){iconTransform=new GameObject("Upgrade Resource",typeof(RectTransform),typeof(Image)).transform;iconTransform.SetParent(upgrade,false);}
            var icon=iconTransform.GetComponent<Image>();var resource=i==0?iron:wood;
            ResourceIconSizing.Apply(icon,resource.resourceIcon);icon.raycastTarget=false; ((RectTransform)iconTransform).anchoredPosition=new Vector2(-30,-16);
            amount.text=(i==0?5:10).ToString();
            view.rows[i]=new BlacksmithUpgradeView.Row{equipment=i==0?EquipmentKind.Sword:i==1?EquipmentKind.Bow:EquipmentKind.Cart, resource=resource,button=buy,label=label,amount=amount,level=level,icon=icon};
        }
        BlacksmithLayoutSetup.Configure(root);
    }
    public static void Pips(GameObject root)
    {
        foreach(var row in root.GetComponentsInChildren<LaboratoryUpgradeRow>(true))
        {
            var height=row.GetComponent<LayoutElement>();if(height!=null)height.preferredHeight=126;
            var rect=(RectTransform)row.transform;rect.sizeDelta=new Vector2(rect.sizeDelta.x,126);
            var target=row.purchaseButton.transform.Find("Level Squares");
            if(target==null){target=new GameObject("Level Squares",typeof(RectTransform),typeof(UpgradeLevelPips)).transform;target.SetParent(row.purchaseButton.transform,false);}
            var r=(RectTransform)target;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(20,-96);r.sizeDelta=new Vector2(200,12);
            row.levelPips=target.GetComponent<UpgradeLevelPips>();
            var stats=AssetDatabase.LoadAssetAtPath<GlobalStats>("Assets/Resources/Global/globalHexStats.asset");
            int total=stats.UpgradeTable.entries.Count(e=>e.GroupId==row.groupId);
            row.levelPips.Display(0,total>0?total:10);
        }
    }
    public static void Apply()
    {
        var loc=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        foreach(var pair in new[]{new[]{"blacksmith.upgrade","Улучшить","Upgrade"},new[]{"blacksmith.maximum","Максимум","Maximum"}})
        {var e=loc.entries.Find(e=>e.key==pair[0]);if(e==null){e=new LocalizationTable.Entry{key=pair[0]};loc.entries.Add(e);}e.values=new(){pair[1],pair[2]};}
        EditorUtility.SetDirty(loc);
        foreach(var path in new[]{"Assets/Prefabs/Base/Blacksmith Popup.prefab","Assets/Prefabs/Base/Variants/Base Location Variant.prefab","Assets/Prefabs/UI/Laboratory Upgrade Row.prefab","Assets/Prefabs/Base/Laboratory Popup.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try{foreach(var view in root.GetComponentsInChildren<BlacksmithProductionView>(true))Configure(view.gameObject);Pips(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var pair in new[]{("Barbarian",EquipmentKind.Sword),("RedArcher",EquipmentKind.Bow),("RedSniper",EquipmentKind.Bow),("Porter",EquipmentKind.Cart)})
        {
            string path="Assets/Prefabs/Units/"+pair.Item1+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try{var c=root.GetComponent<Combatant>();if(c==null)throw new System.Exception("Missing Combatant: "+path);c.equipment=pair.Item2;PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
}

