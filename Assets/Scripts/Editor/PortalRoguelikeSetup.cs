#if UNITY_EDITOR
using System;
using System.Linq;
using GameFoundation.MetaProgression;
using GameFoundation.UI;
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Image=UnityEngine.UI.Image;
using Object=UnityEngine.Object;

public static class PortalRoguelikeSetup
{
    const string BeamPath="Assets/Prefabs/Effects/Portal Burning Beam.prefab";
    static Sprite Sprite(string path)=>AssetDatabase.LoadAssetAtPath<Sprite>(path);
    static Sprite Icon(string name)=>Sprite("Assets/Sprites/Ui/UnitStats/"+name+".png");
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play Mode first");
        Balance();Beam();Cards();
        var hud=PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/Screens/World Screen HUD.prefab");
        try{Wire(hud);PrefabUtility.SaveAsPrefabAsset(hud,"Assets/Prefabs/UI/Screens/World Screen HUD.prefab");}
        finally{PrefabUtility.UnloadPrefabContents(hud);}
        foreach(var p in Object.FindObjectsByType<PortalTowerProgression>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            Wire(p.gameObject);
            PrefabUtility.RecordPrefabInstancePropertyModifications(p);
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Selection.activeObject=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>(PortalTowerProgressionSetup.BalancePath);
        Debug.Log("Roguelike portal installed: four weapons, scoped upgrade pool, three cards, fifth-level weapon slot.");
    }
    static void Balance()
    {
        var b=AssetDatabase.LoadAssetAtPath<PortalTowerBalance>(PortalTowerProgressionSetup.BalancePath);
        b.upgrades.Clear();b.weapons.Clear();
        void Weapon(PortalTowerBalance.Weapon w,string ru,string en,string desc,string eng,Sprite icon)
        {
            b.weapons.Add(new(){weapon=w,title=ru,englishTitle=en,description=desc,englishDescription=eng,icon=icon,combat=AssetDatabase.LoadAssetAtPath<GameFoundation.Combat.CombatWeapon>("Assets/Resources/Combat/Weapons/Portal "+w+".asset"),duration=.16f});
            b.upgrades.Add(new(){id="weapon_"+w,title=ru,englishTitle=en,description=desc,englishDescription=eng,icon=icon,weapon=w,effect=PortalTowerBalance.Effect.UnlockWeapon,value=1,maximumRank=1});
        }
        Weapon(PortalTowerBalance.Weapon.Bolts,"Магические снаряды","Magic bolts","Башня автоматически стреляет магическими снарядами по ближайшему врагу.","Automatically fires magic bolts at the nearest enemy.",Icon("Target"));
        Weapon(PortalTowerBalance.Weapon.Beam,"Выжигающий луч","Burning beam","Автоматически направляет пятно света на ближайшего врага в радиусе. Враги в пятне получают 5 урона каждые 0,25 секунды. Дальность: 6.","Automatically tracks the nearest enemy in range. Enemies inside take 5 damage every 0.25 seconds. Range: 6.",Icon("Magic"));
        Weapon(PortalTowerBalance.Weapon.Rings,"Кольца силы","Force rings","Каждые 1,5 секунды от башни расходится кольцо. Оно наносит 30 урона каждому задетому врагу. Радиус: 6.","Every 1.5 seconds an expanding ring hits each enemy it touches for 30 damage. Range: 6.",Icon("Shield"));
        Weapon(PortalTowerBalance.Weapon.Lightning,"Случайная молния","Random lightning","Каждые 2 секунды молния без промаха бьёт случайного врага в радиусе 8 на 40 электрического урона.","Every 2 seconds lightning hits a random enemy within range 8 for 40 electric damage. It cannot miss.",Sprite("Assets/Sprites/Ui/Icons/Energy Crystal.png"));
        void Upgrade(PortalTowerBalance.WeaponDefinition w,PortalTowerBalance.Effect effect,string ru,string en,string desc,string eng,float value,int max)
        {
            b.upgrades.Add(new(){id=w.weapon+"_"+effect,weapon=w.weapon,title=ru,englishTitle=en,description=desc,englishDescription=eng,icon=w.icon,effect=effect,value=value,maximumRank=max});
        }
        foreach(var w in b.weapons)
        {
            Upgrade(w,PortalTowerBalance.Effect.Damage,"Больше урона","More damage","+25% базового урона этого оружия.","+25% of this weapon's base damage.",.25f,0);
            Upgrade(w,PortalTowerBalance.Effect.AttackSpeed,"Быстрее атаки","Faster attacks","+20% скорости атаки этого оружия.","+20% attack speed for this weapon.",.2f,0);
            Upgrade(w,PortalTowerBalance.Effect.Range,"Дальнее атаки","Longer reach","+15% дальности этого оружия.","+15% range for this weapon.",.15f,0);
            if(w.weapon!=PortalTowerBalance.Weapon.Beam)
                Upgrade(w,PortalTowerBalance.Effect.Projectiles,"Больше атак","More attacks",w.weapon==PortalTowerBalance.Weapon.Lightning?"Ещё одна молния по другому случайному врагу за атаку.":w.weapon==PortalTowerBalance.Weapon.Rings?"Ещё одно кольцо в каждой волне.":"Ещё один снаряд в каждом залпе.",w.weapon==PortalTowerBalance.Weapon.Lightning?"One extra lightning strike on a different random enemy per attack.":w.weapon==PortalTowerBalance.Weapon.Rings?"One extra ring in each wave.":"One extra projectile in each volley.",1,5);
            if(w.weapon==PortalTowerBalance.Weapon.Beam || w.weapon==PortalTowerBalance.Weapon.Rings)
                Upgrade(w,PortalTowerBalance.Effect.Area,"Шире область","Wider area",w.weapon==PortalTowerBalance.Weapon.Beam?"+20% радиуса пятна выжигающего луча.":"+20% толщины кольца силы.",w.weapon==PortalTowerBalance.Weapon.Beam?"+20% burning spot radius.":"+20% force ring thickness.",.2f,5);
            if(w.weapon==PortalTowerBalance.Weapon.Rings)
                Upgrade(w,PortalTowerBalance.Effect.ExpansionSpeed,"Быстрее кольца","Faster rings","+25% скорости расширения колец.","+25% ring expansion speed.",.25f,5);
            if(w.weapon==PortalTowerBalance.Weapon.Bolts)
                Upgrade(w,PortalTowerBalance.Effect.ExpansionSpeed,"Быстрее снаряды","Faster bolts","+25% скорости полёта снарядов.","+25% projectile flight speed.",.25f,5);
        }
        b.upgrades.Add(new(){id="lights",title="Дополнительный рабочий луч",englishTitle="Extra working beam",description="Добавляет рабочий луч для открытия гексов и добычи. Максимум — 6.",englishDescription="Adds a working light beam for opening hexes and gathering. Maximum: 6.",icon=Icon("Magic"),effect=PortalTowerBalance.Effect.ExtraLight,value=1,maximumRank=5});
        EditorUtility.SetDirty(b);
    }
    static void Beam()
    {
        var source=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Flashlight.prefab");
        var root=new GameObject("Portal Burning Beam");
        try
        {
            var original=source.GetComponentInChildren<FlashlightController>(true);
            var originalBeamLight=original.GetComponent<Light2D>();
            var g=new GameObject("Beam",typeof(Light2D));g.transform.SetParent(root.transform,false);
            var light=g.GetComponent<Light2D>();EditorUtility.CopySerialized(originalBeamLight,light);light.intensity*=1.5f;
            var controller=g.AddComponent<FlashlightController>();EditorUtility.CopySerialized(original,controller);
            var spotSource=source.GetComponentsInChildren<Light2D>(true).First(l=>l.lightCookieSprite!=null);
            var marker=new GameObject("Burning Spot",typeof(Light2D));marker.transform.SetParent(root.transform,false);
            var spot=marker.GetComponent<Light2D>();EditorUtility.CopySerialized(spotSource,spot);
            marker.transform.localScale=spotSource.transform.localScale/2.5f;
            spot.color=new Color(1,.3f,.035f);spot.intensity=2.25f;
            var so=new SerializedObject(controller);
            so.FindProperty("spotLight").objectReferenceValue=light;so.FindProperty("marker").objectReferenceValue=marker;
            so.FindProperty("targetBeamWidth").floatValue=.6f;so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,BeamPath);
        }
        finally{Object.DestroyImmediate(root);PrefabUtility.UnloadPrefabContents(source);}
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
        r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static Image Background(RectTransform rect,Sprite sprite,Color color,bool hit=false)
    {
        var i=rect.gameObject.AddComponent<Image>();i.sprite=sprite;i.type=sprite!=null?Image.Type.Sliced:Image.Type.Simple;i.color=color;i.raycastTarget=hit;return i;
    }
    static Text Label(string name,Transform parent,Vector2 size,Vector2 pos,int font,string value,Color color)
    {
        var r=Rect(name,parent,size,pos);var t=r.gameObject.AddComponent<Text>();t.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Pixellari Cyrillic/Pixellari-Cyrillic.ttf");t.fontSize=font;t.text=value;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;
    }
    static void Wire(GameObject root)
    {
        foreach(var p in root.GetComponentsInChildren<PortalTowerProgression>(true))
        {
            var view=p.GetComponentInChildren<PortalUpgradeCards>(true);if(view==null)continue;
            var so=new SerializedObject(p);so.FindProperty("upgradeWindow").objectReferenceValue=view.gameObject;so.ApplyModifiedPropertiesWithoutUndo();
            var c=p.GetComponent<PortalWeaponController>();if(c==null)c=p.gameObject.AddComponent<PortalWeaponController>();c.progression=p;c.beamPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(BeamPath);
        }
    }
    static void Cards()
    {
        var root=PrefabUtility.LoadPrefabContents(PortalTowerProgressionSetup.PrefabPath);
        try
        {
            var old=root.transform.Find("Portal Tower Upgrades");if(old!=null)Object.DestroyImmediate(old.gameObject);
            var modal=Rect("Portal Tower Upgrades",root.transform,Vector2.zero,Vector2.zero);
            modal.anchorMin=Vector2.zero;modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;
            Background(modal,null,new Color(0,0,0,.8f),true);
            var canvas=modal.gameObject.AddComponent<Canvas>();canvas.overrideSorting=true;canvas.sortingOrder=2000;
            modal.gameObject.AddComponent<GraphicRaycaster>();modal.gameObject.AddComponent<CanvasGroup>().ignoreParentGroups=true;
            var view=modal.gameObject.AddComponent<PortalUpgradeCards>();view.progression=root.GetComponent<PortalTowerProgression>();
            var panel=Sprite("Assets/Sprites/Ui/Evolution/Popups/Sprites/Shared/PopupPanel9Slice.png");var buttonSprite=Sprite("Assets/Sprites/Ui/Evolution/Popups/Sprites/Shared/PopupButton9Slice.png");
            var window=Rect("Window",modal,new Vector2(1180,650),Vector2.zero);Background(window,panel,Color.white);
            var gold=new Color(.95f,.83f,.49f);var pale=new Color(.94f,.91f,.82f);
            view.heading=Label("Title",window,new Vector2(1090,55),new Vector2(0,270),36,"БАШНЯ ПОРТАЛА",gold);
            view.hint=Label("Hint",window,new Vector2(1090,45),new Vector2(0,216),24,"Выберите оружие",pale);
            view.cards=new PortalUpgradeCards.Card[3];
            var theme=AssetDatabase.FindAssets("t:ButtonVisualTheme").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>).FirstOrDefault();
            for(int i=0;i<3;i++)
            {
                var card=Rect("Card "+(i+1),window,new Vector2(350,442),new Vector2((i-1)*374,-45));Background(card,panel,new Color(.86f,.82f,.95f));
                var category=Label("Category",card,new Vector2(310,44),new Vector2(0,181),20,"НОВОЕ ОРУЖИЕ",gold);
                var iconRect=Rect("Icon",card,new Vector2(32,32),new Vector2(0,129));var icon=Background(iconRect,null,Color.white);icon.type=Image.Type.Simple;
                var title=Label("Name",card,new Vector2(306,76),new Vector2(0,59),28,"",pale);
                var description=Label("Description",card,new Vector2(302,150),new Vector2(0,-60),22,"",pale);
                var br=Rect("Choose",card,new Vector2(280,56),new Vector2(0,-175));var graphic=Background(br,buttonSprite,Color.white,true);
                var button=br.gameObject.AddComponent<Button>();button.targetGraphic=graphic;
                var feedback=br.gameObject.AddComponent<UnifiedButtonFeedback>();var so=new SerializedObject(feedback);so.FindProperty("theme").objectReferenceValue=theme;so.ApplyModifiedPropertiesWithoutUndo();
                var actionLabel=Label("Label",br,new Vector2(248,44),Vector2.zero,26,"Выбрать",theme!=null?theme.positiveLabelColor:new Color(.59f,.8f,.5f));
                var action=br.gameObject.AddComponent<ActionButtonLabelColor>();var actionSo=new SerializedObject(action);
                actionSo.FindProperty("label").objectReferenceValue=actionLabel;actionSo.FindProperty("theme").objectReferenceValue=theme;actionSo.ApplyModifiedPropertiesWithoutUndo();
                // Persistent int callbacks keep the authored prefab fully functional in builds.
                button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddIntPersistentListener(button.onClick,view.Choose,i);
                view.cards[i]=new(){button=button,icon=icon,title=title,description=description,category=category};
            }
            modal.gameObject.SetActive(false);Wire(root);PrefabUtility.SaveAsPrefabAsset(root,PortalTowerProgressionSetup.PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
#endif
