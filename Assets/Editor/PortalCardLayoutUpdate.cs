#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GameFoundation.MetaProgression;

public static class PortalCardLayoutUpdate
{
    static void Position(RectTransform rect, float y)
    { var p = rect.anchoredPosition; p.y = y; rect.anchoredPosition = p; }

    static void Layout(PortalUpgradeCards view)
    {
        var window = view.transform.Find("Window") as RectTransform;
        if (window == null) throw new System.InvalidOperationException("Missing upgrade window.");
        window.sizeDelta = new Vector2(window.sizeDelta.x, 850);
        Position(view.heading.rectTransform, 370);
        Position(view.hint.rectTransform, 316);
        Position((RectTransform)view.rerollButton.transform, -360);
        foreach (var c in view.cards)
        {
            var card = (RectTransform)c.button.transform.parent;
            card.sizeDelta = new Vector2(card.sizeDelta.x, 552);
            Position(c.category.rectTransform, 242);
            c.icon.rectTransform.sizeDelta = new Vector2(96, 96);
            Position(c.icon.rectTransform, 170);
            Position(c.title.rectTransform, 70);
            Position(c.description.rectTransform, -83);
            Position((RectTransform)c.button.transform, -232);
            if (c.icon.rectTransform.anchoredPosition.y - 48 <= c.title.rectTransform.anchoredPosition.y + c.title.rectTransform.sizeDelta.y / 2)
                throw new System.InvalidOperationException("Icon overlaps title.");
        }
        EditorUtility.SetDirty(view);
    }

    static void Sort(FlashlightActivationButton button)
    {
        var so = new SerializedObject(button);
        var background = so.FindProperty("background").objectReferenceValue as SpriteRenderer;
        var text = so.FindProperty("label").objectReferenceValue as TMPro.TMP_Text;
        if (background == null || text == null) throw new System.InvalidOperationException("Missing light button references.");
        background.sortingLayerName = "OverLight"; background.sortingOrder = 130;
        var renderer = text.GetComponent<Renderer>();
        renderer.sortingLayerName = "OverLight"; renderer.sortingOrder = 131;
        EditorUtility.SetDirty(background); EditorUtility.SetDirty(renderer);
    }

    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var paths = new[] {"Assets/Prefabs/UI/Portal Tower Progression.prefab", "Assets/Prefabs/Flashlight.prefab"};
        foreach (var path in paths)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var v in root.GetComponentsInChildren<PortalUpgradeCards>(true)) Layout(v);
                foreach (var b in root.GetComponentsInChildren<FlashlightActivationButton>(true)) Sort(b);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/World.unity", OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var v in root.GetComponentsInChildren<PortalUpgradeCards>(true)) Layout(v);
            foreach (var b in root.GetComponentsInChildren<FlashlightActivationButton>(true)) Sort(b);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("PORTAL_LAYOUT_OK: 96px icons, taller cards/window, button sorting OverLight 130/131.");
    }
}
#endif
