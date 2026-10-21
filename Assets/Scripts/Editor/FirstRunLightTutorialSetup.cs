#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;
using GameFoundation.Quests;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class FirstRunLightTutorialSetup
{
    public static void UpdateCarrierLocalization()
    {
        var table=AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var item=table.entries.FirstOrDefault(e=>e.key=="world.tutorial.carriers");
        if(item==null){item=new LocalizationTable.Entry{key="world.tutorial.carriers"};table.entries.Add(item);}
        item.values=new(){FirstRunLightTutorial.CarrierInstruction,"There's our first haul! The carriers will bring their carts and take it to the portal. They collect supplies only on lit ground – they won't find anything in the dark. Let's wait until at least two units of resources reach the portal."};
        EditorUtility.SetDirty(table);AssetDatabase.SaveAssets();
    }
    public const string Path = "Assets/Prefabs/UI/Dialogue/First Run Light Tutorial.prefab";
    static TMP_SpriteAsset MouseSpriteAsset()
    {
        const string path = "Assets/Sprites/Ui/Dialogue/Tutorial Mouse.asset";
        var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(path);
        bool create = asset == null;
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Resources/LBM.png");
        if (create) asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
        asset.name = "Tutorial Mouse"; asset.spriteSheet = sprite.texture;
        var serialized = new SerializedObject(asset); serialized.FindProperty("m_Version").stringValue = "1.1.0"; serialized.ApplyModifiedPropertiesWithoutUndo();
        var rect = sprite.rect;
        var glyph = new TMP_SpriteGlyph(0, new UnityEngine.TextCore.GlyphMetrics(rect.width,rect.height,0,rect.height,rect.width),
            new UnityEngine.TextCore.GlyphRect((int)rect.x,(int)rect.y,(int)rect.width,(int)rect.height),1,0);
        asset.spriteGlyphTable.Clear(); asset.spriteCharacterTable.Clear();
        asset.spriteGlyphTable.Add(glyph);
        asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xfffe,glyph) { name = "LMB" });
        if (asset.material == null) asset.material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Tutorial Mouse Material" };
        asset.material.mainTexture = sprite.texture;
        if (create) AssetDatabase.CreateAsset(asset,path);
        if (!AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material,asset);
        asset.UpdateLookupTables(); EditorUtility.SetDirty(asset);
        return asset;
    }
    static void Set(Object obj, string field, Object value)
    { var s = new SerializedObject(obj); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    public static void Apply()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play first");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(QuestDialogueSetup.ViewPath);
        var data = new SerializedObject(source.GetComponent<QuestDialogueView>());
        var root = new GameObject("First Run Light Tutorial", typeof(RectTransform), typeof(FirstRunLightTutorial));
        var rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        var window = Object.Instantiate((GameObject)data.FindProperty("window").objectReferenceValue, root.transform);
        window.name = "Tutorial Window";
        var wr = (RectTransform)window.transform;
        wr.anchorMin = wr.anchorMax = new Vector2(1,.5f); wr.pivot = new Vector2(.5f,.5f);
        wr.anchoredPosition = new Vector2(-350,0); wr.localScale = new Vector3(-1,1,1);
        var bubble = window.transform.Find("Speech Bubble");
        var next=bubble.Find("Next").GetComponent<Button>(); next.onClick=new Button.ButtonClickedEvent();
        next.transform.localScale=new Vector3(-1,1,1); next.gameObject.SetActive(false);
        var nextRect=(RectTransform)next.transform;
        nextRect.anchorMin=nextRect.anchorMax=new Vector2(1,0);nextRect.pivot=new Vector2(.5f,1);
        nextRect.sizeDelta=new Vector2(240,56);nextRect.anchoredPosition=new Vector2(-120,-12);
        var body = bubble.Find("Dialogue Text").GetComponent<TMP_Text>();
        body.spriteAsset = MouseSpriteAsset(); body.richText = true;
        body.text = "Переведи луч на соседний гекс. Подхвати его <size=64><sprite=0></size> и укажи, куда двигаться дальше – пора разведать ещё немного земли.";
        Set(root.GetComponent<FirstRunLightTutorial>(), "firstHexForest", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Forest 1.prefab"));
        bubble.Find("Speaker Name").GetComponent<TMP_Text>().text = "Стайк Пью";
        foreach (var text in window.GetComponentsInChildren<TMP_Text>(true)) text.transform.localScale = new Vector3(-1,1,1);
        foreach(var text in next.GetComponentsInChildren<TMP_Text>(true))text.transform.localScale=Vector3.one;
        var portrait = bubble.Find("Scribe").GetComponent<Image>();
        portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Dialogue/Old Pew.png");
        portrait.rectTransform.sizeDelta = portrait.sprite.rect.size * 2;
        var hint = new GameObject("Continue Hint",typeof(RectTransform),typeof(Image),typeof(LayoutElement)); hint.transform.SetParent(bubble,false);
        hint.GetComponent<LayoutElement>().ignoreLayout = true;
        var hintImage=hint.GetComponent<Image>(); hintImage.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Evolution/Popups/Sprites/Shared/PopupButton9Slice.png"); hintImage.type=Image.Type.Sliced;
        var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)); label.transform.SetParent(hint.transform,false);
        var labelRect=(RectTransform)label.transform; labelRect.anchorMin=Vector2.zero; labelRect.anchorMax=Vector2.one;labelRect.offsetMin=new Vector2(12,4);labelRect.offsetMax=new Vector2(-12,-4);
        var hintText = label.GetComponent<TMP_Text>(); hintText.font=body.font; hintText.fontSize=body.fontSize; hintText.color=body.color;
        hintText.text="Продолжим"; hintText.alignment=TextAlignmentOptions.Center;
        var hr=(RectTransform)hint.transform; hr.anchorMin=hr.anchorMax=new Vector2(1,0); hr.pivot=new Vector2(1,1);
        hr.anchoredPosition=new Vector2(0,-12); hr.sizeDelta=new Vector2(380,48); labelRect.localScale=new Vector3(-1,1,1);
        hint.SetActive(false);
        foreach (var graphic in window.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        next.targetGraphic.raycastTarget=true;
        var tutorial = root.GetComponent<FirstRunLightTutorial>(); Set(tutorial,"window",window); Set(tutorial,"instruction",body);
        Set(tutorial,"continueHint",hint);
        Set(tutorial,"nextButton",next);
        window.SetActive(true);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root,Path); Object.DestroyImmediate(root);
        var active = SceneManager.GetActiveScene(); var world = SceneManager.GetSceneByPath("Assets/Scenes/World.unity"); bool opened = !world.isLoaded;
        if(opened) world = EditorSceneManager.OpenScene("Assets/Scenes/World.unity",OpenSceneMode.Additive);
        var roots = world.GetRootGameObjects();
        var lights = roots.SelectMany(g=>g.GetComponentsInChildren<WorldFlashlightAvailability>(true)).First();
        var canvas = roots.SelectMany(g=>g.GetComponentsInChildren<FirstPortalRunPresentation>(true)).First().GetComponentInParent<Canvas>().rootCanvas;
        var existing = roots.SelectMany(g=>g.GetComponentsInChildren<FirstRunLightTutorial>(true)).ToArray();
        foreach (var previous in existing) Object.DestroyImmediate(previous.gameObject);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,canvas.transform);
        Set(instance.GetComponent<FirstRunLightTutorial>(),"lights",lights);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.GetComponent<FirstRunLightTutorial>());
        EditorSceneManager.MarkSceneDirty(world); EditorSceneManager.SaveScene(world);
        if(opened) EditorSceneManager.CloseScene(world,true);
        if(active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        const string key = "world.tutorial.drag_light";
        var entry = table.entries.FirstOrDefault(e=>e.key==key);
        if(entry==null){entry = new LocalizationTable.Entry{key=key};table.entries.Add(entry);}
        entry.values = new(){"Переведи луч на соседний гекс. Подхвати его <size=64><sprite=0></size> и укажи, куда двигаться дальше – пора разведать ещё немного земли.","Guide the beam to a neighbouring hex. Take hold of it <size=64><sprite=0></size> and lead it onward. Time to scout a little farther."};
        foreach(var translation in new[]{
            new[]{"world.tutorial.activate_light","Включи свет у башни портала. Посмотрим, что Тёмные земли приберегли для нас сегодня.","Light the portal tower. Let's see what the Dark Lands have in store for us today."},
            new[]{"world.tutorial.scouting","Этот луч – наши глаза в Тёмных землях. Куда он доберётся, там и узнаем, что нас ждёт. Хорошая разведка сбережёт немало жизней.","That beam is our eyes in the Dark Lands. Wherever it reaches, we'll learn what awaits us. A little scouting can save a great many lives."},
            new[]{"world.tutorial.any_key","Продолжим","Let's continue"}})
        {
            var item=table.entries.FirstOrDefault(e=>e.key==translation[0]);
            if(item==null){item=new LocalizationTable.Entry{key=translation[0]};table.entries.Add(item);}
            item.values=new(){translation[1],translation[2]};
        }
        foreach(var translation in new[]{
            new[]{"world.tutorial.drag_neighbor","Переведи луч на соседний гекс. Подхвати его <size=64><sprite=0></size> и укажи, куда двигаться дальше – пора разведать ещё немного земли.","Guide the beam to a neighbouring hex. Take hold of it <size=64><sprite=0></size> and lead it onward. Time to scout a little farther."},
            new[]{"world.tutorial.workers","Люди трудятся только там, где светло. Хочешь добыть дерево, камень или еду – направь луч на нужный участок. В темноту я бы и сам без нужды не полез!","Our people can only work in the light. Need timber, stone or food? Guide the beam to the right patch of ground. I wouldn't venture into that darkness without good reason either!"},
            new[]{"world.tutorial.supplies","Отлично, теперь дело пойдёт! Добудь еду для наших семерых и дерево для домов. На сытый желудок даже Тёмные земли кажутся чуть приветливее.","Splendid, now we're getting somewhere! Gather food for our seven settlers and timber for their homes. Even the Dark Lands look a little friendlier on a full stomach."}})
        {
            var item=table.entries.FirstOrDefault(e=>e.key==translation[0]);
            if(item==null){item=new LocalizationTable.Entry{key=translation[0]};table.entries.Add(item);}
            item.values=new(){translation[1],translation[2]};
        }
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
        UpdateCarrierLocalization();
        Selection.activeObject = prefab;
    }
}
#endif
