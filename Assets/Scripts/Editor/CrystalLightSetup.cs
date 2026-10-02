#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using GameFoundation.MetaProgression;
using GameFoundation.Localization;

public static class CrystalLightSetup
{
 static string HudPath="Assets/Prefabs/UI/Screens/World Screen HUD.prefab";
 static string PanelPath="Assets/Prefabs/UI/HUD/Crystal Charge Panel.prefab";
 static string SpritePath="Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Popups/Sprites/Shared/";
 static void Ref(Object target,string key,Object value){var s=new SerializedObject(target);s.FindProperty(key).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position,Vector2 anchor,Vector2 pivot){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=(RectTransform)g.transform;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.sizeDelta=size;r.anchoredPosition=position;return r;}
 static Image Image(RectTransform r,Sprite sprite,Color color){var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.type=sprite!=null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;im.color=color;im.raycastTarget=false;return im;}
 static TextMeshProUGUI Text(RectTransform r,string text,float size){var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Pixellari Cyrillic/Pixellari Cyrillic UI.asset");t.text=text;t.fontSize=size;t.color=new Color32(242,233,209,255);t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
 static void Key(TMP_Text t,string key){var l=t.gameObject.AddComponent<LocalizedText>();var s=new SerializedObject(l);s.FindProperty("key").stringValue=key;s.ApplyModifiedPropertiesWithoutUndo();}
 static void Translation(LocalizationTable table,string key,string ru,string en){var e=table.entries.Find(x=>x.key==key);if(e==null){e=new LocalizationTable.Entry{key=key};table.entries.Add(e);}e.values=new(){ru,en};}
 public static void Run(){
 if(EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode");
 ResourceType ore=null;
 if(ore==null)ore=Resources.FindObjectsOfTypeAll<ResourceType>().FirstOrDefault(x=>x.name=="MagicOre");
 if(ore==null){foreach(var id in AssetDatabase.FindAssets("t:ResourceType")){var x=AssetDatabase.LoadAssetAtPath<ResourceType>(AssetDatabase.GUIDToAssetPath(id));if(x.name=="MagicOre")ore=x;}}
 if(ore==null)throw new System.Exception("Magic ore missing");
 const string flash="Assets/Prefabs/Flashlight.prefab";var f=PrefabUtility.LoadPrefabContents(flash);
 var beam=f.GetComponent<CrystalLightBeam>()??f.AddComponent<CrystalLightBeam>();var flag=f.GetComponentInChildren<LogisticFlag>(true);Ref(beam,"flag",flag);Ref(beam,"controller",f.GetComponentInChildren<FlashlightController>(true));Ref(beam,"groundLight",flag.GetComponentInChildren<UnityEngine.Rendering.Universal.Light2D>(true));
 var button=f.GetComponentInChildren<FlashlightActivationButton>(true);if(button)Object.DestroyImmediate(button.gameObject);
 PrefabUtility.SaveAsPrefabAsset(f,flash);PrefabUtility.UnloadPrefabContents(f);
 var source=Object.FindFirstObjectByType<WorldFlashlightAvailability>();Ref(source,"rechargeResource",ore);PrefabUtility.RecordPrefabInstancePropertyModifications(source);
 var panel=new GameObject("Crystal Charge Panel",typeof(RectTransform),typeof(CrystalChargeHud));var root=(RectTransform)panel.transform;root.sizeDelta=new Vector2(510,76);var view=panel.GetComponent<CrystalChargeHud>();
 var area=Rect("Cells",root,new(282,76),Vector2.zero,new(0,.5f),new(0,.5f));Ref(view,"cellsRect",area);
 var title=Text(Rect("Title",area,new(282,24),new(0,-12),new(.5f,1),new(.5f,.5f)),"Кристалл",18);Key(title,"world.crystal.title");
 var row=Rect("Cell Row",area,new(0,44),new(0,0),new(.5f,0),new(.5f,0));row.anchorMin=new(0,0);row.anchorMax=new(1,0);var layout=row.gameObject.AddComponent<HorizontalLayoutGroup>();layout.spacing=6;layout.childAlignment=TextAnchor.MiddleCenter;layout.childControlWidth=layout.childControlHeight=false;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
 var border=AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath+"PopupButton9Slice.png");var cellView=row.gameObject.AddComponent<CrystalCellBar>();Ref(view,"cellBar",cellView);
 var sv=new SerializedObject(cellView);var cellBindings=sv.FindProperty("cells");cellBindings.arraySize=6;
 for(int i=0;i<6;i++){
 var cell=Rect("Cell "+(i+1),row,new(42,44),Vector2.zero,new(.5f,.5f),new(.5f,.5f));var frame=Image(cell,border,Color.white);
 var inner=Rect("Charge Area",cell,new(-12,-12),Vector2.zero,new(.5f,.5f),new(.5f,.5f));inner.anchorMin=Vector2.zero;inner.anchorMax=Vector2.one;
 var fill=Rect("Charge",inner,Vector2.zero,Vector2.zero,Vector2.zero,Vector2.zero);fill.anchorMax=Vector2.one;var im=Image(fill,null,new(.45f,.82f,1));
 var c=cellBindings.GetArrayElementAtIndex(i);c.FindPropertyRelative("root").objectReferenceValue=cell.gameObject;c.FindPropertyRelative("frame").objectReferenceValue=frame;c.FindPropertyRelative("fill").objectReferenceValue=im;
 }sv.ApplyModifiedPropertiesWithoutUndo();
 var btn=Rect("Recharge",root,new(238,56),new(300,0),new(0,0),new(0,0));var bg=Image(btn,border,Color.white);bg.raycastTarget=true;var b=btn.gameObject.AddComponent<Button>();b.targetGraphic=bg;btn.gameObject.AddComponent<GameFoundation.UI.UnifiedButtonFeedback>();Ref(view,"rechargeButton",b);
 var txt=Text(Rect("Label",btn,new(124,46),new(12,0),new(0,.5f),new(0,.5f)),"Зарядить",20);Key(txt,"world.crystal.recharge");
 var icon=Image(Rect("Magic Ore",btn,ore.resourceIcon.rect.size*2,new(168,0),new(0,.5f),new(.5f,.5f)),ore.resourceIcon,Color.white);icon.type=UnityEngine.UI.Image.Type.Simple;icon.preserveAspect=true;Ref(view,"resourceIcon",icon);
 Ref(view,"price",Text(Rect("Cost",btn,new(28,32),new(218,0),new(0,.5f),new(.5f,.5f)),"1",20));
 var asset=PrefabUtility.SaveAsPrefabAsset(panel,PanelPath);Object.DestroyImmediate(panel);CrystalRecallSetup.InstallHud();
 var hud=PrefabUtility.LoadPrefabContents(HudPath);var escape=hud.GetComponentsInChildren<WorldEscapeController>(true).Single();
 var block=Rect("Crystal and Escape",escape.transform,new(824,76),Vector2.zero,new(.5f,0),new(.5f,0));var h=block.gameObject.AddComponent<HorizontalLayoutGroup>();h.spacing=24;h.childAlignment=TextAnchor.LowerCenter;h.childControlWidth=h.childControlHeight=false;h.childForceExpandWidth=h.childForceExpandHeight=false;block.gameObject.AddComponent<ContentSizeFitter>().horizontalFit=ContentSizeFitter.FitMode.PreferredSize;
 var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,block);instance.transform.localScale=Vector3.one;
 var escapeButton=escape.transform.Find("EscapeButton");escapeButton.SetParent(block,false);((RectTransform)escapeButton).sizeDelta=new(290,56);
 PrefabUtility.SaveAsPrefabAsset(hud,HudPath);PrefabUtility.UnloadPrefabContents(hud);
 var liveView=Object.FindFirstObjectByType<CrystalChargeHud>(FindObjectsInactive.Include);if(liveView){Ref(liveView,"crystal",source);PrefabUtility.RecordPrefabInstancePropertyModifications(liveView);}
 var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");Translation(table,"world.crystal.title","Кристалл","Crystal");Translation(table,"world.crystal.recharge","Зарядить","Recharge");
 var upgrades=Resources.Load<ScientificUpgradeTable>("ScientificUpgradeTable");for(int i=0;i<ScientificUpgrades.Flashlights.Length;i++){var id=ScientificUpgrades.Flashlights[i];var e=upgrades.Find(id);e.title="Ячейка кристалла "+(i+2);e.description="Добавляет ячейку энергии и ещё один луч света. Всего ячеек: "+(i+2)+".";Translation(table,"skill."+id+".title",e.title,"Crystal cell "+(i+2));Translation(table,"skill."+id+".description",e.description,"Adds an energy cell and one more light beam. Total cells: "+(i+2)+".");}EditorUtility.SetDirty(upgrades);EditorUtility.SetDirty(table);
 AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(source.gameObject.scene);EditorSceneManager.SaveOpenScenes();Debug.Log("Crystal light system and editable HUD saved.");
 }
}
#endif



