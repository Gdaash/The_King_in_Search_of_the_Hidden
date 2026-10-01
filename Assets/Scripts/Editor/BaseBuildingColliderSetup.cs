#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GameFoundation.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class BaseBuildingColliderSetup
{
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        const string hudPath = "Assets/Prefabs/UI/Screens/Base Screen HUD.prefab";
        const string artPath = "Assets/Art/BaseScene/Base Scene Artwork.prefab";
        var names = new HashSet<string>();
        var hud = PrefabUtility.LoadPrefabContents(hudPath);
        try
        {
            foreach (var view in hud.GetComponentsInChildren<WorldBuildingButton>(true))
            {
                var so = new SerializedObject(view);
                var layers = so.FindProperty("artworkLayers");
                for (int i = 0; i < layers.arraySize; i++) names.Add(layers.GetArrayElementAtIndex(i).stringValue);
                var image = view.GetComponent<Image>(); image.raycastTarget = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                if (view.name != "Refugees") continue;
                var indicator = view.GetComponent<BuildingActionAvailabilityIndicator>() ?? view.gameObject.AddComponent<BuildingActionAvailabilityIndicator>();
                var data = new SerializedObject(indicator);
                var marker = data.FindProperty("marker").objectReferenceValue as GameObject;
                if (marker == null) marker = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Action Available Indicator.prefab"), view.transform);
                var rect = marker.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                rect.anchoredPosition = new Vector2(-4, -4);
                data.FindProperty("marker").objectReferenceValue = marker;
                data.FindProperty("actionType").enumValueIndex = (int)BuildingActionAvailabilityIndicator.ActionType.RefugeeAdmission;
                data.FindProperty("construction").objectReferenceValue = view.GetComponent<BaseBuildingConstruction>();
                data.ApplyModifiedPropertiesWithoutUndo();
                so.FindProperty("actionMarker").objectReferenceValue = rect; so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            }
            PrefabUtility.SaveAsPrefabAsset(hud, hudPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(hud); }
        var art = PrefabUtility.LoadPrefabContents(artPath);
        try
        {
            if (!art.TryGetComponent<WorldBuildingRaycaster>(out _)) art.AddComponent<WorldBuildingRaycaster>();
            foreach (var sprite in art.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!names.Contains(sprite.name) || sprite.name.EndsWith("Glow")) continue;
                sprite.gameObject.layer = 2; // Ignore ordinary camera/physics raycasts; handled by our Base raycaster.
                var collider = sprite.GetComponent<PolygonCollider2D>();
                if (collider == null)
                {
                    collider = sprite.gameObject.AddComponent<PolygonCollider2D>();
                    collider.pathCount = 1; collider.SetPath(0, Outline(sprite.sprite));
                }
                collider.isTrigger = true;
                if (!sprite.TryGetComponent<WorldBuildingHitArea>(out _)) sprite.gameObject.AddComponent<WorldBuildingHitArea>();
            }
            PrefabUtility.SaveAsPrefabAsset(art, artPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(art); }
        AssetDatabase.SaveAssets();
        foreach (var raycaster in Object.FindObjectsByType<Physics2DRaycaster>(FindObjectsSortMode.None))
        {
            raycaster.eventMask &= ~(1 << 2);
            PrefabUtility.RecordPrefabInstancePropertyModifications(raycaster);
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("Building collider prefabs and refugee availability badge saved.");
    }

    // Convex silhouette has stable, editable edges without tiny pixel-sized holes.
    private static Vector2[] Outline(Sprite sprite)
    {
        var points = new List<Vector2>();
        Rect r = sprite.rect;
        for (int y = 0; y < (int)r.height; y++)
        {
            int first = -1, last = -1;
            for (int x = 0; x < (int)r.width; x++)
                if (sprite.texture.GetPixel((int)r.x + x, (int)r.y + y).a > .1f) { if (first < 0) first = x; last = x; }
            if (first < 0) continue;
            points.Add(new Vector2(first, y)); points.Add(new Vector2(first, y + 1));
            points.Add(new Vector2(last + 1, y)); points.Add(new Vector2(last + 1, y + 1));
        }
        points = points.Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToList();
        var hull = new List<Vector2>();
        foreach (var p in points) { while (hull.Count >= 2 && Cross(hull[^1] - hull[^2], p - hull[^1]) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
        int lower = hull.Count;
        for (int i = points.Count - 2; i >= 0; i--) { var p = points[i]; while (hull.Count > lower && Cross(hull[^1] - hull[^2], p - hull[^1]) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
        hull.RemoveAt(hull.Count - 1);
        return hull.Select(p => (p - sprite.pivot) / sprite.pixelsPerUnit).ToArray();
    }
    private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
}
#endif
