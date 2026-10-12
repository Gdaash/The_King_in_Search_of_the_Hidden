#if UNITY_EDITOR
using GameFoundation.Base;
using GameFoundation.UI;
using GameFoundation.Localization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class UnitStatsWindowSizeSetup
{
    static void Configure(GameObject root)
    {
        foreach (var card in root.GetComponentsInChildren<UnitDescriptionView>(true))
        {
            var rect = (RectTransform)card.transform;
            if (rect.anchorMin == rect.anchorMax)
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 840);
            var scroll = card.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (!scroll) continue;
            scroll.vertical = false;
            scroll.horizontal = false;
            scroll.StopMovement();
            if (scroll.verticalScrollbar) scroll.verticalScrollbar.gameObject.SetActive(false);
            scroll.verticalScrollbar = null;
            scroll.viewport.offsetMax = new Vector2(0, scroll.viewport.offsetMax.y);
            PrefabUtility.RecordPrefabInstancePropertyModifications(scroll);
            PrefabUtility.RecordPrefabInstancePropertyModifications(scroll.viewport);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
        foreach (var tooltip in root.GetComponentsInChildren<UnitDescriptionTooltip>(true))
        {
            var serialized = new SerializedObject(tooltip);
            serialized.FindProperty("preferredSize").vector2Value = new Vector2(640, 840);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            ((RectTransform)tooltip.transform).sizeDelta = new Vector2(640, 840);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tooltip);
            PrefabUtility.RecordPrefabInstancePropertyModifications(tooltip.transform);
        }
        foreach (var training in root.GetComponentsInChildren<MilitaryTrainingView>(true))
        {
            var window = training.transform.Find("Window") as RectTransform;
            if (!window) continue;
            window.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 960);
            var frame = window.Find("Artwork Frame") as RectTransform;
            if (frame) frame.sizeDelta = new Vector2(1120, 960);
            var title = window.Find("Title") as RectTransform;
            if (title) title.anchoredPosition = new Vector2(0, 428);
            var close = window.Find("Close") as RectTransform;
            if (close) close.anchoredPosition = new Vector2(548, 468);
            var upgrade = window.Find("Upgrade") as RectTransform;
            if (upgrade) upgrade.anchoredPosition = new Vector2(0, -518);
            foreach (var rect in new[] { window, frame, title, close, upgrade })
                if (rect) PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }
    }

    [MenuItem("Tools/UI/Expand troop characteristics windows")]
    public static void Apply()
    {
        // Uses the small withdrawal sprite, preserving the original vampirism artwork separately.
        UIKitTroopStatsSetup.Apply();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset.GetComponentInChildren<UnitDescriptionView>(true)) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Configure(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        string original = EditorSceneManager.GetActiveScene().path;
        EditorSceneManager.SaveOpenScenes();
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var root in scene.GetRootGameObjects()) Configure(root);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
        var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>("Assets/Resources/Localization/Base Localization.asset");
        var entry = table.entries.Find(e => e.key == "unit.details.footer_compact");
        if (entry == null) { entry = new LocalizationTable.Entry { key = "unit.details.footer_compact" }; table.entries.Add(entry); }
        while (entry.values.Count < table.languages.Count) entry.values.Add("");
        entry.values[table.languages.IndexOf("ru")] = "За убийство: {0} опыта · за помощь: {1}\nНовый день полностью восстанавливает здоровье.";
        entry.values[table.languages.IndexOf("en")] = "Kill: {0} XP · assist: {1}\nA new day fully restores health.";
        EditorUtility.SetDirty(table); AssetDatabase.SaveAssets();
        Debug.Log("Troop cards expanded to 840, training windows to 960; scrolling disabled.");
    }
}
#endif
