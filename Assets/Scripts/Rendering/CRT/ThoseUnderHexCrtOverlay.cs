using UnityEngine;
using UnityEngine.UI;

/// <summary>Places scanlines and the CRT vignette above Screen Space Overlay UI.</summary>
public static class ThoseUnderHexCrtOverlay
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateOverlay()
    {
        if (Object.FindFirstObjectByType<ThoseUnderHexCrtOverlayMarker>() != null)
            return;

        Material material = ProjectReferences.Instance != null ? ProjectReferences.Instance.crtOverlayMaterial : null;
        if (material == null)
            return;

        var root = new GameObject("CRT Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ThoseUnderHexCrtOverlayMarker));
        Object.DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        root.GetComponent<GraphicRaycaster>().enabled = false;

        var imageObject = new GameObject("Scanlines", typeof(RectTransform), typeof(RawImage));
        imageObject.transform.SetParent(root.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RawImage image = imageObject.GetComponent<RawImage>();
        image.material = material;
        image.raycastTarget = false;
    }
}

internal sealed class ThoseUnderHexCrtOverlayMarker : MonoBehaviour
{
    private Canvas canvas;

    private void Awake() => canvas = GetComponent<Canvas>();

    private void Update()
    {
        if (canvas != null)
            canvas.enabled = ThoseUnderHexCrtSettings.IsEnabled;
    }
}
