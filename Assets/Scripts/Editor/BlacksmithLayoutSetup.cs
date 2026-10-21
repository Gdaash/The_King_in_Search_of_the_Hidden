#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class BlacksmithLayoutSetup
{
 static Transform Find(Transform root,string name)=>root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
 static void Pose(Transform t,float w,float h,float x,float y){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x,y);r.localScale=Vector3.one;}
 static void LabelKey(Text text,string key){var l=text.GetComponent<LocalizedText>()??text.gameObject.AddComponent<LocalizedText>();var so=new SerializedObject(l);so.FindProperty("key").stringValue=key;so.ApplyModifiedPropertiesWithoutUndo();}
 static void Icon(Image image,float x,float y){ResourceIconSizing.Apply(image,image.sprite);image.rectTransform.anchoredPosition=new Vector2(x,y);}
 static Text Label(Transform parent,string name,Font font,string value,float w,float x,float y,int size)
 {var t=parent.Find(name);if(t==null){t=new GameObject(name,typeof(RectTransform),typeof(Text)).transform;t.SetParent(parent,false);}Pose(t,w,30,x,y);var text=t.GetComponent<Text>();text.font=font;text.fontSize=size;text.text=value;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(.68f,.63f,.73f);text.raycastTarget=false;return text;}
 public static void Configure(GameObject root)
 {
  var craft=root.GetComponent<BlacksmithProductionView>();var upgrade=root.GetComponent<BlacksmithUpgradeView>();if(craft==null||upgrade==null)return;
  var font=Find(root.transform,"Title").GetComponent<Text>().font;
  Pose(Find(root.transform,"Artwork Frame"),1220,800,0,0);Pose(Find(root.transform,"Title"),1050,52,0,350);Pose(Find(root.transform,"Close"),34,34,570,360);Pose(Find(root.transform,"Craft Header Divider"),1132,2,0,308);
  var product=Find(root.transform,"Product Column").GetComponent<Text>();Pose(product.transform,230,32,-425,270);
  var production=Find(root.transform,"Price Column").GetComponent<Text>();Pose(production.transform,310,32,-75,270);production.text="Изготовление";LabelKey(production,"blacksmith.section.production");
  var improvement=Find(root.transform,"Stock Column").GetComponent<Text>();Pose(improvement.transform,370,32,345,270);improvement.text="Улучшение";LabelKey(improvement,"blacksmith.section.upgrade");
  var cs=new SerializedObject(craft);var cart=new SerializedObject(root.GetComponent<WarehouseCartPurchaseView>());
  for(int i=0;i<3;i++)
  {
   var row=Find(root.transform,i==0?"Sword Recipe":i==1?"Bow Recipe":"Cart Recipe");Pose(row,1132,180,0,160-i*200);
   var background=row.GetComponent<Image>();if(background!=null){background.color=i%2==0?new Color(.15f,.12f,.18f):new Color(.12f,.10f,.15f);background.raycastTarget=false;}
   Pose(Find(row,"Accent"),2,140,-565,0);
   var title=Find(row,i==2?"Cart Title":"Title").GetComponent<Text>();Pose(title.transform,232,44,-425,43);title.fontSize=24;title.alignment=TextAnchor.MiddleLeft;
   Button make;Image input,output;Text price,stock;
   if(i<2){var recipe=cs.FindProperty(i==0?"swordRecipe":"bowRecipe");make=(Button)recipe.FindPropertyRelative("produceButton").objectReferenceValue;input=(Image)recipe.FindPropertyRelative("inputIcon").objectReferenceValue;output=(Image)recipe.FindPropertyRelative("outputIcon").objectReferenceValue;price=(Text)recipe.FindPropertyRelative("inputAmountText").objectReferenceValue;stock=(Text)recipe.FindPropertyRelative("outputAmountText").objectReferenceValue;}
   else{make=(Button)cart.FindProperty("buyButton").objectReferenceValue;input=(Image)cart.FindProperty("woodIcon").objectReferenceValue;output=(Image)cart.FindProperty("cartIcon").objectReferenceValue;price=(Text)cart.FindProperty("woodCostText").objectReferenceValue;stock=(Text)cart.FindProperty("cartAmountText").objectReferenceValue;}
   foreach(var t in new[]{input.transform,output.transform,price.transform,stock.transform,make.transform})t.SetParent(row,false);
   Icon(output,-500,-12);Pose(stock.transform,140,42,-394,-12);stock.fontSize=24;stock.alignment=TextAnchor.MiddleLeft;
   var stockLabel=Label(row,"Stock Caption",font,"В запасе",200,-425,-55,18);LabelKey(stockLabel,"base.blacksmith.column.stock");
   Icon(input,-110,25);Pose(price.transform,110,42,-25,25);price.alignment=TextAnchor.MiddleLeft;price.fontSize=24;
   Pose(make.transform,286,60,-75,-44);var makeLabel=make.GetComponentInChildren<Text>(true);Pose(makeLabel.transform,260,48,0,0);makeLabel.fontSize=22;
   var entry=upgrade.rows[i];entry.button.transform.SetParent(row,false);Pose(entry.button.transform,370,60,345,-44);
   Pose(entry.label.transform,196,44,-70,0);entry.label.fontSize=22;entry.label.alignment=TextAnchor.MiddleCenter;
   Icon(entry.icon,88,0);Pose(entry.amount.transform,66,44,145,0);entry.amount.fontSize=22;entry.amount.alignment=TextAnchor.MiddleLeft;
   Pose(entry.level.transform,370,30,345,50);entry.level.fontSize=20;entry.level.color=new Color(.88f,.77f,.48f);
   var pipRect=row.Find("Equipment Level Pips") as RectTransform;if(pipRect==null){pipRect=(RectTransform)new GameObject("Equipment Level Pips",typeof(RectTransform),typeof(UpgradeLevelPips)).transform;pipRect.SetParent(row,false);}
   Pose(pipRect,i==2?76:156,16,345,15);pipRect.pivot=new Vector2(0,.5f);pipRect.anchoredPosition=new Vector2(345-(i==2?38:78),15);
   entry.pips=pipRect.GetComponent<UpgradeLevelPips>();entry.pips.Display(0,i==2?5:10);
  }
  EditorUtility.SetDirty(upgrade);
 }
 public static void Apply()
 {
  var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
  foreach(var pair in new[]{("blacksmith.section.production","Изготовление","Crafting"),("blacksmith.section.upgrade","Улучшение","Upgrade"),("blacksmith.speed","Скорость","Speed"),("blacksmith.damage","Урон","Damage")})
  {var entry=table.entries.Find(e=>e.key==pair.Item1);if(entry==null){entry=new LocalizationTable.Entry{key=pair.Item1};table.entries.Add(entry);}while(entry.values.Count<table.languages.Count)entry.values.Add("");for(int i=0;i<table.languages.Count;i++)if(table.languages[i]=="ru"||table.languages[i]=="en")entry.values[i]=table.languages[i]=="ru"?pair.Item2:pair.Item3;}
  EditorUtility.SetDirty(table);
  foreach(var path in new[]{"Assets/Prefabs/Base/Blacksmith Popup.prefab","Assets/Prefabs/Base/Variants/Base Location Variant.prefab"})
  {var root=PrefabUtility.LoadPrefabContents(path);try{foreach(var view in root.GetComponentsInChildren<BlacksmithProductionView>(true))Configure(view.gameObject);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
  AssetDatabase.SaveAssets();
 }
}
#endif
