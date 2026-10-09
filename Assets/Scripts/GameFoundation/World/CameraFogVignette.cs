using UnityEngine;

namespace GameFoundation.World
{
    /// <summary>Keeps a transparent fog vignette aligned with an orthographic camera viewport.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class CameraFogVignette : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer vignette;
        [SerializeField, Min(0.01f)] private float forwardOffset = 1f;
        [SerializeField] private Color tint = new Color(1f, 1f, 1f, 0.58f);

        private float lastOrthoSize = -1f;
        private float lastAspect = -1f;
        private Sprite lastSprite;

        private void OnEnable() => RefreshLayout();

        private void LateUpdate() => UpdateLayout(false);

        public void RefreshLayout() => UpdateLayout(true);

        private void UpdateLayout(bool force)
        {
            if (targetCamera == null || vignette == null || vignette.sprite == null || !targetCamera.orthographic)
                return;

            float orthoSize = targetCamera.orthographicSize;
            float aspect = targetCamera.aspect;
            if (!force && Mathf.Approximately(lastOrthoSize, orthoSize) &&
                Mathf.Approximately(lastAspect, aspect) && lastSprite == vignette.sprite)
                return;

            Transform vignetteTransform = vignette.transform;
            vignetteTransform.localPosition = new Vector3(0f, 0f, forwardOffset);
            vignetteTransform.localRotation = Quaternion.identity;

            Bounds spriteBounds = vignette.sprite.bounds;
            float viewHeight = orthoSize * 2f;
            float viewWidth = viewHeight * aspect;
            vignetteTransform.localScale = new Vector3(
                viewWidth / spriteBounds.size.x,
                viewHeight / spriteBounds.size.y,
                1f);
            vignette.color = tint;

            lastOrthoSize = orthoSize;
            lastAspect = aspect;
            lastSprite = vignette.sprite;
        }
    }
}
