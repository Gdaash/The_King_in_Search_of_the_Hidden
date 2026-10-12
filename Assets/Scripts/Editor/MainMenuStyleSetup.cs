#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Saves;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class MainMenuStyleSetup
{
    static void Place(RectTransform rect, Vector2 size, Vector2 position)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;rect.localScale=Vector3.one;}
    static void Reference(SerializedObject so,string field,Object value)=>so.FindProperty(field).objectReferenceValue=value;
    static RectTransform Child(Transform parent,string name)
    {var found=parent.Find(name) as RectTransform;if(found)return found;var t=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();t.SetParent(parent,false);return t;}
    static void Label(TMP_Text text,float size,Color color)
    {text.fontSize=size*1.3f;text.enableAutoSizing=false;text.characterSpacing=0;text.lineSpacing=0;text.alignment=TextAlignmentOptions.Center;text.color=color;text.raycastTarget=false;}
    static TMP_Text CopyLabel(TMP_Text template,Transform parent,string name,Vector2 size,Vector2 pos)
    {var t=parent.Find(name)?.GetComponent<TMP_Text>();if(!t){t=Object.Instantiate(template,parent);t.name=name;}Place(t.rectTransform,size,pos);Label(t,20,new Color(.94f,.91f,.82f));return t;}
    static void CloseButton(UnityEngine.UI.Button button)
    {
        var image=button.GetComponent<UnityEngine.UI.Image>();image.enabled=true;image.raycastTarget=true;image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Close Square Normal.png");image.type=UnityEngine.UI.Image.Type.Simple;image.color=Color.white;button.targetGraphic=image;
        foreach(Transform child in button.transform)child.gameObject.SetActive(false);
        var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one;rect.sizeDelta=new Vector2(40,40);rect.anchoredPosition=new Vector2(-12,-12);
        button.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;button.spriteState=new UnityEngine.UI.SpriteState{highlightedSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Close Square Hover.png"),pressedSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Close Square Pressed.png")};
        var feedback=button.GetComponent<UnifiedButtonFeedback>();if(feedback){var s=new SerializedObject(feedback);s.FindProperty("animateGraphic").boolValue=false;s.ApplyModifiedPropertiesWithoutUndo();}
    }
    static void Configure(GameObject root)
    {
        var menu=root.GetComponentInChildren<MainMenuController>(true);if(!menu)return;
        var config=new SerializedObject(menu);var play=(UnityEngine.UI.Button)config.FindProperty("playButton").objectReferenceValue;var settings=(UnityEngine.UI.Button)config.FindProperty("settingsButton").objectReferenceValue;var exit=(UnityEngine.UI.Button)config.FindProperty("exitButton").objectReferenceValue;
        var canvas=play.GetComponentInParent<Canvas>().transform;
        var theme=AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset");
        var common=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Game Menu.prefab").transform.Find("Window");
        var commonButton=common.Find("Settings").GetComponent<UnityEngine.UI.Button>();
        var background=canvas.Find("Background").GetComponent<UnityEngine.UI.Image>();background.color=new Color(.045f,.035f,.06f,1);
        var accent=canvas.Find("Accent");if(accent)accent.gameObject.SetActive(false);
        var panel=Child(canvas,"Main Menu Panel");var panelImage=panel.GetComponent<UnityEngine.UI.Image>()??panel.gameObject.AddComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(common.GetComponent<UnityEngine.UI.Image>(),panelImage);panelImage.raycastTarget=false;Place(panel,new Vector2(600,640),Vector2.zero);panel.SetSiblingIndex(background.transform.GetSiblingIndex()+1);
        var logo=canvas.Find("Game Logo");if(!logo)logo=panel.Find("Game Logo");logo.SetParent(panel,false);var logoImage=logo.GetComponent<UnityEngine.UI.Image>();Place((RectTransform)logo,logoImage.sprite.rect.size*2,new Vector2(0,160));
        var subtitle=canvas.Find("Subtitle");if(!subtitle)subtitle=panel.Find("Subtitle");subtitle.SetParent(panel,false);Place((RectTransform)subtitle,new Vector2(512,56),new Vector2(0,4));Label(subtitle.GetComponent<TMP_Text>(),20,theme.neutralLabelColor);
        var buttons=new[]{play,settings,exit};for(int i=0;i<buttons.Length;i++)
        {
            var b=buttons[i];b.transform.SetParent(panel,false);Place((RectTransform)b.transform,new Vector2(400,48),new Vector2(0,-76-i*72));
            var image=b.GetComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(commonButton.image,image);image.enabled=true;image.raycastTarget=true;b.targetGraphic=image;b.transition=UnityEngine.UI.Selectable.Transition.None;
            foreach(var graphic in b.GetComponentsInChildren<UnityEngine.UI.Image>(true))if(graphic!=image)graphic.gameObject.SetActive(false);
            var label=b.GetComponentInChildren<TMP_Text>(true);label.gameObject.SetActive(true);Place(label.rectTransform,new Vector2(360,40),Vector2.zero);Label(label,20,i==2?theme.negativeLabelColor:i==0?theme.positiveLabelColor:theme.neutralLabelColor);
            var feedback=b.GetComponent<UnifiedButtonFeedback>()??b.gameObject.AddComponent<UnifiedButtonFeedback>();EditorUtility.CopySerialized(commonButton.GetComponent<UnifiedButtonFeedback>(),feedback);
            var tint=b.GetComponent<ActionButtonLabelColor>()??b.gameObject.AddComponent<ActionButtonLabelColor>();var s=new SerializedObject(tint);Reference(s,"label",null);Reference(s,"tmpLabel",label);Reference(s,"theme",theme);s.FindProperty("negative").boolValue=i==2;s.FindProperty("neutral").boolValue=i==1;s.ApplyModifiedPropertiesWithoutUndo();
            var outline=b.GetComponent<UnityEngine.UI.Outline>();if(outline)Object.DestroyImmediate(outline);
        }
        var oldPanel=canvas.Find("Settings Panel");if(oldPanel)oldPanel.gameObject.SetActive(false);
        var slots=(GameObject)config.FindProperty("slotsPanel").objectReferenceValue;var window=slots.transform.Find("Window") as RectTransform;var imageWindow=window.GetComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(common.GetComponent<UnityEngine.UI.Image>(),imageWindow);Place(window,new Vector2(760,560),Vector2.zero);
        var title=window.Find("Title").GetComponent<TMP_Text>();Place(title.rectTransform,new Vector2(620,48),new Vector2(0,220));Label(title,28,new Color(1,.84f,.45f));CloseButton((UnityEngine.UI.Button)config.FindProperty("slotsCloseButton").objectReferenceValue);
        var summaries=config.FindProperty("slotSummaries");summaries.arraySize=3;
        for(int i=0;i<3;i++)
        {
            var b=(UnityEngine.UI.Button)config.FindProperty("slotButtons").GetArrayElementAtIndex(i).objectReferenceValue;Place((RectTransform)b.transform,new Vector2(640,112),new Vector2(0,126-i*140));EditorUtility.CopySerialized(commonButton.image,b.GetComponent<UnityEngine.UI.Image>());
            var text=(TMP_Text)config.FindProperty("slotLabels").GetArrayElementAtIndex(i).objectReferenceValue;Place(text.rectTransform,new Vector2(584,60),new Vector2(0,20));Label(text,20,theme.neutralLabelColor);
            var row=Child(b.transform,"Resources");Place(row,new Vector2(320,40),new Vector2(0,-30));
            var view=b.GetComponent<SaveSlotSummaryView>()??b.gameObject.AddComponent<SaveSlotSummaryView>();var s=new SerializedObject(view);Reference(s,"summary",text);Reference(s,"resources",row.gameObject);
            for(int k=0;k<2;k++)
            {
                string id=k==0?"Human":"MagicOre";var type=AssetDatabase.LoadAssetAtPath<ResourceType>("Assets/Resources/ResourceTypes/"+id+".asset");var iconRect=Child(row,id+" Icon");var icon=iconRect.GetComponent<UnityEngine.UI.Image>()??iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();icon.sprite=type.resourceIcon;icon.raycastTarget=false;Place(iconRect,icon.sprite.rect.size*2,new Vector2(k==0?-112:32,0));
                var amount=CopyLabel(text,row,id+" Amount",new Vector2(72,36),new Vector2(k==0?-48:108,0));amount.text="0";
                Reference(s,k==0?"human":"magicOre",type);Reference(s,k==0?"humanIcon":"oreIcon",icon);Reference(s,k==0?"humanAmount":"oreAmount",amount);
            }
            s.ApplyModifiedPropertiesWithoutUndo();summaries.GetArrayElementAtIndex(i).objectReferenceValue=view;
        }
        config.ApplyModifiedPropertiesWithoutUndo();
        var popup=(GameFoundation.Settings.SettingsPopupController)config.FindProperty("settingsPopup").objectReferenceValue;
        if(popup)
        {
            foreach(var text in popup.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                if(text.name.EndsWith(" Label")){var p=text.rectTransform.anchoredPosition;p.x=-166;Place(text.rectTransform,new Vector2(230,35),p);text.alignment=TextAnchor.MiddleLeft;}
            }
            foreach(var slider in popup.GetComponentsInChildren<UnityEngine.UI.Slider>(true)){var rect=(RectTransform)slider.transform;var p=rect.anchoredPosition;p.x=128;Place(rect,new Vector2(220,22),p);}
            foreach(var toggle in popup.GetComponentsInChildren<UnityEngine.UI.Toggle>(true)){var rect=(RectTransform)toggle.transform;var p=rect.anchoredPosition;p.x=toggle.name=="Fullscreen"?-166:128;Place(rect,new Vector2(190,30),p);}
            foreach(var b in popup.GetComponentsInChildren<UnityEngine.UI.Button>(true))
            {
                if(b.name=="Close"){CloseButton(b);Place((RectTransform)b.transform,new Vector2(40,40),new Vector2(298,218));}
                else if(b.name=="Russian"||b.name=="English"){var p=((RectTransform)b.transform).anchoredPosition;p.x=b.name=="Russian"?-130:130;Place((RectTransform)b.transform,new Vector2(214,52),p);}
            }
        }
        foreach(var c in root.GetComponentsInChildren<Component>(true))if(c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
        foreach(var t in root.GetComponentsInChildren<Transform>(true))if(PrefabUtility.IsPartOfPrefabInstance(t.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
    }
    public static void Apply()
    {
        var path="Assets/Prefabs/UI/Screens/Main Menu Screen.prefab";var prefab=PrefabUtility.LoadPrefabContents(path);try{Configure(prefab);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var scene=EditorSceneManager.GetActiveScene();foreach(var root in scene.GetRootGameObjects())Configure(root);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");if(!table.entries.Any(e=>e.key=="menu.slot.number"))table.entries.Add(new LocalizationTable.Entry{key="menu.slot.number",values=new System.Collections.Generic.List<string>{"Ячейка {0}","Slot {0}"}});EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
    }
}
#endif
