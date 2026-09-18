using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using DeskCat.FindIt.Scripts.Core.Main.Utility.Region;

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj
{
    [Serializable]
    public class DragAndDropEvent3D : UnityEvent<DragObj> { }

    public class DragObj : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        // Глобальный флаг, чтобы камера или другие системы знали, что сейчас что-то тащат
        public static bool IsAnyObjectDragging { get; private set; }

        [Header("General Settings")] 
        [Tooltip("Имя региона, в который можно бросить этот объект")]
        public string DropRegionName = "";
        
        [Tooltip("Уничтожить объект при успешном сбросе в регион")]
        public bool DestroyWhenDropToRegion = true;
        
        [Tooltip("Отключать коллайдер во время перетаскивания (чтобы не мешал рейкастам)")]
        public bool DisableColliderWhenDrag = true;

        [Header("Drag Behavior")] 
        [Tooltip("Вызывать событие региона при перетаскивании над ним")]
        public bool TriggerEventsOnRegion = true;
        
        [Tooltip("Вернуть объект на исходную позицию, если он не был сброшен в регион")]
        public bool ReturnToOriginalPositionOnFail = true;

        [Header("Freeze Drag Axis")] 
        public bool freezeX;
        public bool freezeY;
        public bool freezeZ;

        [Header("Drag Events")] 
        public DragAndDropEvent3D onBeginDrag;
        public DragAndDropEvent3D onDrag;
        public DragAndDropEvent3D onDragToRegion;
        public DragAndDropEvent3D onEndDrag;

        [Header("Drop Events")] 
        public DragAndDropEvent3D onDropRegion;

        private Camera _mainCamera;
        private Vector3 _mOffset;
        private float _mZCoord;

        private Vector3 _originalPosition;
        private Collider2D _collider; // Теперь поддерживает ЛЮБОЙ 2D коллайдер

        private bool _isDragging;
        private bool _colliderWasDisabled;
        
        [Tooltip("Можно ли вообще перетаскивать этот объект (можно отключать программно)")]
        public bool CanDrag = true;

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("DragObj: Main Camera not found!");
                enabled = false;
                return;
            }

            // Пытаемся получить любой 2D коллайдер
            _collider = GetComponent<Collider2D>();
            _originalPosition = transform.position;
        }

        private void OnEnable()
        {
            if (_collider != null) _collider.enabled = true;
            _colliderWasDisabled = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanDrag) return;

            _mOffset = transform.position - CalculateWorldPoint();
            _originalPosition = transform.position;

            onBeginDrag?.Invoke(this);

            IsAnyObjectDragging = true;
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!CanDrag || !_isDragging) return;

            // Отключаем коллайдер во время перетаскивания, если нужно
            if (DisableColliderWhenDrag && _collider != null && !_colliderWasDisabled)
            {
                _collider.enabled = false;
                _colliderWasDisabled = true;
            }

            transform.position = CalculateWorldPoint() + _mOffset;
            FreezePositionOnDrag();

            onDrag?.Invoke(this);

            // Проверка нахождения над регионом во время перетаскивания
            if (TriggerEventsOnRegion && CurrentDragInfo.CurrentDropRegion != null)
            {
                bool isOverTarget = CurrentDragInfo.CurrentDropRegion.regions.Exists(r => r.isActive && r.regionName == DropRegionName);
                if (isOverTarget)
                {
                    onDragToRegion?.Invoke(this);
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;

            // Возвращаем коллайдер
            if (_colliderWasDisabled && _collider != null)
            {
                _collider.enabled = true;
                _colliderWasDisabled = false;
            }

            onEndDrag?.Invoke(this);
            IsAnyObjectDragging = false;

            DropRegionCheck();

            // Если объект не был уничтожен в DropRegionCheck, возвращаем его на место при необходимости
            if (this != null && ReturnToOriginalPositionOnFail)
            {
                transform.position = _originalPosition;
            }

            _isDragging = false;
        }

        private void DropRegionCheck()
        {
            if (CurrentDragInfo.CurrentDropRegion == null) return;

            bool hasValidRegion = CurrentDragInfo.CurrentDropRegion.regions.Exists(r => r.isActive && r.regionName == DropRegionName);
            if (!hasValidRegion) return;

            // Успешный сброс в регион
            onDropRegion?.Invoke(this);
            CurrentDragInfo.CurrentDropRegion.ExecuteRegionEvent(DropRegionName);

            if (DestroyWhenDropToRegion) 
            {
                Destroy(gameObject);
            }
        }

        private Vector3 CalculateWorldPoint()
        {
            _mZCoord = _mainCamera.WorldToScreenPoint(transform.position).z;
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