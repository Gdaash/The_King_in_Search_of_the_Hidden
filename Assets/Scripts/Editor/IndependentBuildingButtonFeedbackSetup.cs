#if UNITY_EDITOR
using System;
using System.Linq;
using GameFoundation.Base;
using GameFoundation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Keeps building/upgrade placement together, but animates each button's own visuals.</summary>
public static class IndependentBuildingButtonFeedbackSetup
{
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing prefabs.");
        foreach (var path in new[] {
                     "Assets/Prefabs/Base/Islands/Fort Island.prefab",
                     "Assets/Prefabs/Base/Islands/Archery Range Island.prefab",
                     "Assets/Prefabs/Base/Islands/Housing Island.prefab",
                     "Assets/Prefabs/Base/Base Panel.prefab",
                     "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab",
                     "Assets/Prefabs/Base/Variants/Base Location Variant.prefab",
                     "Assets/Prefabs/Base/Variants/Base Islands Backup.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var upgrade in root.GetComponentsInChildren<BuildingUpgradeButton>(true))
                {
                    var button = upgrade.transform.parent.GetComponent<Button>();
                    if (button != null) Configure(button);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    public static void Configure(Button button)
    {
        var rootImage = button.GetComponent<Image>();
        var feedback = button.GetComponent<UnifiedButtonFeedback>();
        if (rootImage == null || feedback == null) return;
        var visual = button.transform.Find("Button Visual") as RectTransform;
        bool created = visual == null;
        if (created)
        {
            visual = new GameObject("Button Visual", typeof(RectTransform)).GetComponent<RectTransform>();
            visual.SetParent(button.transform, false);
            visual.SetAsFirstSibling();
            visual.anchorMin = Vector2.zero; visual.anchorMax = Vector2.one;
            visual.offsetMin = visual.offsetMax = Vector2.zero;
        }
        var image = visual.GetComponent<Image>() ?? visual.gameObject.AddComponent<Image>();
        // Take any variant-specific artwork before turning the parent into a hit area.
        if (created || rootImage.sprite != null)
        {
            EditorUtility.CopySerialized(rootImage, image);
            image.raycastTarget = false;
        }
        var outline = button.GetComponent<Outline>();
        if (outline != null && outline.enabled)
        {
            var visualOutline = visual.GetComponent<Outline>() ?? visual.gameObject.AddComponent<Outline>();
            EditorUtility.CopySerialized(outline, visualOutline);
            outline.enabled = false;
        }
        foreach (string name in new[] { "Label", "Action Available Indicator" })
        {
            var child = button.transform.Find(name);
            if (child != null) child.SetParent(visual, false);
        }
        rootImage.sprite = null; rootImage.overrideSprite = null; rootImage.color = Color.clear;
        rootImage.type = Image.Type.Simple;
        button.targetGraphic = image;
        var serialized = new SerializedObject(feedback);
        serialized.FindProperty("scaleTarget").objectReferenceValue = visual;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var island = button.GetComponent<CanvasBuildingIsland>();
        if (island != null)
        {
            var fields = new SerializedObject(island);
            fields.FindProperty("buttonBackground").objectReferenceValue = image;
            fields.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var component in new Component[] { rootImage, button, feedback, island, outline })
            if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
}
#endif
