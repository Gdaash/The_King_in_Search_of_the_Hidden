#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.UI;
using GameFoundation.Base;
using GameFoundation.MetaProgression;

public static class VerticalMilitaryHudSetup
{
    const string Shared = "Assets/Prefabs/UI/HUD/Military Side Panel.prefab";
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Shared) == null)
        {
        var source = PrefabUtility.LoadPrefabContents("Assets/Prefabs/UI/HUD/Base Military Overview.prefab");
        try
        {
            if (PrefabUtility.IsPartOfPrefabInstance(source)) PrefabUtility.UnpackPrefabInstance(source, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var data = new SerializedObject(source.GetComponent<BaseMilitaryOverview>());
            var panel = source.AddComponent<VerticalMilitaryPanel>();
            panel.title = (RectTransform)data.FindProperty("title").objectReferenceValue;
            panel.content = (RectTransform)data.FindProperty("content").objectReferenceValue;
            panel.emptyLabel = (Text)data.FindProperty("emptyLabel").objectReferenceValue;
            panel.itemTemplate = (WorldMilitaryRosterItemView)data.FindProperty("itemTemplate").objectReferenceValue;
            Object.DestroyImmediate(source.GetComponent<BaseMilitaryOverview>());
            Object.DestroyImmediate(source.GetComponent<WorldMilitaryRosterView>());
            source.name = "Military Side Panel";
            var rect = (RectTransform)source.transform;
            rect.anchorMin = new Vector2(1, 0); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(1, .5f);
            rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(120, 0);
            var bg = source.GetComponent<UnityEngine.UI.Image>();
            bg.sprite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Building Construction Tooltip.prefab").GetComponent<UnityEngine.UI.Image>().sprite;
            bg.type = UnityEngine.UI.Image.Type.Sliced; bg.color = Color.white; bg.raycastTarget = true;
            panel.title.anchorMin = panel.title.anchorMax = panel.title.pivot = new Vector2(.5f, 1);
            panel.title.anchoredPosition = new Vector2(0, -18); panel.title.sizeDelta = new Vector2(104, 28);
            panel.content.anchorMin = Vector2.zero; panel.content.anchorMax = Vector2.one;
            panel.content.offsetMin = new Vector2(16, 16); panel.content.offsetMax = new Vector2(-16, -56);
            panel.emptyLabel.rectTransform.anchorMin = panel.emptyLabel.rectTransform.anchorMax = new Vector2(.5f, 1);
            panel.emptyLabel.rectTransform.anchoredPosition = new Vector2(0, -26); panel.emptyLabel.rectTransform.sizeDelta = new Vector2(80, 28);
            panel.itemTemplate.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(source, Shared);
        }
        finally { PrefabUtility.UnloadPrefabContents(source); }
        }
        foreach (var path in new[] { "Assets/Prefabs/UI/HUD/Base Military Overview.prefab", "Assets/Prefabs/UI/HUD/Military Controls.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var children = root.transform.Cast<Transform>().ToArray();
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Shared), root.transform);
                var view = visual.GetComponent<VerticalMilitaryPanel>();
                foreach (var component in root.GetComponents<MonoBehaviour>())
                    if (component is BaseMilitaryOverview || component is WorldMilitaryRosterView)
                    {
                        var so = new SerializedObject(component);
                        so.FindProperty("title").objectReferenceValue = view.title;
                        so.FindProperty("content").objectReferenceValue = view.content;
                        so.FindProperty("emptyLabel").objectReferenceValue = view.emptyLabel;
                        so.FindProperty("itemTemplate").objectReferenceValue = view.itemTemplate;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                foreach (var child in children) Object.DestroyImmediate(child.gameObject);
                Stretch((RectTransform)root.transform);
                root.GetComponent<UnityEngine.UI.Image>().enabled = false;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in new[] { "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab", "Assets/Prefabs/UI/Screens/World Screen HUD.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in new[] { "Assets/Scenes/Base.unity", "Assets/Scenes/World.unity" })
        {
            var scene = EditorSceneManager.OpenScene(path);
            foreach (var root in scene.GetRootGameObjects()) Configure(root);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
    }
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f,.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }
    public static void Configure(GameObject root)
    {
        foreach (var view in root.GetComponentsInChildren<WorldMilitaryRosterView>(true))
        {
            Stretch((RectTransform)view.transform);
            var panels = view.GetComponentsInChildren<VerticalMilitaryPanel>(true);
            for (int i = 1; i < panels.Length; i++) Object.DestroyImmediate(panels[i].gameObject);
        }
        foreach (var t in root.GetComponentsInChildren<RectTransform>(true))
            if (t.name == "Title" && t.parent != null && t.parent.name == "Base Panel")
            {
                t.anchoredPosition = new Vector2(104, -24);
                PrefabUtility.RecordPrefabInstancePropertyModifications(t);
            }
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!(component is BaseMilitaryOverview) && !(component is WorldMilitaryRosterView)) continue;
            var visual = component.GetComponentInChildren<VerticalMilitaryPanel>(true);
            if (visual == null) continue;
            var so = new SerializedObject(component);
            so.FindProperty("title").objectReferenceValue = visual.title;
            so.FindProperty("content").objectReferenceValue = visual.content;
            so.FindProperty("emptyLabel").objectReferenceValue = visual.emptyLabel;
            so.FindProperty("itemTemplate").objectReferenceValue = visual.itemTemplate;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        var sidePanel = root.GetComponentInChildren<VerticalMilitaryPanel>(true);
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            var rect = (RectTransform)button.transform;
            if (button.name == "Settings")
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(20, -20); rect.sizeDelta = new Vector2(64,64);
                foreach (var label in button.GetComponentsInChildren<Graphic>(true))
                    if (label is Text || label is TMPro.TMP_Text) { label.gameObject.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(label.gameObject); }
                var iconTransform = button.transform.Find("Gear Icon");
                var go = iconTransform != null ? iconTransform.gameObject : new GameObject("Gear Icon", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(button.transform, false);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/IconSettings.png");
                ResourceIconSizing.Apply(image, image.sprite); image.raycastTarget = false;
                image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(.5f,.5f);
                image.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (button.name == "Next Day")
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0);
                rect.anchoredPosition = new Vector2(-136, 24);
                var follow = button.GetComponent<BesideMilitaryPanel>() ?? button.gameObject.AddComponent<BesideMilitaryPanel>();
                var so = new SerializedObject(follow); so.FindProperty("panel").objectReferenceValue = sidePanel; so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (button.name == "Next Day" || button.name == "Settings") PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
    }
}
#endif
