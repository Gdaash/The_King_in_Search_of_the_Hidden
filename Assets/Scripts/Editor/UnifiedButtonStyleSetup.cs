#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GameFoundation.Base;
using GameFoundation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UnifiedButtonStyleSetup
{
    private const string ThemePath = "Assets/Prefabs/UI/ButtonVisualTheme.asset";
    private const string ButtonSpritePath =
        "Assets/Sprites/Evolution adventure/Sprites/UI/Sprites/Popups/Sprites/Shared/PopupButton9Slice.png";

    [MenuItem("Game/UI/Apply Unified Button Style")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlaying) return;
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            Debug.LogError("Save the current scene before applying the button style.");
            return;
        }

        var theme = AssetDatabase.LoadAssetAtPath<ButtonVisualTheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<ButtonVisualTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }
        theme.textButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonSpritePath);
        if (theme.textButtonSprite == null)
        {
            Debug.LogError("Shared popup button sprite was not found.");
            return;
        }
        EditorUtility.SetDirty(theme);

        var prefabPaths = new List<string>();
        foreach (var id in AssetDatabase.FindAssets("t:Prefab",
                     new[] { "Assets/Prefabs", "Assets/Resources/LegacyFindIt/Prefabs" }))
            prefabPaths.Add(AssetDatabase.GUIDToAssetPath(id));
        prefabPaths.Sort((a, b) => Priority(a).CompareTo(Priority(b)));

        int buttonCount = 0;
        foreach (var path in prefabPaths)
        {
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    Apply(button, theme);
                    changed = true;
                    buttonCount++;
                }
                if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (root != null) PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (var path in new[]
                 {
                     "Assets/Scenes/Base.unity", "Assets/Scenes/World.unity",
                     "Assets/Scenes/MainMenu.unity"
                 })
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int inScene = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                Apply(button, theme);
                inScene++;
            }
            if (inScene == 0) continue;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            buttonCount += inScene;
        }

        if (!string.IsNullOrEmpty(activeScene.path))
            EditorSceneManager.OpenScene(activeScene.path, OpenSceneMode.Single);
        AssetDatabase.SaveAssets();
        Debug.Log($"Unified button style applied to {buttonCount} prefab and scene buttons.");
    }

    private static int Priority(string path)
    {
        if (path.EndsWith("/SkillButton.prefab", StringComparison.OrdinalIgnoreCase)) return 0;
        if (path.EndsWith("/Laboratory Skill Tree.prefab", StringComparison.OrdinalIgnoreCase)) return 1;
        if (path.EndsWith("/Laboratory Popup.prefab", StringComparison.OrdinalIgnoreCase)) return 2;
        return 3;
    }

    private static void Apply(Button button, ButtonVisualTheme theme)
    {
        // Tooltips use Button only as a convenient raycast target. Their root image is
        // a stretchable tooltip background and must not receive the shared button art.
        if (button.name.IndexOf("Tooltip", StringComparison.OrdinalIgnoreCase) >= 0 ||
            button.GetComponent<TooltipManager>() != null)
            return;

        bool skill = button.GetComponent<SkillButton>() != null;
        bool warehouse = button.name == "Buy Cart" &&
                         button.GetComponentInParent<WarehouseCartPurchaseView>(true) != null;
        bool portal = button.GetComponent<PortalSiteButtonView>() != null;
        bool customPalette = warehouse || portal;

        var image = button.targetGraphic as Image;
        if (image != null && !skill && IsTextButtonSprite(image.sprite))
        {
            image.sprite = theme.textButtonSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
            EditorUtility.SetDirty(image);
        }
        if (image != null)
        {
            var outline = image.GetComponent<Outline>();
            if (outline == null && !PrefabUtility.IsPartOfPrefabInstance(image.gameObject))
                outline = image.gameObject.AddComponent<Outline>();
            if (outline != null)
            {
                outline.effectDistance = theme.outlineDistance;
                outline.useGraphicAlpha = false;
                var outlineColor = theme.hoverOutlineColor;
                outlineColor.a = 0f;
                outline.effectColor = outlineColor;
                PrefabUtility.RecordPrefabInstancePropertyModifications(outline);
                EditorUtility.SetDirty(outline);
            }
        }

        button.transition = Selectable.Transition.None;
        var colors = button.colors;
        colors.normalColor = theme.normalColor;
        colors.highlightedColor = theme.hoverColor;
        colors.selectedColor = theme.hoverColor;
        colors.pressedColor = theme.pressedColor;
        colors.disabledColor = theme.disabledColor;
        button.colors = colors;
        PrefabUtility.RecordPrefabInstancePropertyModifications(button);
        EditorUtility.SetDirty(button);

        var feedback = button.GetComponent<UnifiedButtonFeedback>();
        if (feedback == null) feedback = button.gameObject.AddComponent<UnifiedButtonFeedback>();
        var serialized = new SerializedObject(feedback);
        serialized.FindProperty("theme").objectReferenceValue = theme;
        serialized.FindProperty("animateScale").boolValue = !skill;
        serialized.FindProperty("useButtonPalette").boolValue = customPalette;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(feedback);
        EditorUtility.SetDirty(feedback);

        if (skill)
        {
            var skillButton = button.GetComponent<SkillButton>();
            var skillSerialized = new SerializedObject(skillButton);
            skillSerialized.FindProperty("hoverScale").floatValue = theme.hoverScale;
            skillSerialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(skillButton);
        }
        if (warehouse)
            SetPalette(button.GetComponentInParent<WarehouseCartPurchaseView>(true), theme);
        if (portal)
            SetPalette(button.GetComponent<PortalSiteButtonView>(), theme);
    }

    private static void SetPalette(UnityEngine.Object view, ButtonVisualTheme theme)
    {
        var serialized = new SerializedObject(view);
        serialized.FindProperty("buttonNormalColor").colorValue = theme.normalColor;
        serialized.FindProperty("buttonHighlightedColor").colorValue = theme.hoverColor;
        serialized.FindProperty("buttonPressedColor").colorValue = theme.pressedColor;
        serialized.FindProperty("buttonDisabledColor").colorValue = theme.disabledColor;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        EditorUtility.SetDirty(view);
    }

    private static bool IsTextButtonSprite(Sprite sprite)
    {
        if (sprite == null) return true;
        switch (sprite.name)
        {
            case "PopupButton9Slice":
            case "ButtonCreateActive":
            case "Button_02":
            case "buttonBuy":
                return true;
            default:
                return false;
        }
    }
}
#endif
