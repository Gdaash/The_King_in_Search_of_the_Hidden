using UnityEngine;

namespace Evolution_adventure.Scripts.UI
{
    /// <summary>
    /// Зумит контент ScrollView
    /// </summary>
    public class ContentZoom : MonoBehaviour
    {
        [SerializeField] public RectTransform _content;
        
        public float _zoomSpeed = 0.1f;
        public float _minZoom = 0.5f;
        public float _maxZoom = 2f;

        void Update()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (scroll == 0)
            {
                return;
            }

            Vector2 localMousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _content,
                Input.mousePosition,
                null,
                out localMousePos
            );

            float scale = _content.localScale.x;
            float newScale = Mathf.Clamp(scale + scroll * _zoomSpeed, _minZoom, _maxZoom);

            float scaleFactor = newScale / scale;

            _content.localScale = Vector3.one * newScale;

            _content.anchoredPosition -= localMousePos * (scaleFactor - 1f);
        }
    }
}