using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.Base
{
    public sealed class LaboratoryPanZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        [Header("Настраиваемая область взаимодействия")]
        [Tooltip("Область внутри попапа, которая принимает перетаскивание и колесо мыши.")]
        [SerializeField] private RectTransform interactionArea;
        [Tooltip("Ширина и высота области, в которой работают перетаскивание и колесо мыши.")]
        [SerializeField] private Vector2 interactionSize = new Vector2(1450f, 740f);
        [SerializeField] private RectTransform content;

        [Header("Масштаб")]
        [SerializeField, Min(0.1f)] private float minZoom = 0.65f;
        [SerializeField, Min(0.1f)] private float maxZoom = 2f;
        [SerializeField, Min(0.01f)] private float zoomStep = 0.12f;

        [Header("Перемещение")]
        [SerializeField, Min(0f)] private float keyboardSpeed = 520f;
        [SerializeField, Min(0f)] private float dragSensitivity = 1f;
        [SerializeField] private Vector2 extraPanRange = new Vector2(60f, 60f);

        private bool _dragging;
        private Vector2 _lastPointer;

        private void Awake()
        {
            ApplyInteractionSize();
        }

        private void OnEnable()
        {
            ApplyInteractionSize();
            ClampPosition();
        }

        private void OnValidate()
        {
            interactionSize = new Vector2(Mathf.Max(100f, interactionSize.x), Mathf.Max(100f, interactionSize.y));
            ApplyInteractionSize();
        }

        private void ApplyInteractionSize()
        {
            if (interactionArea != null) interactionArea.sizeDelta = interactionSize;
        }

        private void Update()
        {
            if (content == null || interactionArea == null) return;
            Vector2 direction = Vector2.zero;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) direction.x += 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) direction.x -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) direction.y -= 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) direction.y += 1f;
            if (direction.sqrMagnitude < 0.01f) return;
            content.anchoredPosition += direction.normalized * (keyboardSpeed * Time.unscaledDeltaTime);
            ClampPosition();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (content == null || interactionArea == null ||
                (eventData.button != PointerEventData.InputButton.Left && eventData.button != PointerEventData.InputButton.Middle)) return;
            _dragging = RectTransformUtility.RectangleContainsScreenPoint(interactionArea, eventData.position, eventData.pressEventCamera);
            if (_dragging) RectTransformUtility.ScreenPointToLocalPointInRectangle(interactionArea, eventData.position, eventData.pressEventCamera, out _lastPointer);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || content == null || interactionArea == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(interactionArea, eventData.position, eventData.pressEventCamera, out Vector2 pointer)) return;
            content.anchoredPosition += (pointer - _lastPointer) * dragSensitivity;
            _lastPointer = pointer;
            ClampPosition();
        }

        public void OnEndDrag(PointerEventData eventData) => _dragging = false;

        public void OnScroll(PointerEventData eventData)
        {
            if (content == null || interactionArea == null ||
                !RectTransformUtility.RectangleContainsScreenPoint(interactionArea, eventData.position, eventData.enterEventCamera)) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(interactionArea, eventData.position, eventData.enterEventCamera, out Vector2 pointer)) return;
            float oldScale = content.localScale.x;
            float newScale = Mathf.Clamp(oldScale * (1f + eventData.scrollDelta.y * zoomStep), minZoom, maxZoom);
            if (Mathf.Approximately(oldScale, newScale)) return;
            Vector2 pointInContent = (pointer - content.anchoredPosition) / oldScale;
            content.localScale = Vector3.one * newScale;
            content.anchoredPosition = pointer - pointInContent * newScale;
            ClampPosition();
            eventData.Use();
        }

        private void OnRectTransformDimensionsChange() => ClampPosition();

        private void ClampPosition()
        {
            if (content == null || interactionArea == null) return;
            float scale = content.localScale.x;
            float horizontal = Mathf.Max(0f, (content.rect.width * scale - interactionArea.rect.width) * .5f) + Mathf.Max(0f, extraPanRange.x);
            float vertical = Mathf.Max(0f, (content.rect.height * scale - interactionArea.rect.height) * .5f) + Mathf.Max(0f, extraPanRange.y);
            Vector2 position = content.anchoredPosition;
            content.anchoredPosition = new Vector2(Mathf.Clamp(position.x, -horizontal, horizontal), Mathf.Clamp(position.y, -vertical, vertical));
        }
    }
}
