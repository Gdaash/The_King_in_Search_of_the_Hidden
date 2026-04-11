using System;
using UnityEngine;

namespace DeskCat.FindIt.Scripts.Core.Main.System
{
    public class CameraView2D : MonoBehaviour
    {
        public SpriteRenderer backgroundSprite;

        [Header("---Zoom---")] public bool _enableZoom;
        public float zoomMin = 2f;
        public float zoomMax = 5.4f;
        public float zoomPan = 0f;

        [Header("---Pan---")] public bool _enablePan;
        public bool _infinitePan = false;
        public bool _autoPanBoundary = true;
        public float _panMinX, _panMinY;
        public float _panMaxX, _panMaxY;

        private Camera _camera;
        private Vector3 _dragOrigin;
        private Vector3 _touchStart;
        public static CameraView2D instance { get; private set; }

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public bool IsPanning;
        public bool StopCameraFunc;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
            }

            _camera = Camera.main;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            ScaleOverflowCamera();
        }

        private void Update()
        {
            if (StopCameraFunc) return;
            
            PanCamera();
            ZoomCamera();

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                ScaleOverflowCamera();
            }
        }

        public void SetStopCameraFunc(bool stopCameraFunc)
        {
            StopCameraFunc = stopCameraFunc;
        }

        public static void SetEnablePanAndZoom(bool value)
        {
            if (instance == null) return;
            instance._enablePan = value;
            instance._enableZoom = value;
        }

        private void ScaleOverflowCamera()
        {
            if (_camera == null || backgroundSprite == null)
                return;

            float spriteWidthInPixels = backgroundSprite.sprite.textureRect.width;
            float cameraWidthInPixels = _camera.pixelWidth;

            if (cameraWidthInPixels > spriteWidthInPixels)
            {
                float aspectOverrun = (_camera.aspect - 1.7f) / 0.4375f;
                zoomMax = Mathf.Max(zoomMax - aspectOverrun, zoomMin);
            }
        }

        private void PanCamera()
        {
            if (!_enablePan) return;

            if (Input.GetMouseButtonDown(0))
            {
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(0))
            {
                var dragDifference = _dragOrigin - _camera.ScreenToWorldPoint(Input.mousePosition);

                if (dragDifference.sqrMagnitude > Mathf.Epsilon) 
                {
                    if (_infinitePan)
                    {
                        _camera.transform.position += dragDifference;
                    }
                    else
                    {
                        _camera.transform.position = ClampCamera(_camera.transform.position + dragDifference);
                    }

                    IsPanning = true; 
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                IsPanning = false; 
            }
        }

        private void ZoomCamera()
        {
            if (!_enableZoom) return;

            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(zoomDelta) > 0.01f)
            {
                Zoom(zoomDelta);
            }

            MobileTouchZoom();

            if (!_infinitePan)
            {
                _camera.transform.position = ClampCamera(_camera.transform.position);
            }
        }

        private void MobileTouchZoom()
        {
            if (Input.touchCount != 2) return;

            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);

            var touch0PrevPos = touch0.position - touch0.deltaPosition;
            var touch1PrevPos = touch1.position - touch1.deltaPosition;

            var prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            var currentMagnitude = (touch0.position - touch1.position).magnitude;

            var touchDifference = currentMagnitude - prevMagnitude;

            if (Mathf.Abs(touchDifference) > 0.01f)
            {
                Zoom(touchDifference * 0.01f);
            }
        }

        private void Zoom(float increment)
        {
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - increment, zoomMin, zoomMax);
        }

        private Vector3 ClampCamera(Vector3 targetPosition)
        {
            var orthographicSize = _camera.orthographicSize;
            var camWidth = orthographicSize * _camera.aspect;

            if (_camera.orthographicSize < zoomMax + zoomPan && _autoPanBoundary)
            {
                var position = backgroundSprite.transform.position;
                var bounds = backgroundSprite.bounds;

                _panMinX = position.x - bounds.size.x / 2f;
                _panMinY = position.y - bounds.size.y / 2f;
                _panMaxX = position.x + bounds.size.x / 2f;
                _panMaxY = position.y + bounds.size.y / 2f;

                var minX = _panMinX + camWidth;
                var minY = _panMinY + orthographicSize;
                var maxX = _panMaxX - camWidth;
                var maxY = _panMaxY - orthographicSize;

                var clampX = Mathf.Clamp(targetPosition.x, minX, maxX);
                var clampY = Mathf.Clamp(targetPosition.y, minY, maxY);
                return new Vector3(clampX, clampY, targetPosition.z);
            }
            else
            {
                if (_autoPanBoundary)
                {
                    _panMinX = 0f;
                    _panMinY = -0.5f;
                    _panMaxX = 0;
                    _panMaxY = -0.5f;
                }

                var minX = _panMinX;
                var minY = _panMinY;
                var maxX = _panMaxX;
                var maxY = _panMaxY;

                var clampX = Mathf.Clamp(targetPosition.x, minX, maxX);
                var clampY = Mathf.Clamp(targetPosition.y, minY, maxY);
                return new Vector3(clampX, clampY, targetPosition.z);
            }
        }
    }
}