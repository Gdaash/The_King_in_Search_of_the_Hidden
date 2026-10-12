#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Settings;
using GameFoundation.Localization;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class GameMenuSetup
{
    const string MenuPath = "Assets/Prefabs/UI/Game Menu.prefab";
    const string ArtPath = "Assets/SpritesOld/Evolution adventure/Sprites/UI/Sprites/Gameplay/Sprites/NewUi/";
    static void Ref(SerializedObject so, string name, Object value) => so.FindProperty(name).objectReferenceValue = value;
    static void Place(RectTransform rect, Vector2 size, Vector2 position)
    { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f); rect.sizeDelta = size; rect.anchoredPosition = position; rect.localScale = Vector3.one; }
    static GameMenuController Build()
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Construction Confirmation.prefab"));
        root.name = "Game Menu"; root.SetActive(false);
        Object.DestroyImmediate(root.GetComponent<GameFoundation.Base.BuildingConstructionConfirmation>());
        var window = root.transform.Find("Window");
        foreach (var name in new[]{"Wood Icon","Stone Icon","Wood Cost","Stone Cost"}) Object.DestroyImmediate(window.Find(name).gameObject);
        Place((RectTransform)window,new Vector2(600,400),Vector2.zero);
        var title = window.Find("Title").GetComponent<UnityEngine.UI.Text>();
        Place(title.rectTransform,new Vector2(500,48),new Vector2(0,154));title.text="Меню";title.fontSize=28;
        var description = Object.Instantiate(title,window);description.name="Description";
        Place(description.rectTransform,new Vector2(512,112),new Vector2(0,60));description.fontSize=20;description.horizontalOverflow=HorizontalWrapMode.Wrap;description.text="Все люди, оставшиеся в портале, погибнут. Продолжить?";
        var exit = window.Find("Confirm").GetComponent<UnityEngine.UI.Button>();exit.name="Exit";
        var cancel = window.Find("Cancel").GetComponent<UnityEngine.UI.Button>();
        var settings = Object.Instantiate(cancel,window);settings.name="Settings";
        Place((RectTransform)settings.transform,new Vector2(400,48),new Vector2(0,60));
        Place((RectTransform)exit.transform,new Vector2(400,48),new Vector2(0,-40));
        Place((RectTransform)cancel.transform,new Vector2(400,48),new Vector2(0,-120));
        foreach(var b in new[]{settings,exit,cancel}){b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();b.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};}
        var canvas=root.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=32700;root.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var controller=root.AddComponent<GameMenuController>();var so=new SerializedObject(controller);
        Ref(so,"window",window.gameObject);Ref(so,"title",title);Ref(so,"description",description);
        Ref(so,"settingsButton",settings);Ref(so,"exitButton",exit);Ref(so,"cancelButton",cancel);
        Ref(so,"settingsLabel",settings.GetComponentInChildren<UnityEngine.UI.Text>(true));Ref(so,"exitLabel",exit.GetComponentInChildren<UnityEngine.UI.Text>(true));Ref(so,"cancelLabel",cancel.GetComponentInChildren<UnityEngine.UI.Text>(true));so.ApplyModifiedPropertiesWithoutUndo();
        foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true)) if(!text.GetComponent<LocalizedGameText>())text.gameObject.AddComponent<LocalizedGameText>();
        var saved=PrefabUtility.SaveAsPrefabAsset(root,MenuPath);Object.DestroyImmediate(root);return saved.GetComponent<GameMenuController>();
    }
    static int Configure(GameObject root,GameMenuController menu)
    {
        int count=0;
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true).Where(b=>b.name=="Settings"&&b.transform.parent.name=="Settings Panel"))
        {
            var image=b.GetComponent<UnityEngine.UI.Image>();b.targetGraphic=image;image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+"MenuButton.png");image.type=UnityEngine.UI.Image.Type.Simple;image.color=Color.white;
            b.onClick=new UnityEngine.UI.Button.ButtonClickedEvent();
            ((RectTransform)b.transform).sizeDelta=image.sprite.rect.size*2;
            foreach(Transform child in b.transform)child.gameObject.SetActive(false);
            var outline=b.GetComponent<UnityEngine.UI.Outline>();if(outline)Object.DestroyImmediate(outline);
            b.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;b.spriteState=new UnityEngine.UI.SpriteState{pressedSprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath+"MenuButtonClick.png")};
            b.navigation=new UnityEngine.UI.Navigation{mode=UnityEngine.UI.Navigation.Mode.None};
            var feedback=b.GetComponent<GameFoundation.UI.UnifiedButtonFeedback>();if(feedback){var f=new SerializedObject(feedback);f.FindProperty("animateGraphic").boolValue=false;f.ApplyModifiedPropertiesWithoutUndo();}
            var launcher=b.GetComponent<GameMenuLauncher>()??b.gameObject.AddComponent<GameMenuLauncher>();var so=new SerializedObject(launcher);Ref(so,"menuPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath).GetComponent<GameMenuController>());Ref(so,"settingsPrefab",AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SettingsPopup.prefab").GetComponent<SettingsPopupController>());
            foreach(var ui in root.GetComponentsInChildren<GameFoundation.Base.BaseUIController>(true)){var s=new SerializedObject(ui);if(s.FindProperty("settingsButton").objectReferenceValue!=b)continue;var popup=s.FindProperty("settings").objectReferenceValue as GameObject;if(popup)Ref(so,"settings",popup.GetComponent<SettingsPopupController>());}
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var c in b.GetComponents<Component>()){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            foreach(Transform child in b.transform){EditorUtility.SetDirty(child.gameObject);if(PrefabUtility.IsPartOfPrefabInstance(child.gameObject))PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);}
            count++;
        }
        return count;
    }
    public static void Apply()
    {
        foreach(var name in new[]{"MenuButton","MenuButtonClick"}){var importer=(TextureImporter)AssetImporter.GetAtPath(ArtPath+name+".png");importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();}
        Build(); Repair();
    }
    public static void Repair()
    {
        var menu=AssetDatabase.LoadAssetAtPath<GameObject>(MenuPath).GetComponent<GameMenuController>();
        foreach(var b in menu.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {var tint=b.GetComponent<GameFoundation.UI.ActionButtonLabelColor>();if(tint)tint.Negative=b.name=="Exit";}
        EditorUtility.SetDirty(menu.gameObject);int count=0;
        foreach(var path in new[]{"Assets/Prefabs/UI/Screens/Base Screen HUD.prefab","Assets/Prefabs/Base/Base Panel.prefab","Assets/Prefabs/Base/Variants/Base Islands Backup.prefab","Assets/Prefabs/Base/Variants/Base Location Variant.prefab"})
        {var root=PrefabUtility.LoadPrefabContents(path);try{count+=Configure(root,menu);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
        string original=EditorSceneManager.GetActiveScene().path;EditorSceneManager.SaveOpenScenes();
        foreach(var path in new[]{"Assets/Scenes/Base.unity","Assets/Scenes/World.unity"}){var scene=EditorSceneManager.OpenScene(path);foreach(var root in scene.GetRootGameObjects())count+=Configure(root,menu);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        foreach(var pair in new[]{new[]{"menu.title","Меню","Menu"},new[]{"menu.resume","Продолжить игру","Resume game"},new[]{"menu.main","В главное меню","Main menu"},new[]{"menu.exit.question","Выйти в главное меню?","Exit to main menu?"},new[]{"menu.exit.warning","Все люди, оставшиеся в портале, погибнут. Продолжить?","Everyone left in the portal will die. Continue?"},new[]{"menu.exit.confirm","Выйти","Exit"}})
            if(!table.entries.Any(e=>e.key==pair[0]))table.entries.Add(new LocalizationTable.Entry{key=pair[0],values=new System.Collections.Generic.List<string>{pair[1],pair[2]}});
        EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);Debug.Log("Game menu launchers installed: "+count);
    }
}
#endif
