#if UNITY_EDITOR
using System.Linq;
using GameFoundation.UI;
using GameFoundation.Quests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JournalHudSetup
{
    const float BarHeight = 76.8f;
    static void Frame(UnityEngine.UI.Image image)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab").transform.Find("Day Panel").GetComponent<UnityEngine.UI.Image>();
        EditorUtility.CopySerialized(source, image);
        image.raycastTarget = false;
    }
    static void Feed(NotificationFeed feed)
    {
        var rect = (RectTransform)feed.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(0, BarHeight);
        rect.sizeDelta = new Vector2(440, 272);
        var serialized = new SerializedObject(feed);
        serialized.FindProperty("maximumRows").intValue = 5;
        serialized.FindProperty("retainUntilReplaced").boolValue = true;
        serialized.FindProperty("rowSpacing").floatValue = 2;
        serialized.FindProperty("contentInsets").vector2Value = new Vector2(16, 12);
        serialized.FindProperty("growUpwards").boolValue = true;
        var row = serialized.FindProperty("rowPrefab").objectReferenceValue as NotificationRow;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (row)
        {
            var r = new SerializedObject(row);
            r.FindProperty("singleLine").boolValue = true;
            r.FindProperty("minimumHeight").floatValue = 48;
            var text = r.FindProperty("label").objectReferenceValue as UnityEngine.UI.Text;
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 18;
            r.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(row);
            PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(feed);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }
    static void Speed(GameSpeedControls controls)
    {
        var rect = (RectTransform)controls.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(200, 0);
        rect.sizeDelta = new Vector2(240, BarHeight);
        var image = controls.GetComponent<UnityEngine.UI.Image>() ?? controls.gameObject.AddComponent<UnityEngine.UI.Image>();
        Frame(image);
        var layout = controls.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layout) layout.enabled = false;
        int index = 0;
        foreach (string name in new[] { "Pause", "Speed 1", "Speed 2", "Speed 4" })
        {
            var button = controls.transform.Find(name) as RectTransform;
            if (!button) continue;
            button.anchorMin = button.anchorMax = button.pivot = new Vector2(.5f, .5f);
            button.anchoredPosition = new Vector2(-72 + index++ * 48, 4);
            button.sizeDelta = new Vector2(38, 38);
            button.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }
    static void Scene()
    {
        var feed = Object.FindObjectsByType<NotificationFeed>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (!feed) return;
        Feed(feed);
        var canvas = feed.GetComponentInParent<Canvas>().rootCanvas.transform;
        var panel = canvas.Find("Journal Panel") as RectTransform;
        if (!panel)
        {
            panel = new GameObject("Journal Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(JournalHudLayout)).GetComponent<RectTransform>();
            panel.SetParent(canvas, false);
        }
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(200, BarHeight);
        Frame(panel.GetComponent<UnityEngine.UI.Image>());
        var title = panel.Find("Title") as RectTransform;
        if (!title)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab").transform.Find("Day Panel/Day");
            title = (RectTransform)Object.Instantiate(source.gameObject, panel, false).transform;
            title.name = "Title";
        }
        title.anchorMin = title.anchorMax = title.pivot = new Vector2(.5f, .5f);
        title.anchoredPosition = new Vector2(16, 3);
        title.sizeDelta = new Vector2(144, 40);
        foreach (var text in title.GetComponentsInChildren<TMPro.TMP_Text>(true))
        { text.text = "Журнал"; text.fontSize = 24; text.alignment = TMPro.TextAlignmentOptions.Center; text.raycastTarget = false; }
        foreach (var text in title.GetComponentsInChildren<UnityEngine.UI.Text>(true))
        { text.text = "Журнал"; text.fontSize = 24; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; }
        var quests = Object.FindObjectsByType<QuestPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(q => q.GetComponentInParent<Canvas>().rootCanvas.transform == canvas).OrderBy(q => q.name == "Quest Panel" ? 0 : 1).ToArray();
        foreach (var quest in quests)
        {
            var stack = quest.GetComponent<QuestPanelStackBelow>(); if (stack) { stack.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(stack); }
            var align = quest.GetComponent<BesideMilitaryPanel>(); if (align) { align.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(align); }
            var rect = (RectTransform)quest.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(0, BarHeight + 272);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 440);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
        var hud = panel.GetComponent<JournalHudLayout>() ?? panel.gameObject.AddComponent<JournalHudLayout>();
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("log").objectReferenceValue = feed.transform;
        var entries = serialized.FindProperty("quests"); entries.arraySize = quests.Length;
        for (int i = 0; i < quests.Length; i++) entries.GetArrayElementAtIndex(i).objectReferenceValue = quests[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (var controls in Object.FindObjectsByType<GameSpeedControls>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Speed(controls);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
    public static void Apply()
    {
        // Keep the shared feed and World HUD prefab consistent with the authored scenes.
        foreach (string path in new[] { "Assets/Prefabs/UI/HUD/Notification Feed.prefab", "Assets/Prefabs/UI/HUD/Game Speed Controls.prefab", "Assets/Prefabs/UI/Screens/World Screen HUD.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var feed in root.GetComponentsInChildren<NotificationFeed>(true)) Feed(feed);
                foreach (var speed in root.GetComponentsInChildren<GameSpeedControls>(true)) Speed(speed);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        string original = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        foreach (string name in new[] { "Base", "World" })
        { EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity"); Scene(); }
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();
    }
}
#endif
