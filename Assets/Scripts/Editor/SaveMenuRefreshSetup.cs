using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameFoundation.Saves;
using GameFoundation.Settings;
using GameFoundation.Localization;
public static class SaveMenuRefreshSetup
{
 static Sprite panelSprite,buttonSprite;static TMP_FontAsset font;
 static readonly Color Ink=new Color(.94f,.91f,.82f),Gold=new Color(1,.84f,.45f);
 static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
 {var found=parent.Find(name) as RectTransform;if(found==null){found=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();found.SetParent(parent,false);}found.anchorMin=found.anchorMax=found.pivot=new Vector2(.5f,.5f);found.sizeDelta=size;found.anchoredPosition=position;found.localScale=Vector3.one;return found;}
 static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
 static Image Image(RectTransform rect,Sprite sprite,Color color){var image=rect.GetComponent<Image>()??rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;return image;}
 static TMP_Text Text(Transform parent,string name,string value,Vector2 size,Vector2 position,float fontSize=26)
 {var rect=Rect(parent,name,size,position);var text=rect.GetComponent<TMP_Text>()??rect.gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=fontSize;text.enableAutoSizing=false;text.color=Ink;text.alignment=TextAlignmentOptions.Center;text.text=value;text.raycastTarget=false;return text;}
 static Button Button(Transform parent,string name,string value,Vector2 size,Vector2 position,bool negative=false)
 {var rect=Rect(parent,name,size,position);var image=Image(rect,buttonSprite,Color.white);var button=rect.GetComponent<Button>()??rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.transition=Selectable.Transition.ColorTint;button.colors=new ColorBlock{normalColor=Color.white,highlightedColor=new Color(1,.96f,.88f),pressedColor=new Color(.7f,.64f,.76f),selectedColor=Color.white,disabledColor=new Color(.45f,.4f,.5f),colorMultiplier=1,fadeDuration=.08f};button.navigation=new Navigation{mode=Navigation.Mode.None};var label=Text(rect,"Label",value,size-new Vector2(24,8),Vector2.zero,24);label.color=negative?new Color(1,.44f,.48f):Ink;return button;}
 static RectTransform Modal(Transform parent,string name,Vector2 size,out GameObject overlay)
 {var root=Rect(parent,name,Vector2.zero,Vector2.zero);Stretch(root);Image(root,null,new Color(0,0,0,.7f));var body=Rect(root,"Window",size,Vector2.zero);Image(body,panelSprite,Color.white);overlay=root.gameObject;return body;}
 static void LanguageSettings(SettingsPopupController settings)
 {
  var so=new SerializedObject(settings);var russian=(Button)so.FindProperty("russianButton").objectReferenceValue;var english=(Button)so.FindProperty("englishButton").objectReferenceValue;if(russian==null)return;
  var list=settings.GetComponent<LanguageSelectionList>()??settings.gameObject.AddComponent<LanguageSelectionList>();var container=Rect(settings.transform,"Language Options",new Vector2(520,52),new Vector2(0,-154));container.pivot=new Vector2(.5f,1);
  list.container=container;list.template=russian;list.window=(RectTransform)settings.transform;list.firstLaunch=false;
  russian.gameObject.SetActive(false);if(english!=null)english.gameObject.SetActive(false);
 }
 public static string Apply()
 {
  var common=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Game Menu.prefab").transform.Find("Window");panelSprite=common.GetComponent<Image>().sprite;buttonSprite=common.Find("Settings").GetComponent<Button>().image.sprite;
  const string path="Assets/Prefabs/UI/Screens/Main Menu Screen.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var menu=root.GetComponentInChildren<MainMenuController>(true);var so=new SerializedObject(menu);var slots=(GameObject)so.FindProperty("slotsPanel").objectReferenceValue;var window=(RectTransform)slots.transform.Find("Window");var heading=window.Find("Title").GetComponent<TMP_Text>();font=heading.font;
   Image(window,panelSprite,Color.white);window.sizeDelta=new Vector2(900,548);var oldTitle=heading.rectTransform;oldTitle.anchorMin=oldTitle.anchorMax=new Vector2(.5f,1);oldTitle.pivot=new Vector2(.5f,1);oldTitle.sizeDelta=new Vector2(744,44);oldTitle.anchoredPosition=new Vector2(0,-24);heading.fontSize=32;heading.color=Gold;heading.raycastTarget=false;
   var view=slots.GetComponent<SaveSlotsPopupView>()??slots.AddComponent<SaveSlotsPopupView>();view.window=window;view.heading=heading;view.closeButton=(Button)so.FindProperty("slotsCloseButton").objectReferenceValue;view.slots=new Button[3];view.deleteButtons=new Button[3];view.summaries=new SaveSlotSummaryView[3];
   var close=view.closeButton;var cr=(RectTransform)close.transform;cr.anchorMin=cr.anchorMax=cr.pivot=Vector2.one;cr.anchoredPosition=new Vector2(-12,-12);cr.sizeDelta=new Vector2(40,40);close.interactable=true;close.image.enabled=true;close.image.raycastTarget=true;close.transform.SetAsLastSibling();close.onClick=new Button.ButtonClickedEvent();
   for(int i=0;i<3;i++)
   {
    var button=(Button)so.FindProperty("slotButtons").GetArrayElementAtIndex(i).objectReferenceValue;var br=(RectTransform)button.transform;br.sizeDelta=new Vector2(800,132);Image(br,buttonSprite,Color.white);view.slots[i]=button;view.summaries[i]=(SaveSlotSummaryView)so.FindProperty("slotSummaries").GetArrayElementAtIndex(i).objectReferenceValue;
    var label=(TMP_Text)so.FindProperty("slotLabels").GetArrayElementAtIndex(i).objectReferenceValue;var lr=label.rectTransform;lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(.5f,.5f);lr.sizeDelta=new Vector2(736,44);lr.anchoredPosition=new Vector2(0,30);label.fontSize=27;label.raycastTarget=false;
    var resources=button.transform.Find("Resources") as RectTransform;resources.sizeDelta=new Vector2(520,56);resources.anchoredPosition=new Vector2(0,-32);
    for(int k=0;k<2;k++){string id=k==0?"Human":"MagicOre";var icon=resources.Find(id+" Icon").GetComponent<Image>();ResourceIconSizing.Apply(icon,AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+id+".asset").resourceIcon);icon.rectTransform.anchoredPosition=new Vector2(k==0?-192:48,0);icon.raycastTarget=false;var amount=resources.Find(id+" Amount").GetComponent<TMP_Text>();amount.rectTransform.sizeDelta=new Vector2(156,40);amount.rectTransform.anchoredPosition=new Vector2(k==0?-88:152,0);amount.fontSize=26;amount.enableAutoSizing=true;amount.fontSizeMin=16;amount.fontSizeMax=26;amount.raycastTarget=false;}
    view.deleteButtons[i]=Button(window,"Delete Slot "+(i+1),"Удалить сохранение",new Vector2(400,40),Vector2.zero,true);
   }
   var body=Modal(slots.transform,"Delete Confirmation",new Vector2(760,280),out var confirmation);view.confirmation=confirmation;view.confirmationText=Text(body,"Prompt","Вы действительно хотите удалить это сохранение?\nДанное действие необратимо.",new Vector2(660,120),new Vector2(0,42));view.confirmYes=Button(body,"Yes","Да",new Vector2(260,48),new Vector2(-156,-84),true);view.confirmNo=Button(body,"No","Нет",new Vector2(260,48),new Vector2(156,-84));confirmation.SetActive(false);
   var canvas=slots.transform.parent;var languageBody=Modal(canvas,"First Launch Language",new Vector2(640,160),out var languageRoot);Text(languageBody,"Title","Choose language",new Vector2(560,42),new Vector2(0,44),28).color=Gold;
   var list=languageRoot.GetComponent<LanguageSelectionList>()??languageRoot.AddComponent<LanguageSelectionList>();list.firstLaunch=true;list.window=languageBody;list.container=Rect(languageBody,"Languages",new Vector2(520,52),new Vector2(0,-8));list.container.pivot=new Vector2(.5f,1);list.template=Button(languageBody,"Language Template","Language",new Vector2(224,52),Vector2.zero);list.template.gameObject.SetActive(false);
   LanguageSettings((SettingsPopupController)so.FindProperty("settingsPopup").objectReferenceValue);
   foreach(var graphic in window.GetComponentsInChildren<TMP_Text>(true))graphic.raycastTarget=false;
   PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  foreach(string path2 in new[]{"Assets/Prefabs/Base/Settings Popup.prefab","Assets/Prefabs/UI/SettingsPopup.prefab"})
  {var prefab=PrefabUtility.LoadPrefabContents(path2);try{foreach(var settings in prefab.GetComponentsInChildren<SettingsPopupController>(true))LanguageSettings(settings);PrefabUtility.SaveAsPrefabAsset(prefab,path2);}finally{PrefabUtility.UnloadPrefabContents(prefab);}}
  var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
  void Add(string key,string ru,string en){var entry=table.entries.Find(e=>e.key==key);if(entry==null){entry=new LocalizationTable.Entry{key=key};table.entries.Add(entry);}while(entry.values.Count<table.languages.Count)entry.values.Add("");entry.values[table.languages.IndexOf("ru")]=ru;entry.values[table.languages.IndexOf("en")]=en;}
  Add("menu.slots.title","Выберите сохранение","Choose a save slot");Add("menu.slots.delete","Удалить сохранение","Delete save");Add("menu.slots.delete_confirm","Вы действительно хотите удалить это сохранение?\nДанное действие необратимо.","Are you sure you want to delete this save?\nThis action cannot be undone.");Add("common.yes","Да","Yes");Add("common.no","Нет","No");EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);
  return "Updated save slot popup, safe delete confirmation and shared language lists";
 }
}

