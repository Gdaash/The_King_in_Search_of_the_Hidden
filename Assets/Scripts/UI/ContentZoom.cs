using UnityEngine;

namespace Evolution_adventure.Scripts.UI
{
    public class ContentZoom : MonoBehaviour
    {
        [SerializeField] public RectTransform _content;
        
        [Header("Settings")]
        public float _zoomSpeed = 0.1f;
        public float _minZoom = 0.5f;
        public float _maxZoom = 2f;
        public float _startZoom = 1f;

        private void Start()
        {
            if (_content == null) return;

            // 1. Устанавливаем масштаб
            float clampedStartZoom = Mathf.Clamp(_startZoom, _minZoom, _maxZoom);
            _content.localScale = Vector3.one * clampedStartZoom;

            // 2. Центрируем объект относительно его родителя
            CenterOnObject();
        }

        public void CenterOnObject()
        {
            RectTransform viewport = _content.parent as RectTransform;
            if (viewport == null) return;

            // Получаем мировые углы контента
            Vector3[] contentCorners = new Vector3[4];
            _content.GetWorldCorners(contentCorners);
            // Геометрический центр контента в мировых координатах
            Vector3 contentWorldCenter = (contentCorners[0] + contentCorners[2]) / 2f;

            // Получаем мировые углы видимой области (viewport)
            Vector3[] viewportCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            // Геометрический центр видимой области в мировых координатах
            Vector3 viewportWorldCenter = (viewportCorners[0] + viewportCorners[2]) / 2f;

            // Находим разницу между центрами
            Vector3 offset = viewportWorldCenter - contentWorldCenter;

            // Двигаем контент в мировом пространстве на величину смещения
            _content.position += offset;
        }

        void Update()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll == 0) return;

            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _content,
                Input.mousePosition,
                null,
                out localMousePos
            );

            float scale = _content.localScale.x;
            float newScale = Mathf.Clamp(scale + scroll * _zoomSpeed, _minZoom, _maxZoom);

            if (scale > 0)
            {
                float scaleFactor = newScale / scale;
                _content.localScale = Vector3.one * newScale;
                _content.anchoredPosition -= localMousePos * (scaleFactor - 1f);
            }
        }
    }
}
