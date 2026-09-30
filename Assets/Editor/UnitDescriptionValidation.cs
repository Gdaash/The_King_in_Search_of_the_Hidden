#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using GameFoundation.UI;
using GameFoundation.MetaProgression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class UnitDescriptionValidation
{
    private const string PlayScene = "Assets/UnitDescriptionPreview.unity";
    public static void BeginPlaytest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var canvasGo = new GameObject("Unit UI QA", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule)); SceneManager.MoveGameObjectToScene(events, scene);
            var language = new GameObject("Localization", typeof(GameFoundation.Localization.LocalizationService)); SceneManager.MoveGameObjectToScene(language, scene);
            var languageSO = new SerializedObject(language.GetComponent<GameFoundation.Localization.LocalizationService>()); languageSO.FindProperty("table").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameFoundation.Localization.LocalizationTable>("Assets/Resources/Localization/Base Localization.asset"); languageSO.ApplyModifiedPropertiesWithoutUndo();
            foreach (string path in new[]{"Assets/Prefabs/Base/Fort Popup.prefab","Assets/Prefabs/Base/Archery Range Popup.prefab"})
            {
                var popup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), canvasGo.transform); popup.SetActive(path.Contains("Fort"));
            }
            var roster = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HUD/Base Military Overview.prefab"),canvasGo.transform);
            foreach (var controller in roster.GetComponents<MonoBehaviour>()) controller.enabled = false;
            var template = roster.GetComponentInChildren<WorldMilitaryRosterItemView>(true);
            var item = Object.Instantiate(template, template.transform.parent); item.name = "QA Warrior"; item.gameObject.SetActive(true);
            var so = new SerializedObject(item); so.FindProperty("tooltipDelay").floatValue = 0; so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, PlayScene);
        }
        finally { EditorSceneManager.CloseScene(scene,true); }
        SessionState.SetString("UnitDescriptionPreviousStartScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PlayScene);
        EditorApplication.isPlaying = true;
    }

    public static void EndPlaytest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode before cleanup.");
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("UnitDescriptionPreviousStartScene", ""));
        AssetDatabase.DeleteAsset(PlayScene);
    }

    public static void Capture(string prefabPath, string output, bool veteran = false, int width = 1280, int height = 1000)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture render = null;
        Texture2D pixels = null;
        try
        {
            var cameraGo = new GameObject("Unit UI preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var camera = cameraGo.GetComponent<Camera>(); camera.scene = scene; camera.enabled = false; camera.orthographic = true; camera.orthographicSize = height / 2f;
            camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .038f, .055f); camera.cullingMask = 1 << 30;
            render = new RenderTexture(width, height, 24); camera.targetTexture = render;
            var canvasGo = new GameObject("Unit UI preview", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.pixelPerfect = true;
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), canvasGo.transform); root.SetActive(true);
            foreach (var t in canvasGo.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            foreach (var group in root.GetComponentsInChildren<CanvasGroup>(true)) group.alpha = 1;
            foreach (var view in root.GetComponentsInChildren<UnitDescriptionView>(true))
                view.Show(view.Definition, veteran ? new MilitaryProfile { type = "Swordsman", experience = 325, healthPercent = .08f } : null);
            Canvas.ForceUpdateCanvases();
            foreach (var fit in root.GetComponentsInChildren<CanvasWindowFit>(true)) fit.Fit();
            foreach (var layout in root.GetComponentsInChildren<UnityEngine.UI.VerticalLayoutGroup>(true)) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture previous = RenderTexture.active; RenderTexture.active = render;
            pixels = new Texture2D(width, height, TextureFormat.RGBA32, false); pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply(); RenderTexture.active = previous;
            Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllBytes(output, pixels.EncodeToPNG());
            foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                if (text.gameObject.activeInHierarchy && !string.IsNullOrEmpty(text.text) && text.preferredHeight > text.rectTransform.rect.height + 2)
                    Debug.LogWarning("Unit description text may clip: " + text.name + " text=" + text.text + " preferred=" + text.preferredHeight + " height=" + text.rectTransform.rect.height);
            Debug.Log("Saved unit UI preview: " + output);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (pixels != null) Object.DestroyImmediate(pixels);
            if (render != null) { render.Release(); Object.DestroyImmediate(render); }
        }
    }

    public static void VerifyValues()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UnitDescriptionSetup.CardPath);
        var root = Object.Instantiate(prefab); root.SetActive(false);
        try
        {
            var view = root.GetComponent<UnitDescriptionView>();
            var sword = AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(UnitDescriptionSetup.Data + "/Swordsman Description.asset");
            var archer = AssetDatabase.LoadAssetAtPath<UnitDescriptionDefinition>(UnitDescriptionSetup.Data + "/Archer Description.asset");
            view.Show(sword);
            Check(root, "Здоровье", "100 / 100"); Check(root, "Физический урон", "10"); Check(root, "Частота атак", "0.67 / с");
            view.Show(archer);
            Check(root, "Здоровье", "40 / 40"); Check(root, "Физический урон", "15"); Check(root, "Дальность атаки", "4 ед.");
            view.Show(sword, new MilitaryProfile { experience = 325, healthPercent = .08f });
            Check(root, "Здоровье", "16 / 200"); Check(root, "Физический урон", "20"); Check(root, "Частота атак", "1.33 / с");
            int count = root.GetComponentsInChildren<UnitStatRowView>(true).Length;
            for (int i = 0; i < 10; i++) { view.Show(archer); view.Show(sword); }
            if (root.GetComponentsInChildren<UnitStatRowView>(true).Length != count) throw new Exception("Rows accumulate when switching unit types.");
            Debug.Log("Unit descriptions verified: recruit values for both types; wounded level 10 health, damage and faster attack interval; reusable rows after 20 switches.");
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Check(GameObject root, string caption, string expected)
    {
        foreach (var row in root.GetComponentsInChildren<UnitStatRowView>(true))
        {
            if (!row.gameObject.activeSelf) continue;
            var label = row.transform.Find("Label").GetComponent<UnityEngine.UI.Text>();
            if (label.text != caption) continue;
            string actual = row.transform.Find("Value").GetComponent<UnityEngine.UI.Text>().text;
            if (actual != expected) throw new Exception(caption + ": expected " + expected + ", got " + actual);
            return;
        }
        throw new Exception("Missing stat row: " + caption);
    }
}
#endif
