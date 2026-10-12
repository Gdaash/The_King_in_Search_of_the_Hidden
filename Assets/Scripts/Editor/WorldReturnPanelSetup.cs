#if UNITY_EDITOR
using System.Linq;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WorldReturnPanelSetup
{
    static void Configure(GameObject root)
    {
        var button = root.GetComponentsInChildren<UnityEngine.UI.Button>(true).FirstOrDefault(b => b.name == "EscapeButton");
        if (!button) return;
        var hud = button.GetComponentInParent<Canvas>().rootCanvas.transform;
        var panel = hud.Find("Return Panel") as RectTransform;
        if (!panel)
        {
            panel = new GameObject("Return Panel", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup)).GetComponent<RectTransform>();
            panel.SetParent(hud, false);
        }
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 0);
        panel.anchoredPosition = new Vector2(-120, 0);
        panel.sizeDelta = new Vector2(208, 76.8f);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Base/Base Panel.prefab").transform.Find("Day Panel");
        EditorUtility.CopySerialized(source.GetComponent<UnityEngine.UI.Image>(), panel.GetComponent<UnityEngine.UI.Image>());
        panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var align = panel.GetComponent<BesideMilitaryPanel>() ?? panel.gameObject.AddComponent<BesideMilitaryPanel>();
        var a = new SerializedObject(align);
        a.FindProperty("panel").objectReferenceValue = root.GetComponentInChildren<VerticalMilitaryPanel>(true);
        a.FindProperty("gap").floatValue = 0;
        a.FindProperty("preserveVerticalPosition").boolValue = true;
        a.ApplyModifiedPropertiesWithoutUndo();
        var dragon = panel.Find("Dragon") as RectTransform;
        if (!dragon)
        {
            dragon = new GameObject("Dragon", typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<RectTransform>();
            dragon.SetParent(panel, false);
        }
        EditorUtility.CopySerialized(source.Find("Dragon").GetComponent<UnityEngine.UI.Image>(), dragon.GetComponent<UnityEngine.UI.Image>());
        dragon.anchorMin = dragon.anchorMax = new Vector2(1, 1);
        dragon.pivot = new Vector2(1, 0);
        dragon.anchoredPosition = new Vector2(0, -8);
        dragon.sizeDelta = dragon.GetComponent<UnityEngine.UI.Image>().sprite.rect.size * 2;
        dragon.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        if (button.transform.parent != panel) button.transform.SetParent(panel, false);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, 4);
        rect.sizeDelta = new Vector2(166, 48);
        rect.localScale = Vector3.one;
        foreach (var text in button.GetComponentsInChildren<UnityEngine.UI.Text>(true)) text.text = "Вернуться";
        foreach (var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.text = "Вернуться";
        var visibility = panel.GetComponent<WorldReturnPanel>() ?? panel.gameObject.AddComponent<WorldReturnPanel>();
        var s = new SerializedObject(visibility);
        s.FindProperty("returnButton").objectReferenceValue = button; s.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
    }

    public static void Apply()
    {
        const string path = "Assets/Prefabs/UI/Screens/World Screen HUD.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try { Configure(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        string original = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/World.unity");
        foreach (var root in scene.GetRootGameObjects()) Configure(root);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        if (!string.IsNullOrEmpty(original) && original != scene.path) EditorSceneManager.OpenScene(original);
        AssetDatabase.SaveAssets();
    }
}
#endif
