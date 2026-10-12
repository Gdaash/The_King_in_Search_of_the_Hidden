#if UNITY_EDITOR
using System.Linq;
using GameFoundation.UI;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HudCompactPolishSetup
{
    static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/" + name + ".png");
    static UnityEngine.UI.Image Image(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (!child) { child = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).transform; child.SetParent(parent, false); }
        return child.GetComponent<UnityEngine.UI.Image>() ?? child.gameObject.AddComponent<UnityEngine.UI.Image>();
    }
    static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one; }
    static void Panel(UnityEngine.UI.Image image)
    { image.sprite = Sprite("Day Panel"); image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1.5625f; image.color = Color.white; image.raycastTarget = false; }
    static void Journal()
    {
        var feed = Object.FindAnyObjectByType<NotificationFeed>();
        if (!feed) return;
        var canvas = feed.GetComponentInParent<Canvas>().rootCanvas.transform;
        var journal = canvas.Find("Journal Panel");
        if (!journal) return;
        var buttonImage = Image(journal, "Toggle Journal");
        buttonImage.sprite = Sprite("Day Button Background"); buttonImage.type = UnityEngine.UI.Image.Type.Sliced;
        buttonImage.pixelsPerUnitMultiplier = 1.5625f; buttonImage.raycastTarget = true;
        Place(buttonImage.rectTransform, new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(30, 3), new Vector2(32, 40));
        var button = buttonImage.GetComponent<UnityEngine.UI.Button>() ?? buttonImage.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = buttonImage;
        var feedback = button.GetComponent<UnifiedButtonFeedback>() ?? button.gameObject.AddComponent<UnifiedButtonFeedback>();
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab").GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "Next Day").GetComponent<UnifiedButtonFeedback>();
        var serialized = new SerializedObject(feedback);
        serialized.FindProperty("theme").objectReferenceValue = new SerializedObject(source).FindProperty("theme").objectReferenceValue;
        serialized.FindProperty("animateScale").boolValue = true;
        serialized.FindProperty("hoverScaleOverride").floatValue = 1.08f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var arrow = Image(button.transform, "Arrow"); arrow.sprite = Sprite("Journal Arrow"); arrow.raycastTarget = false;
        Place(arrow.rectTransform, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, arrow.sprite.rect.size * 2);
        var layout = new SerializedObject(journal.GetComponent<JournalHudLayout>());
        layout.FindProperty("toggleButton").objectReferenceValue = button;
        layout.FindProperty("arrow").objectReferenceValue = arrow.rectTransform;
        layout.ApplyModifiedPropertiesWithoutUndo();
        Vector2[] anchors = { new(0,1), new(1,1), new(0,0), new(1,0) };
        for (int i = 0; i < 4; i++)
        {
            var corner = Image(feed.transform, "Journal Corner " + i); corner.sprite = Sprite("Journal Corner"); corner.raycastTarget = false;
            var a = anchors[i]; Place(corner.rectTransform, a, a, new Vector2(a.x == 0 ? 8 : -8, a.y == 0 ? 8 : -8), corner.sprite.rect.size * 2);
            corner.rectTransform.localScale = new Vector3(a.x == 0 ? 1 : -1, a.y == 1 ? 1 : -1, 1);
            // Use a centered pivot for mirroring without shifting the corner beyond its bounds.
            corner.rectTransform.pivot = new Vector2(.5f,.5f);
            corner.rectTransform.anchoredPosition = new Vector2(a.x == 0 ? 13 : -13, a.y == 0 ? 13 : -13);
        }
    }
    static void Roster(GameObject root)
    {
        foreach (var panel in root.GetComponentsInChildren<VerticalMilitaryPanel>(true))
        { var s = new SerializedObject(panel); s.FindProperty("cellSize").vector2Value = new Vector2(88,88); s.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.RecordPrefabInstancePropertyModifications(panel); }
        foreach (var view in root.GetComponentsInChildren<WorldMilitaryRosterItemView>(true))
        {
            var rect = (RectTransform)view.transform; rect.sizeDelta = new Vector2(88,88);
            var image = view.GetComponent<UnityEngine.UI.Image>() ?? view.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = Sprite("Day Button Background"); image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.5625f; image.color = Color.white; image.raycastTarget = true;
            var s = new SerializedObject(view);
            var fill = s.FindProperty("healthFill").objectReferenceValue as UnityEngine.UI.Image;
            if (fill)
            {
                var frame = fill.transform.parent.GetComponent<UnityEngine.UI.Image>();
                frame.sprite = Sprite("Health Frame Compact"); frame.type = UnityEngine.UI.Image.Type.Simple;
                Place(frame.rectTransform, new Vector2(.5f,1), new Vector2(.5f,1), new Vector2(0,-6), new Vector2(62,14));
                fill.sprite = Sprite("Health Fill Compact"); fill.type = UnityEngine.UI.Image.Type.Simple;
                Place(fill.rectTransform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(2,0), new Vector2(58,10));
            }
            var stars = view.transform.Find("Stars") as RectTransform;
            if (stars) stars.anchoredPosition = new Vector2(0,-22);
            var icon = s.FindProperty("icon").objectReferenceValue as UnityEngine.UI.Image;
            if (icon) icon.rectTransform.anchoredPosition = new Vector2(0,-38);
            foreach (var component in view.GetComponentsInChildren<Component>(true)) if (component) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }
    static void UnitBars(GameObject root)
    {
        if (!root.GetComponent<EnemyAI>() && !root.GetComponent<EnemyAI_Ranged>()) return;
        if (!root.name.StartsWith("Red")) return;
        foreach (var health in root.GetComponentsInChildren<HealthBar>(true))
        {
            var s = new SerializedObject(health);
            var main = s.FindProperty("mainBar").objectReferenceValue as UnityEngine.UI.Image;
            var buffer = s.FindProperty("bufferBar").objectReferenceValue as UnityEngine.UI.Image;
            if (!main) continue;
            var frame = health.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(i => i != main && i != buffer);
            if (!frame) continue;
            float pixel = frame.rectTransform.rect.height / 7;
            frame.sprite = Sprite("Health Frame Compact");
            frame.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pixel * 31);
            foreach (var image in new[] { main, buffer })
            {
                if (!image) continue;
                image.sprite = Sprite(image == main ? "Health Fill Compact" : "Health Damage Buffer Compact");
                image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, pixel * 29);
            }
        }
    }
    static void Return(GameObject root)
    {
        foreach (var controller in root.GetComponentsInChildren<WorldEscapeController>(true))
        {
            var s = new SerializedObject(controller);
            var button = s.FindProperty("escapeButton").objectReferenceValue as UnityEngine.UI.Button;
            var label = s.FindProperty("buttonLabel").objectReferenceValue as TMPro.TMP_Text;
            if (label) { label.enableAutoSizing = true; label.fontSizeMin = 12; label.fontSizeMax = 24; label.enableWordWrapping = false; }
            var progress = s.FindProperty("progressPanel").objectReferenceValue as GameObject;
            var fill = s.FindProperty("progressFill").objectReferenceValue as UnityEngine.UI.Image;
            var count = s.FindProperty("progressLabel").objectReferenceValue as TMPro.TMP_Text;
            var human = s.FindProperty("humanResource").objectReferenceValue as ResourceType;
            if (!button || !progress || !fill) continue;
            if (progress.transform.parent != button.transform.parent) progress.transform.SetParent(button.transform.parent, false);
            var rect = (RectTransform)progress.transform;
            Place(rect, Vector2.zero, new Vector2(1,0), Vector2.zero, new Vector2(360,76.8f));
            Panel(progress.GetComponent<UnityEngine.UI.Image>() ?? progress.AddComponent<UnityEngine.UI.Image>());
            var track = Image(progress.transform, "Track");
            track.transform.SetAsFirstSibling(); track.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ui/Run Bars/Experience Fill.png");
            track.color = new Color(.24f,.2f,.29f,1); track.raycastTarget = false;
            Place(track.rectTransform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(16,4), track.sprite.rect.size * 2);
            fill.sprite = track.sprite; fill.color = Color.white; fill.raycastTarget = false;
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0;
            Place(fill.rectTransform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(16,4), fill.sprite.rect.size * 2);
            if (count)
            {
                Place(count.rectTransform, new Vector2(0,.5f), new Vector2(0,.5f), new Vector2(16,4), new Vector2(270,28));
                count.fontSize = 18; count.alignment = TMPro.TextAlignmentOptions.Center; count.raycastTarget = false;
                count.transform.SetAsLastSibling();
            }
            if (human && human.resourceIcon)
            {
                var icon = Image(progress.transform, "Human Icon"); icon.sprite = human.resourceIcon; icon.raycastTarget = false;
                Place(icon.rectTransform, new Vector2(1,.5f), new Vector2(.5f,.5f), new Vector2(-34,4), human.resourceIcon.rect.size * 2);
            }
            foreach (var component in progress.GetComponentsInChildren<Component>(true)) if (component) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            if (label) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }
    }
    public static void Apply()
    {
        AssetDatabase.Refresh();
        foreach (string name in new[] { "Journal Corner", "Journal Arrow", "Health Frame Compact", "Health Fill Compact", "Health Damage Buffer Compact" })
        {
            var i = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/Ui/" + name + ".png");
            i.textureType = TextureImporterType.Sprite; i.spriteImportMode = SpriteImportMode.Single; i.spritePixelsPerUnit = 32;
            i.filterMode = FilterMode.Point; i.textureCompression = TextureImporterCompression.Uncompressed; i.mipmapEnabled = false; i.SaveAndReimport();
        }
        JournalHudSetup.Apply();
        foreach (string id in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(id); var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset.GetComponentInChildren<WorldMilitaryRosterItemView>(true) && !asset.GetComponentInChildren<WorldEscapeController>(true) && !asset.name.StartsWith("Red")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Roster(root); UnitBars(root); Return(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        string original = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        foreach (string name in new[] { "Base", "World" })
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            Journal(); foreach (var root in scene.GetRootGameObjects()) { Roster(root); Return(root); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();
    }
}
#endif
