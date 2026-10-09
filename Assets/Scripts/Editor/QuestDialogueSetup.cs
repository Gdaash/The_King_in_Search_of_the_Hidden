#if UNITY_EDITOR
using System.Linq;
using GameFoundation.Quests;
using GameFoundation.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class QuestDialogueSetup
{
    public const string ViewPath = "Assets/Prefabs/UI/Dialogue/Quest Dialogue.prefab";
    public const string FirstPath = "Assets/Prefabs/Quests/01 First Supplies.prefab";
    static void Set(Object o, string field, Object value) { var s = new SerializedObject(o); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static QuestDialogueDefinition Make(string path, string id, QuestDefinition quest, QuestDefinition prerequisite, params string[] lines)
    {
        var g = new GameObject(id); var d = g.AddComponent<QuestDialogueDefinition>();
        d.id = id; d.speakerName = "Старик Пью"; d.portrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Dialogue/Old Pew.png"); d.quest = quest; d.prerequisite = prerequisite; d.lines = lines;
        var saved = PrefabUtility.SaveAsPrefabAsset(g, path).GetComponent<QuestDialogueDefinition>(); Object.DestroyImmediate(g); return saved;
    }
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new System.Exception("Exit Play Mode first");
        var importer = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Ui/Dialogue/Old Pew.png");
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 32; importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.SaveAndReimport();
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Quests"); AssetDatabase.Refresh();
        var first = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/First Expedition.asset");
        var archers = AssetDatabase.LoadAssetAtPath<QuestDefinition>("Assets/Resources/Quests/Recruit Two Archers.asset");
        var d1 = Make(FirstPath, "pew_first_supplies", first, null,
            "Рыцарь, добро пожаловать в новые владения. Прежде всего нам нужно собрать припасы, чтобы пережить первую ночь.",
            "Отправляйся через портал в Тёмные земли. Добудь еду и стройматериалы: древесину и камень. Они помогут нам обустроить укрытие.");
        var d2 = Make("Assets/Prefabs/Quests/02 Two Archers.prefab", "pew_two_archers", archers, first,
            "Припасы собраны, но владениям нужна защита. Построй кузницу и стрельбище, затем найми двух лучников. После этого мы сможем принять беженцев.");
        var root = new GameObject("Quest Dialogue", typeof(RectTransform)); var view = root.AddComponent<QuestDialogueView>();
        var window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Dialogue/Royal Scribe Dialogue.prefab"), root.transform);
        window.name = "Dialogue Window";
        var dimmer = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Full Screen Popup Dimmer.prefab"), root.transform);
        dimmer.name = "Dialogue Screen Dimmer"; dimmer.transform.SetAsFirstSibling();
        Set(window.AddComponent<PopupDimmerLink>(), "dimmer", dimmer);
        var wr = (RectTransform)window.transform; wr.anchorMin = wr.anchorMax = wr.pivot = new Vector2(.5f,.5f); wr.anchoredPosition = Vector2.zero;
        var bubble = window.transform.Find("Speech Bubble");
        var speaker = bubble.Find("Speaker Name").GetComponent<TMP_Text>(); speaker.text = d1.speakerName;
        var body = bubble.Find("Dialogue Text").GetComponent<TMP_Text>(); body.text = d1.lines[0];
        var portrait = bubble.Find("Scribe").GetComponent<Image>(); portrait.sprite = d1.portrait;
        var buttonGo = new GameObject("Next", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)); buttonGo.transform.SetParent(bubble,false);
        buttonGo.GetComponent<LayoutElement>().ignoreLayout = true;
        var rt = (RectTransform)buttonGo.transform; rt.anchorMin = rt.anchorMax = new Vector2(1,0); rt.pivot = new Vector2(1,1); rt.anchoredPosition = new Vector2(0,-12); rt.sizeDelta = new Vector2(240,56);
        var im = buttonGo.GetComponent<Image>(); im.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Evolution/Popups/Sprites/Shared/PopupButton9Slice.png"); im.type = Image.Type.Sliced;
        var button = buttonGo.GetComponent<Button>(); button.targetGraphic = im;
        var feedback = buttonGo.AddComponent<UnifiedButtonFeedback>(); Set(feedback,"theme",AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset"));
        var labelGo = new GameObject("Label",typeof(RectTransform)); labelGo.transform.SetParent(buttonGo.transform,false);
        var lr = (RectTransform)labelGo.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = new Vector2(16,8); lr.offsetMax = new Vector2(-16,-8);
        var label = labelGo.AddComponent<TextMeshProUGUI>(); label.font = speaker.font; label.fontSize = 22; label.alignment = TextAlignmentOptions.Center; label.color = new Color(.55f,.76f,.50f); label.text = "Далее"; label.raycastTarget = false;
        Set(view,"window",window); Set(view,"speaker",speaker); Set(view,"dialogue",body); Set(view,"portrait",portrait); Set(view,"nextButton",button); Set(view,"nextLabel",label);
        Set(view,"buttonTheme",AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>("Assets/Resources/UI/ButtonVisualTheme.asset"));
        var so = new SerializedObject(view); var list = so.FindProperty("conversations"); list.arraySize = 2; list.GetArrayElementAtIndex(0).objectReferenceValue = d1; list.GetArrayElementAtIndex(1).objectReferenceValue = d2; so.ApplyModifiedPropertiesWithoutUndo();
        ((RectTransform)root.transform).anchorMin = Vector2.zero; ((RectTransform)root.transform).anchorMax = Vector2.one; ((RectTransform)root.transform).sizeDelta = Vector2.zero;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root,ViewPath); Object.DestroyImmediate(root);
        var active = SceneManager.GetActiveScene(); var scene = SceneManager.GetSceneByPath("Assets/Scenes/Base.unity"); bool opened = !scene.isLoaded; if(opened) scene = EditorSceneManager.OpenScene("Assets/Scenes/Base.unity",OpenSceneMode.Additive);
        var panel = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<QuestPanel>(true)).First();
        var existing = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<QuestDialogueView>(true)).FirstOrDefault();
        if(existing==null) existing=((GameObject)PrefabUtility.InstantiatePrefab(prefab,panel.GetComponentInParent<Canvas>().rootCanvas.transform)).GetComponent<QuestDialogueView>();
        Set(existing,"questPanel",panel); panel.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); if(opened)EditorSceneManager.CloseScene(scene,true);
        if(active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active);
        AssetDatabase.SaveAssets(); Selection.activeObject=AssetDatabase.LoadAssetAtPath<GameObject>(FirstPath);
        Debug.Log("Quest dialogues installed in Base; configuration: "+FirstPath);
    }
}
#endif
