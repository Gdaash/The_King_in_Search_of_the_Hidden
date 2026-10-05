using UnityEngine;
using UnityEngine.Rendering;

namespace GameFoundation.Base
{
    /// <summary>
    /// Keeps the authored base composition in view while rendering world-space UI
    /// at the display resolution, without a low-resolution intermediate texture.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class BaseSceneCameraFraming : MonoBehaviour
    {
        [Tooltip("Pixel dimensions of the base artwork. These control framing, not render resolution.")]
        [SerializeField] private Vector2Int referenceResolution = new(640, 480);
        [SerializeField, Min(1)] private int pixelsPerUnit = 32;

        private Camera targetCamera;

        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
            ApplyFraming();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        }

        private void OnValidate()
        {
            referenceResolution.x = Mathf.Max(1, referenceResolution.x);
            referenceResolution.y = Mathf.Max(1, referenceResolution.y);
            pixelsPerUnit = Mathf.Max(1, pixelsPerUnit);
        }

        private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == targetCamera)
                ApplyFraming();
        }

        private void ApplyFraming()
        {
            if (targetCamera == null)
                return;

            // The black camera background fills the space beyond the artwork.
            // A full viewport avoids the pixel-perfect camera's 640 x 480 buffer.
            var fullViewport = new Rect(0f, 0f, 1f, 1f);
            if (targetCamera.rect != fullViewport)
                targetCamera.rect = fullViewport;
            targetCamera.orthographic = true;

            float aspect = (float)Mathf.Max(1, targetCamera.pixelWidth) /
                           Mathf.Max(1, targetCamera.pixelHeight);
            float visiblePixelHeight = Mathf.Max(referenceResolution.y, referenceResolution.x / aspect);
            float size = visiblePixelHeight / (2f * pixelsPerUnit);
            if (!Mathf.Approximately(targetCamera.orthographicSize, size))
                targetCamera.orthographicSize = size;
        }
    }
}
