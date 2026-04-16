using System;
using DeskCat.FindIt.Scripts.Core.Main.System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DeskCat.FindIt.Scripts.Core.Main.Utility.Region; // Убедитесь, что этот namespace подключен

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj
{
    [Serializable]
    public class DragAndDropEvent3D : UnityEvent<DragObj>
    {
    }

    public class DragObj : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public static bool IsAnyObjectDragging { get; private set; }

        [Header("General Settings")] public string DropRegionName = "";
        public bool HideWhenDropToRegion = true;
        public bool EnableCollisionWhenDrag = false;

        [Header("Drag Behavior")] public bool IsThisRegionAsTarget = true;
        public bool DragToRegionToFound = false;
        public bool IsReturnToOriginalPosition = false;

        [Header("Freeze Drag Axis")] public bool freezeX;
        public bool freezeY;
        public bool freezeZ;

        [Header("Drag Events")] public DragAndDropEvent3D onBeginDrag;
        public DragAndDropEvent3D onDrag;
        public DragAndDropEvent3D onDragToRegion;
        public DragAndDropEvent3D onEndDrag;

        [Header("Drop Events")] public DragAndDropEvent3D onDropRegion;

        private Camera _mainCamera;
        private Vector3 _mOffset;
        private float _mZCoord;

        private Vector3 _originalPosition;
        private HiddenObj _hiddenObj;
        private BoxCollider2D _collider;

        private bool _isDragging;
        private bool _colliderWasDisabled;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("DragObj: Main Camera not found!");
                enabled = false;
                return;
            }

            _hiddenObj = GetComponent<HiddenObj>();
            _collider = GetComponent<BoxCollider2D>();
            _originalPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_collider != null) _collider.enabled = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_hiddenObj.isAbleToClick || _hiddenObj.IsFound) return;

            _mOffset = gameObject.transform.position - CalculateWorldPoint();
            _originalPosition = transform.position;

            onBeginDrag?.Invoke(this);

            IsAnyObjectDragging = true;
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_hiddenObj.isAbleToClick || _hiddenObj.IsFound) return;

            if (!EnableCollisionWhenDrag && !_colliderWasDisabled)
            {
                _collider.enabled = false;
                _colliderWasDisabled = true;
            }

            transform.position = CalculateWorldPoint() + _mOffset;
            FreezePositionOnDrag();

            onDrag?.Invoke(this);

            // ИСПРАВЛЕНО: Проверка через метод наличия нужного региона в списке
            if (CurrentDragInfo.CurrentDropRegion != null)
            {
                // Проверяем, есть ли в текущем регионе активная зона с нашим DropRegionName
                bool isOverTarget = CurrentDragInfo.CurrentDropRegion.regions.Exists(r => r.isActive && r.regionName == DropRegionName);
                
                if (isOverTarget)
                {
                    onDragToRegion?.Invoke(this);
                    if (DragToRegionToFound) _hiddenObj.DragRegionAction?.Invoke();
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;

            if (_colliderWasDisabled)
            {
                _collider.enabled = true;
                _colliderWasDisabled = false;
            }

            onEndDrag?.Invoke(this);
            IsAnyObjectDragging = false;

            DropRegionCheck();

            if (IsReturnToOriginalPosition)
            {
                transform.position = _originalPosition;
            }

            _isDragging = false;
        }

        private void DropRegionCheck()
        {
            if (CurrentDragInfo.CurrentDropRegion == null) return;

            // ИСПРАВЛЕНО: Проверяем наличие региона в списке
            bool hasValidRegion = CurrentDragInfo.CurrentDropRegion.regions.Exists(r => r.isActive && r.regionName == DropRegionName);
            
            if (!hasValidRegion) return;

            if (HideWhenDropToRegion) gameObject.SetActive(false);
            if (IsThisRegionAsTarget) _hiddenObj.DragRegionAction?.Invoke();

            onDropRegion?.Invoke(this);
            
            // ИСПРАВЛЕНО: Вызываем метод выполнения событий из списка
            CurrentDragInfo.CurrentDropRegion.ExecuteRegionEvent(DropRegionName);
        }

        private Vector3 CalculateWorldPoint()
        {
            _mZCoord = _mainCamera.WorldToScreenPoint(gameObject.transform.position).z;
            var mousePoint = Input.mousePosition;
            mousePoint.z = _mZCoord;
            return _mainCamera.ScreenToWorldPoint(mousePoint);
        }

        private void FreezePositionOnDrag()
        {
            var position = transform.position;
            if (freezeX) position.x = _originalPosition.x;
            if (freezeY) position.y = _originalPosition.y;
            if (freezeZ) position.z = _originalPosition.z;
            transform.position = position;
        }
    }
}