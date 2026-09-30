#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using GameFoundation.Bestiary;
using GameFoundation.Localization;
using GameFoundation.Saves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Temporary isolated verification with an exact backup of the active slot envelope.</summary>
public static class MagicLibraryValidation
{
    private const string ScenePath = "Assets/MagicLibraryPreview.unity";
    private const string BackupKey = "MagicLibraryValidation.Save";
    private const string BackupExistsKey = "MagicLibraryValidation.Exists";

    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode first.");
        string saveKey = "foundation.slot." + SaveSlotPrefs.SelectedSlot + ".saveData";
        SessionState.SetBool(BackupExistsKey, PlayerPrefs.HasKey(saveKey));
        SessionState.SetString(BackupKey, PlayerPrefs.GetString(saveKey, string.Empty));
        // The test starts with an empty slot envelope and restores this exact envelope in End.
        PlayerPrefs.DeleteKey(saveKey); PlayerPrefs.Save();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var canvasGo = new GameObject("Magic Library QA Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            SceneManager.MoveGameObjectToScene(events, scene);
            var localization = new GameObject("Localization", typeof(LocalizationService)); SceneManager.MoveGameObjectToScene(localization, scene);
            var localizationSO = new SerializedObject(localization.GetComponent<LocalizationService>());
            localizationSO.FindProperty("table").objectReferenceValue = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
            localizationSO.ApplyModifiedPropertiesWithoutUndo();
            var popup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(MagicLibrarySetup.PopupPath), canvasGo.transform);
            popup.SetActive(true);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
        SessionState.SetString("MagicLibraryValidation.Start", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorApplication.isPlaying = true;
    }

    public static void Verify()
    {
        var popup = Object.FindAnyObjectByType<MagicLibraryView>();
        Check(popup != null, "Magic Library popup did not load.");
        Check(popup.GetComponentsInChildren<BestiaryEntryView>(true).Length == 1, "Only the inactive entry template should exist before discovery.");
        var skeleton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Units/Skeleton.prefab");
        Check(skeleton != null, "Skeleton prefab is unavailable for validation.");
        var spawned = Object.Instantiate(skeleton);
        BestiaryService.RegisterSpawn(skeleton, spawned);
        Check(BestiaryService.HasEncountered(skeleton), "Spawning an enemy did not record the encounter.");
        Check(popup.GetComponentsInChildren<BestiaryEntryView>(true).Count(x => x.gameObject.activeSelf) == 1, "Discovered monster did not create one visible library entry.");
        string save = PlayerPrefs.GetString("foundation.slot." + SaveSlotPrefs.SelectedSlot + ".saveData", string.Empty);
        Check(save.Contains("Skeleton"), "Encounter was not written to the slot save.");
        popup.gameObject.SetActive(false); popup.gameObject.SetActive(true);
        Check(popup.GetComponentsInChildren<BestiaryEntryView>(true).Count(x => x.gameObject.activeSelf) == 1, "Reopening the library duplicated or lost the entry.");
        Debug.Log("PASS: Magic Library begins empty, records a World enemy, persists it and displays one reusable entry after reopening.");
    }

    public static void End()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stop Play Mode first.");
        string saveKey = "foundation.slot." + SaveSlotPrefs.SelectedSlot + ".saveData";
        if (SessionState.GetBool(BackupExistsKey, false)) PlayerPrefs.SetString(saveKey, SessionState.GetString(BackupKey, string.Empty));
        else PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        SessionState.EraseString(BackupKey); SessionState.EraseBool(BackupExistsKey);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString("MagicLibraryValidation.Start", ""));
        SessionState.EraseString("MagicLibraryValidation.Start");
        AssetDatabase.DeleteAsset(ScenePath);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
#endif
