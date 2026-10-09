using System;
using System.IO;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Explicit migration, never runs automatically on import.
public static class ShelterInteractionSetup
{
    const string BasePath = "Assets/Prefabs/Base/";
    static ButtonVisualTheme Theme => AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset");
    static T Get<T>(GameObject go) where T:Component => go.GetComponent<T>() ?? go.AddComponent<T>();
    static Transform Find(GameObject go, string name) => go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
    public static void Set(Object obj, string field, Object value) { var s = new SerializedObject(obj); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Str(Object obj,string field,string value) {var s=new SerializedObject(obj);s.FindProperty(field).stringValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Edit(string path, Action<GameObject> action)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try {action(root); PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;
        rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=position;return rt;
    }
    static void Pose(RectTransform rt,Vector2 size,Vector2 position)
    {rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=position;rt.localScale=Vector3.one;}

    public static void Buttons()
    {
        Edit(BasePath+"Shelter Button.prefab", root=>{
            var label=Find(root,"Label").GetComponent<Text>();
            Set(Get<BuildingButtonLayout>(root),"label",label);
            var highlight=Get<BuildingButtonHighlight>(root);Set(highlight,"label",label);Set(highlight,"theme",Theme);
            var starRect=Find(root,"Availability Stars") as RectTransform;
            if(starRect==null)starRect=Rect("Availability Stars",label.transform.parent,Vector2.one*100,Vector2.zero);
            var stars=Get<RisingLabelStars>(starRect.gameObject);Set(stars,"label",label.rectTransform);Set(highlight,"stars",stars);stars.raycastTarget=false;
        });
        Edit(BasePath+"Buttons/Build Button.prefab",root=>{
            var icon=Find(root,"Construction Icon").GetComponent<Image>();icon.transform.SetParent(Find(root,"Button Visual"),false);
            Set(root.GetComponent<BuildingButtonLayout>(),"constructionIcon",icon);
            Set(root.GetComponent<BuildingActionAvailabilityIndicator>(),"highlight",root.GetComponent<BuildingButtonHighlight>());
            RemoveMarker(root);root.GetComponent<BuildingButtonLayout>().Refresh();
        });
        Edit(BasePath+"Buttons/Upgrade Button.prefab",root=>{
            var arrow=Find(root,"Upgrade Arrow");if(arrow)Object.DestroyImmediate(arrow.gameObject);
            var label=Find(root,"Label").GetComponent<Text>();label.gameObject.SetActive(true);label.text="Улучшить 0/7";
            Set(root.GetComponent<BuildingUpgradeButton>(),"highlight",root.GetComponent<BuildingButtonHighlight>());
            RemoveMarker(root);Pose((RectTransform)root.transform,new Vector2(270,60),Vector2.zero);
            var feedback=new SerializedObject(root.GetComponent<UnifiedButtonFeedback>());feedback.FindProperty("useHoverGraphicAlpha").boolValue=false;feedback.ApplyModifiedPropertiesWithoutUndo();
            root.GetComponent<Button>().targetGraphic.color=Color.white;
            root.GetComponent<BuildingButtonLayout>().Refresh();
        });
        foreach(var path in Directory.GetFiles(BasePath+"Buttons/Buildings","*.prefab"))
            Edit(path,root=>{
                var world=root.GetComponent<WorldBuildingButton>();
                foreach(var old in root.GetComponentsInChildren<BuildingUpgradeButton>(true))Object.DestroyImmediate(old.gameObject);
                if(world){Set(world,"upgradeButton",null);Set(world,"actionMarker",null);}
                var indicator=root.GetComponent<BuildingActionAvailabilityIndicator>();
                if(indicator)Set(indicator,"highlight",root.GetComponent<BuildingButtonHighlight>());
                RemoveMarker(root);root.GetComponent<BuildingButtonLayout>().Refresh();
            });
        AssetDatabase.SaveAssets();
    }
    static void RemoveMarker(GameObject root)
    {
        foreach(var marker in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Action Available Indicator").ToArray())Object.DestroyImmediate(marker.gameObject);
    }

    public static void Popups()
    {
        foreach(var pair in new[]{("Housing","housing"),("Fort","fort"),("Archery Range","archery_range"),("Portal","portal")})
            Edit(BasePath+pair.Item1+" Popup.prefab",root=>{
                if(root.GetComponentInChildren<BuildingUpgradeButton>(true))return;
                var frame=Find(root,"Window") as RectTransform ?? Find(root,"Artwork Frame") as RectTransform;
                float bottom=frame.anchoredPosition.y-frame.sizeDelta.y*.5f;
                var parent=frame.name=="Window"?frame:root.transform;
                frame.sizeDelta+=new Vector2(0,104);
                var upgrade=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath+"Buttons/Upgrade Button.prefab"),parent);
                upgrade.name="Upgrade";
                Pose((RectTransform)upgrade.transform,new Vector2(270,60),new Vector2(0,bottom-12));
                Str(upgrade.GetComponent<BuildingUpgradeButton>(),"buildingId",pair.Item2);
                upgrade.GetComponent<BuildingButtonLayout>().Refresh();
            });
        // Copy the existing cart UI so icon and price references stay explicit.
        var warehouse=PrefabUtility.LoadPrefabContents(BasePath+"Warehouse Popup.prefab");
        try
        {
            Edit(BasePath+"Blacksmith Popup.prefab",root=>{
                if(root.GetComponent<WarehouseCartPurchaseView>())return;
                var source=warehouse.GetComponent<WarehouseCartPurchaseView>();
                var cartView=root.AddComponent<WarehouseCartPurchaseView>();EditorUtility.CopySerialized(source,cartView);
                var buy=Object.Instantiate(Find(warehouse,"Buy Cart").gameObject,root.transform);buy.name="Make Cart";
                var stock=Object.Instantiate(Find(warehouse,"Cart Stock").gameObject,root.transform);stock.name="Cart Stock";
                Pose((RectTransform)buy.transform,new Vector2(290,96),new Vector2(172,-260));
                Pose((RectTransform)stock.transform,new Vector2(140,48),new Vector2(-180,-260));
                Set(cartView,"buyButton",buy.GetComponent<Button>());Set(cartView,"buyLabelText",Find(buy,"Text (Legacy)").GetComponent<Text>());
                Set(cartView,"woodIcon",Find(buy,"Wood Icon").GetComponent<Image>());Set(cartView,"woodCostText",Find(buy,"Wood Cost").GetComponent<Text>());
                Set(cartView,"cartIcon",Find(stock,"Cart Icon").GetComponent<Image>());Set(cartView,"cartAmountText",Find(stock,"Cart Amount").GetComponent<Text>());
                Pose((RectTransform)Find(root,"Artwork Frame"),new Vector2(760,680),new Vector2(0,-60));
                Str(Find(buy,"Text (Legacy)").GetComponent<LocalizedText>(),"key","base.blacksmith.make_cart");
                Find(buy,"Text (Legacy)").GetComponent<Text>().text="Сделать телегу";
            });
            var old=Find(warehouse,"Buy Cart");if(old)Object.DestroyImmediate(old.gameObject);
            var view=warehouse.GetComponent<WarehouseCartPurchaseView>();Set(view,"buyButton",null);Set(view,"buyLabelText",null);Set(view,"woodIcon",null);Set(view,"woodCostText",null);
            PrefabUtility.SaveAsPrefabAsset(warehouse,BasePath+"Warehouse Popup.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(warehouse);}
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var entry=table.entries.Find(e=>e.key=="base.blacksmith.make_cart");
        if(entry==null){entry=new LocalizationTable.Entry{key="base.blacksmith.make_cart"};table.entries.Add(entry);}entry.values=new(){"Сделать телегу","Make cart"};
        var oldEntry=table.entries.Find(e=>e.key=="base.warehouse_popup.buy_cart.text_(legacy)");if(oldEntry!=null)oldEntry.values=new(){"Сделать телегу","Make cart"};
        EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
    }

    public static void ConfirmationPrefab()
    {
        var template=AssetDatabase.LoadAssetAtPath<GameObject>(BasePath+"Warehouse Popup.prefab");
        var font=Find(template,"Title").GetComponent<Text>().font;
        var frameSprite=Find(template,"Artwork Frame").GetComponent<Image>().sprite;
        var root=Rect("Construction Confirmation",null,Vector2.zero,Vector2.zero).gameObject;
        try
        {
            var rr=(RectTransform)root.transform;rr.anchorMin=Vector2.zero;rr.anchorMax=Vector2.one;
            var shade=root.AddComponent<Image>();shade.color=new Color(0,0,0,.65f);
            var window=Rect("Window",root.transform,new Vector2(760,330),Vector2.zero);window.gameObject.AddComponent<CanvasWindowFit>();
            var background=window.gameObject.AddComponent<Image>();background.sprite=frameSprite;background.type=Image.Type.Sliced;
            Text Label(string name,Vector2 size,Vector2 pos,int fontSize,string content)
            {var t=Rect(name,window,size,pos).gameObject.AddComponent<Text>();t.font=font;t.fontSize=fontSize;t.alignment=TextAnchor.MiddleCenter;t.color=Theme.neutralLabelColor;t.text=content;t.raycastTarget=false;return t;}
            var title=Label("Title",new Vector2(680,100),new Vector2(0,96),30,"Построить «Здание»?");title.color=new Color(.89f,.78f,.47f);
            var a=Rect("Wood Icon",window,new Vector2(48,48),new Vector2(-125,5)).gameObject.AddComponent<Image>();a.raycastTarget=false;
            var b=Rect("Stone Icon",window,new Vector2(48,48),new Vector2(75,5)).gameObject.AddComponent<Image>();b.raycastTarget=false;
            var at=Label("Wood Cost",new Vector2(100,50),new Vector2(-48,5),28,"0");var bt=Label("Stone Cost",new Vector2(100,50),new Vector2(152,5),28,"0");
            Button Button(string name,int x,bool negative)
            {
                var br=Rect(name,window,new Vector2(280,58),new Vector2(x,-98));
                var im=br.gameObject.AddComponent<Image>();im.sprite=Theme.textButtonSprite;im.type=Image.Type.Sliced;
                var button=br.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=im;
                var t=Label(name+" Label",new Vector2(250,44),Vector2.zero,24,negative?"Нет":"Да");t.transform.SetParent(br,false);
                var f=br.gameObject.AddComponent<UnifiedButtonFeedback>();Set(f,"theme",Theme);
                var colors=br.gameObject.AddComponent<ActionButtonLabelColor>();Set(colors,"label",t);Set(colors,"theme",Theme);
                var s=new SerializedObject(colors);s.FindProperty("negative").boolValue=negative;s.ApplyModifiedPropertiesWithoutUndo();return button;
            }
            var yes=Button("Confirm",-162,false);var no=Button("Cancel",162,true);
            var confirmation=root.AddComponent<BuildingConstructionConfirmation>();Set(confirmation,"title",title);Set(confirmation,"woodIcon",a);Set(confirmation,"stoneIcon",b);
            Set(confirmation,"woodAmount",at);Set(confirmation,"stoneAmount",bt);Set(confirmation,"confirm",yes);Set(confirmation,"cancel",no);
            Set(confirmation,"confirmLabel",yes.GetComponentInChildren<Text>());Set(confirmation,"cancelLabel",no.GetComponentInChildren<Text>());
            root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,BasePath+"Construction Confirmation.prefab");
        }
        finally{Object.DestroyImmediate(root);}
    }

    public static void PortalFragments()
    {
        const string folder="Assets/Sprites/Effects/Portal";
        Directory.CreateDirectory(folder);
        var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes("Assets/Sprites/World/Base/Layers/13_Portal.png"));
        var centers=new[]{new Vector2Int(46,39),new Vector2Int(48,43),new Vector2Int(43,37)};
        for(int i=0;i<centers.Length;i++)
        {
            var texture=new Texture2D(3,3,TextureFormat.RGBA32,false);texture.SetPixels(source.GetPixels(centers[i].x,centers[i].y,3,3));texture.Apply();
            var path=folder+"/PortalGlow"+(i+1)+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
        }
        Object.DestroyImmediate(source);
    }

    public static void Location()
    {
        Edit(BasePath+"Variants/Base Location Variant.prefab",ConfigureLocation);
        foreach(var c in Object.FindObjectsByType<BaseUIController>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(c.transform.root.name=="Base - Location Variant")ConfigureLocation(c.transform.root.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
    public static void FinishVisuals()
    {
        Edit(BasePath+"Shelter Button.prefab",root=>{
            var stars=root.GetComponentInChildren<RisingLabelStars>(true);Get<CanvasRenderer>(stars.gameObject);
        });
        foreach(var name in new[]{"Housing","Fort","Archery Range","Portal"})
            Edit(BasePath+name+" Popup.prefab",root=>{
                var frame=Find(root,"Window") as RectTransform ?? Find(root,"Artwork Frame") as RectTransform;
                // Restore the original authored height; the footer now sits outside the frame.
                frame.sizeDelta=new Vector2(frame.sizeDelta.x,name=="Housing"?380:name=="Portal"?800:832);
                var upgrade=root.GetComponentInChildren<BuildingUpgradeButton>(true);
                var placement=Get<BuildingPopupUpgradePlacement>(upgrade.gameObject);Set(placement,"frame",frame);placement.Refresh();
                var fit=Get<CanvasWindowFit>(frame.gameObject);var s=new SerializedObject(fit);s.FindProperty("footerSpace").floatValue=80;s.ApplyModifiedPropertiesWithoutUndo();
            });
        void Wire(GameObject root)
        {
            foreach(var upgrade in root.GetComponentsInChildren<BuildingUpgradeButton>(true))
            {
                var placement=upgrade.GetComponent<BuildingPopupUpgradePlacement>();if(placement)placement.Refresh();
            }
        }
        Edit(BasePath+"Variants/Base Location Variant.prefab",Wire);
        foreach(var controller in Object.FindObjectsByType<BaseUIController>(FindObjectsSortMode.None))Wire(controller.transform.root.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
    }
    static void ConfigureLocation(GameObject root)
    {
        var controller=root.GetComponentInChildren<BaseUIController>(true);
        var popups=controller.transform;
        var blacksmith=Find(root,"Blacksmith Popup").GetComponent<WarehouseCartPurchaseView>();
        Set(controller,"cartWorkshop",blacksmith);Set(controller,"buyCartButton",new SerializedObject(blacksmith).FindProperty("buyButton").objectReferenceValue);
        var confirmation=root.GetComponentInChildren<BuildingConstructionConfirmation>(true);
        if(!confirmation){var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BasePath+"Construction Confirmation.prefab"),popups);confirmation=go.GetComponent<BuildingConstructionConfirmation>();}
        foreach(var construction in root.GetComponentsInChildren<BaseBuildingConstruction>(true))Set(construction,"confirmation",confirmation);
        foreach(var owner in root.GetComponentsInChildren<WorldBuildingButton>(true))
        {
            Set(owner,"upgradeButton",null);Set(owner,"actionMarker",null);
            var construction=owner.GetComponent<BaseBuildingConstruction>();
            var build=construction?new SerializedObject(construction).FindProperty("buildButton").objectReferenceValue as Button:null;
            Set(owner,"constructionButton",build?build.transform:null);
            var indicator=owner.GetComponent<BuildingActionAvailabilityIndicator>();
            if(owner.name=="Housing" && !indicator)indicator=owner.gameObject.AddComponent<BuildingActionAvailabilityIndicator>();
            if(indicator)
            {
                Set(indicator,"highlight",owner.GetComponent<BuildingButtonHighlight>());
                if(owner.name=="Blacksmith")Set(indicator,"warehouse",blacksmith);
                if(owner.name=="Warehouse"||owner.name=="Housing")
                {var s=new SerializedObject(indicator);s.FindProperty("actionType").enumValueIndex=(int)BuildingActionAvailabilityIndicator.ActionType.None;s.ApplyModifiedPropertiesWithoutUndo();}
                string id=owner.name=="Portal"?"portal":owner.name=="Housing"?"housing":owner.name=="Fort"?"fort":owner.name=="Archery Range"?"archery_range":"";
                Str(indicator,"upgradeBuildingId",id);
            }
            owner.GetComponent<BuildingButtonLayout>()?.Refresh();
        }
        var housing=root.GetComponentsInChildren<WorldBuildingButton>(true).First(w=>w.name=="Housing");
        var art=Find(root,"Base Scene Artwork");
        var houseSprites=art.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.name.StartsWith("House ")||s.name=="Market").ToArray();
        var hs=new SerializedObject(housing);var array=hs.FindProperty("artworkSprites");array.arraySize=houseSprites.Length;for(int i=0;i<houseSprites.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=houseSprites[i];hs.ApplyModifiedPropertiesWithoutUndo();
        var magic=root.GetComponentsInChildren<BaseBuildingConstruction>(true).First(c=>c.BuildingId=="magic_library");
        var glow=art.GetComponentsInChildren<SpriteRenderer>(true).First(s=>s.name=="Magic Library Glow");
        var magicOwner=magic.GetComponent<WorldBuildingButton>();var ms=new SerializedObject(magicOwner);var ma=ms.FindProperty("artworkSprites");
        for(int i=ma.arraySize-1;i>=0;i--)if(ma.GetArrayElementAtIndex(i).objectReferenceValue==glow){ma.GetArrayElementAtIndex(i).objectReferenceValue=null;ma.DeleteArrayElementAtIndex(i);}ms.ApplyModifiedPropertiesWithoutUndo();
        var pulse=Get<BuildingGlowPulse>(glow.gameObject);Set(pulse,"construction",magic);Set(pulse,"glow",glow);glow.enabled=false;
        var light=Find(root,"Portal Light");
        if(light)
        {
            Get<Light2DFlicker>(light.gameObject);
            var ls=new SerializedObject(light.GetComponent<Light2DFlicker>());ls.FindProperty("positionJitterX").floatValue=0;ls.FindProperty("positionJitterY").floatValue=0;
            ls.FindProperty("radiusFlickerAmount").floatValue=.3f;ls.FindProperty("speed").floatValue=1.3f;ls.FindProperty("burstChance").floatValue=0;ls.ApplyModifiedPropertiesWithoutUndo();
        }
        var portal=art.GetComponentsInChildren<SpriteRenderer>(true).First(s=>s.name=="Portal");
        var embers=Get<PortalEmbers>(portal.gameObject);Set(embers,"portal",portal);
        var es=new SerializedObject(embers);var fragments=es.FindProperty("fragments");fragments.arraySize=3;
        for(int i=0;i<3;i++)fragments.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Effects/Portal/PortalGlow"+(i+1)+".png");
        es.FindProperty("portalLight").objectReferenceValue=light.GetComponent<UnityEngine.Rendering.Universal.Light2D>();
        es.FindProperty("rate").floatValue=36;es.FindProperty("lifetime").vector2Value=new Vector2(2.8f,3.6f);es.ApplyModifiedPropertiesWithoutUndo();
        // Remove only the obsolete square's UI/entity. Its artwork now belongs to housing.
        foreach(var square in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Square"||t.name=="Square Popup"||t.name=="Square Dimmer").ToArray())if(square)Object.DestroyImmediate(square.gameObject);
        foreach(var c in root.GetComponentsInChildren<Component>(true))if(c){EditorUtility.SetDirty(c);if(PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
    }
}
