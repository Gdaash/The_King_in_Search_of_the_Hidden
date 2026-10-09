using System.Linq;
using GameFoundation.Base;
using GameFoundation.Localization;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BlacksmithVisualSetup
{
    const string PopupPath = "Assets/Prefabs/Base/Blacksmith Popup.prefab";
    static readonly Color Muted = new(.65f,.60f,.71f,1);
    static readonly Color Gold = new(.88f,.77f,.48f,1);
    static Transform Find(Transform root,string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
    static T Ref<T>(Object target,string field) where T:Object => new SerializedObject(target).FindProperty(field).objectReferenceValue as T;
    static void Set(Object target,string field,Object value) => ShelterInteractionSetup.Set(target,field,value);
    static void Pose(Transform transform,Vector2 size,Vector2 pos)
    {
        var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
        rect.sizeDelta=size;rect.anchoredPosition=pos;rect.localScale=Vector3.one;
    }
    static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 pos)
    {
        var rect=Find(parent,name) as RectTransform;
        if(!rect){rect=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;rect.SetParent(parent,false);}
        Pose(rect,size,pos);return rect;
    }
    static Text Label(Transform parent,string name,Font font,string content,Vector2 size,Vector2 pos,int fontSize,Color color,TextAnchor alignment=TextAnchor.MiddleCenter)
    {
        var rect=Rect(parent,name,size,pos);var text=rect.GetComponent<Text>()??rect.gameObject.AddComponent<Text>();
        text.font=font;text.text=content;text.fontSize=fontSize;text.color=color;text.alignment=alignment;text.raycastTarget=false;
        text.resizeTextForBestFit=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;return text;
    }
    static void Localize(Text label,string key)
    {var l=label.GetComponent<LocalizedText>()??label.gameObject.AddComponent<LocalizedText>();var s=new SerializedObject(l);s.FindProperty("key").stringValue=key;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Plate(RectTransform rect,Color color)
    {var im=rect.GetComponent<Image>()??rect.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;}
    static void Row(Transform row,Text title,Image input,Text cost,Image output,Text stock,Button button,Font font,string name,float y,Color color)
    {
        Pose(row,new Vector2(872,120),new Vector2(0,y));Plate((RectTransform)row,color);
        if(title.transform.parent!=row)title.transform.SetParent(row,false);Pose(title.transform,new Vector2(194,48),new Vector2(-312,0));
        title.alignment=TextAnchor.MiddleLeft;title.fontSize=24;title.color=new Color(.94f,.91f,.82f);title.text=name;
        foreach(var item in new[]{input.transform,cost.transform,output.transform,stock.transform,button.transform})if(item.parent!=row)item.SetParent(row,false);
        ResourceIconSizing.Apply(input,input.sprite);ResourceIconSizing.Apply(output,output.sprite);
        Pose(input.transform,input.sprite.rect.size*2,new Vector2(-128,0));Pose(cost.transform,new Vector2(64,48),new Vector2(-64,0));
        Pose(output.transform,output.sprite.rect.size*2,new Vector2(18,0));Pose(stock.transform,new Vector2(72,48),new Vector2(82,0));
        cost.fontSize=24;stock.fontSize=24;cost.alignment=stock.alignment=TextAnchor.MiddleLeft;
        Pose(button.transform,new Vector2(280,76),new Vector2(282,0));
        var text=button.GetComponentInChildren<Text>(true);Pose(text.transform,new Vector2(224,44),Vector2.zero);text.fontSize=22;text.alignment=TextAnchor.MiddleCenter;
        var accent=Rect(row,"Accent",new Vector2(2,68),new Vector2(-434,0));Plate(accent,new Color(.46f,.39f,.32f,.8f));
    }
    public static void ConfigurePopup(GameObject root)
    {
        var view=root.GetComponent<BlacksmithProductionView>();var cart=root.GetComponent<WarehouseCartPurchaseView>();
        if(!view||!cart)return;
        var title=Find(root.transform,"Title").GetComponent<Text>();var font=title.font;
        var frame=Find(root.transform,"Artwork Frame");Pose(frame,new Vector2(960,576),Vector2.zero);
        Pose(title.transform,new Vector2(800,52),new Vector2(0,236));title.color=Gold;title.fontSize=34;
        Pose(Find(root.transform,"Close"),new Vector2(34,34),new Vector2(441,250));
        var divider=Rect(root.transform,"Craft Header Divider",new Vector2(872,2),new Vector2(0,202));Plate(divider,new Color(.44f,.39f,.33f));
        var captions=new[]{("Product Column","base.blacksmith.column.product","Изделие",-312f,194f),
            ("Price Column","base.blacksmith.column.price","Цена",-102f,136f),
            ("Stock Column","base.blacksmith.column.stock","В запасе",54f,136f)};
        foreach(var caption in captions)
        {
            var label=Label(root.transform,caption.Item1,font,caption.Item3,new Vector2(caption.Item5,32),new Vector2(caption.Item4,170),20,Muted,
                caption.Item1=="Product Column"?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter);Localize(label,caption.Item2);
        }
        var so=new SerializedObject(view);int index=0;
        foreach(var field in new[]{"swordRecipe","bowRecipe"})
        {
            var recipe=so.FindProperty(field);
            T R<T>(string f)where T:Object=>recipe.FindPropertyRelative(f).objectReferenceValue as T;
            var row=Find(root.transform,index==0?"Sword Recipe":"Bow Recipe");
            Row(row,Find(row,"Title").GetComponent<Text>(),R<Image>("inputIcon"),R<Text>("inputAmountText"),R<Image>("outputIcon"),R<Text>("outputAmountText"),R<Button>("produceButton"),font,index==0?"Мечи":"Луки",95-index*140,new Color(.15f,.12f,.18f,1));
            var arrow=Find(row,"Arrow");if(arrow)Object.DestroyImmediate(arrow.gameObject);
            index++;
        }
        var cartRow=Rect(root.transform,"Cart Recipe",new Vector2(832,120),new Vector2(0,-185));
        var cartTitle=Label(cartRow,"Cart Title",font,"Телеги",new Vector2(194,48),new Vector2(-292,0),24,Color.white);Localize(cartTitle,"base.blacksmith.cart_title");
        var cartButton=Ref<Button>(cart,"buyButton");
        Row(cartRow,cartTitle,Ref<Image>(cart,"woodIcon"),Ref<Text>(cart,"woodCostText"),Ref<Image>(cart,"cartIcon"),Ref<Text>(cart,"cartAmountText"),cartButton,font,"Телеги",-185,new Color(.15f,.12f,.18f,1));
        var emptyPrice=Find(cartButton.transform,"Price");if(emptyPrice)Object.DestroyImmediate(emptyPrice.gameObject);
        var emptyStock=Find(root.transform,"Cart Stock");if(emptyStock)Object.DestroyImmediate(emptyStock.gameObject);
        // Match the existing recipe button's feedback and disabled state without changing its click binding.
        var template=Ref<Button>(view,"swordRecipe.produceButton");
        cartButton.GetComponent<Image>().sprite=template.GetComponent<Image>().sprite;cartButton.GetComponent<Image>().type=Image.Type.Sliced;
        var feedback=cartButton.GetComponent<UnifiedButtonFeedback>();EditorUtility.CopySerialized(template.GetComponent<UnifiedButtonFeedback>(),feedback);
        var cartLabel=Ref<Text>(cart,"buyLabelText");cartLabel.text="Сделать телегу";
        Set(cartButton.GetComponent<ActionButtonLabelColor>(),"label",cartLabel);
        var palette=new SerializedObject(cart);var weaponPalette=new SerializedObject(view);
        palette.FindProperty("enoughWoodColor").colorValue=weaponPalette.FindProperty("enoughColor").colorValue;
        palette.FindProperty("notEnoughWoodColor").colorValue=weaponPalette.FindProperty("notEnoughColor").colorValue;
        palette.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Apply()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        void Entry(string key,string ru,string en)
        {var e=table.entries.Find(e=>e.key==key);if(e==null){e=new LocalizationTable.Entry{key=key};table.entries.Add(e);}e.values=new(){ru,en};}
        Entry("base.blacksmith.column.product","Изделие","Item");Entry("base.blacksmith.column.price","Цена","Cost");Entry("base.blacksmith.column.stock","В запасе","In stock");Entry("base.blacksmith.cart_title","Телеги","Carts");
        EditorUtility.SetDirty(table);
        var root=PrefabUtility.LoadPrefabContents(PopupPath);
        try{ConfigurePopup(root);PrefabUtility.SaveAsPrefabAsset(root,PopupPath);}finally{PrefabUtility.UnloadPrefabContents(root);}
        const string locationPath="Assets/Prefabs/Base/Variants/Base Location Variant.prefab";
        var location=PrefabUtility.LoadPrefabContents(locationPath);
        try
        {
            foreach(var view in location.GetComponentsInChildren<BlacksmithProductionView>(true))ConfigurePopup(view.gameObject);
            PrefabUtility.SaveAsPrefabAsset(location,locationPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(location);}
        foreach(var view in Object.FindObjectsByType<BlacksmithProductionView>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(view.transform.root.name=="Base - Location Variant")
            {
                ConfigurePopup(view.gameObject);
                foreach(var c in view.GetComponentsInChildren<Component>(true))if(c&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
                EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            }
        var materialPath="Assets/Resources/UI/Availability Stars.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(Shader.Find("Game/UI/Sprite Silhouette"));AssetDatabase.CreateAsset(material,materialPath);}
        var button=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Base/Shelter Button.prefab");
        try
        {
            var stars=button.GetComponentInChildren<RisingLabelStars>(true);
            Set(stars,"starSprite",AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Experience Star.asset"));stars.material=material;
            PrefabUtility.SaveAsPrefabAsset(button,"Assets/Prefabs/Base/Shelter Button.prefab");
        }
        finally{PrefabUtility.UnloadPrefabContents(button);}
        AssetDatabase.SaveAssets();EditorSceneManager.SaveOpenScenes();
    }
}
