using System;
using DeskCat.FindIt.Scripts.Core.Main.System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems; // Required for pointer events

namespace DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj
{
    [Serializable]
    public class DragAndDropEvent3D : UnityEvent<DragObj>
    {
    }

    public class DragObj : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
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
            // Ensure component dependencies and initialize them
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("DragObj: Main Camera not found! Ensure there is a camera tagged 'MainCamera'.");
                enabled = false;
                return;
            }

            _hiddenObj = GetComponent<HiddenObj>();
            if (_hiddenObj == null)
            {
                Debug.LogError("DragObj: Missing HiddenObj component on this GameObject.");
                enabled = false;
                return;
            }

            _collider = GetComponent<BoxCollider2D>();
            if (_collider == null)
            {
                Debug.LogError("DragObj: Missing BoxCollider2D component on this GameObject.");
                enabled = false;
                return;
            }

            _originalPosition = transform.position;
        }

        private void OnEnable()
        {
            // Ensure the collider is enabled when the object becomes active
            if (_collider != null)
            {
                _collider.enabled = true;
            }
        }

        // Called when the pointer is pressed down
        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_hiddenObj.isAbleToClick || _hiddenObj.IsFound) return;

            // Cache the mouse click offset
            _mOffset = gameObject.transform.position - CalculateWorldPoint();
            _originalPosition = transform.position;

            // Trigger drag start event
            onBeginDrag?.Invoke(this);

            // Disable camera controls
            CameraView2D.SetEnablePanAndZoom(false);
            CameraView3D.SetEnableOrbit(false);

            _isDragging = true;
        }

        // Called while dragging (pointer is moving)
        public void OnDrag(PointerEventData eventData)
        {
            if (!_hiddenObj.isAbleToClick || _hiddenObj.IsFound) return;

            // Temporarily disable collider if required
            if (!EnableCollisionWhenDrag && !_colliderWasDisabled)
            {
                _collider.enabled = false;
                _colliderWasDisabled = true; // Mark as disabled
            }

            // Update the object position while dragging
            transform.position = CalculateWorldPoint() + _mOffset;
            FreezePositionOnDrag();

            // Trigger drag event
            onDrag?.Invoke(this);

            // If the object is dragged to a valid drop region
            if (CurrentDragInfo.CurrentDropRegion != null &&
                CurrentDragInfo.CurrentDropRegion.RegionName == DropRegionName)
            {
                onDragToRegion?.Invoke(this);

                if (DragToRegionToFound)
                {
                    _hiddenObj.DragRegionAction?.Invoke();
                }
            }
        }

        // Called when the pointer is released
        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragging) return;

            // Re-enable the collider if it was disabled
            if (_colliderWasDisabled)
            {
                _collider.enabled = true;
                _colliderWasDisabled = false;
            }

            // Trigger drag end event
            onEndDrag?.Invoke(this);

            // Re-enable camera controls
            CameraView2D.SetEnablePanAndZoom(true);
            CameraView3D.SetEnableOrbit(true);

            // Perform drop region checks
            DropRegionCheck();

            // Optionally reset object position
            if (IsReturnToOriginalPosition)
            {
                transform.position = _originalPosition;
            }

            _isDragging = false;
        }

        private void DropRegionCheck()
        {
            if (CurrentDragInfo.CurrentDropRegion == null)
            {
                //  Debug.Log("No drop region detected.");
                return;
            }

            if (CurrentDragInfo.CurrentDropRegion.RegionName != DropRegionName)
            {
                //  Debug.Log($"Drop region mismatch. Expected: {DropRegionName}, " +
                //            $"Found: {CurrentDragInfo.CurrentDropRegion.RegionName}");
                return;
            }

            // Hide the object if dropped in the correct region
            if (HideWhenDropToRegion)
            {
                gameObject.SetActive(false);
            }

            // Trigger target region's actions
            if (IsThisRegionAsTarget)
            {
                _hiddenObj.DragRegionAction?.Invoke();
            }

            // Invoke drop events
            onDropRegion?.Invoke(this);
            CurrentDragInfo.CurrentDropRegion.DropRegionEvent?.Invoke();
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

            // Freeze position axes if specified
            if (freezeX) position.x = _originalPosition.x;
            if (freezeY) position.y = _originalPosition.y;
            if (freezeZ) position.z = _originalPosition.z;

            transform.position = position;
        }
    }
}