using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.MetaProgression;
using GameFoundation.Localization;
public static class WorldCasualtyPopupSetup
{
 public static string Apply()
 {
  const string path="Assets/Prefabs/UI/Screens/World Screen HUD.prefab";var root=PrefabUtility.LoadPrefabContents(path);
  try
  {
   var escape=root.GetComponentInChildren<WorldEscapeController>(true);var prior=root.transform.Find("World Casualties");if(prior!=null)UnityEngine.Object.DestroyImmediate(prior.gameObject);
   var popup=new GameObject("World Casualties",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster),typeof(Image),typeof(WorldCasualtyPopup));popup.transform.SetParent(root.transform,false);
   var rect=(RectTransform)popup.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
   var canvas=popup.GetComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=32760;
   var common=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Game Menu.prefab").transform.Find("Window");var view=popup.GetComponent<WorldCasualtyPopup>();view.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");view.panelSprite=common.GetComponent<Image>().sprite;view.buttonSprite=common.Find("Settings").GetComponent<Button>().image.sprite;
   var so=new SerializedObject(escape);so.FindProperty("casualtyPopup").objectReferenceValue=view;so.ApplyModifiedPropertiesWithoutUndo();popup.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,path);
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
  void Add(string key,string[] values){var e=table.entries.Find(x=>x.key==key);if(e==null){e=new LocalizationTable.Entry{key=key};table.entries.Add(e);}e.values=values.ToList();}
  Add("world.losses.dead",new[]{"Погибли в бою","Killed in battle","Morts au combat","战斗中阵亡","Caduti in battaglia","Im Kampf gefallen","Caídos en combate","Mortos em combate"});
  Add("world.losses.missing",new[]{"Не вернулись в портал","Did not return to the portal","Non revenus au portail","未能返回传送门","Non tornati al portale","Nicht zum Portal zurückgekehrt","No regresaron al portal","Não retornaram ao portal"});
  Add("world.losses.ok",new[]{"OK","OK","OK","确定","OK","OK","Aceptar","OK"});EditorUtility.SetDirty(table);AssetDatabase.SaveAssetIfDirty(table);return "World losses popup installed and localized in eight languages";
 }
}
