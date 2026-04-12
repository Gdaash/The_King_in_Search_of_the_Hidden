using System;
using DeskCat.FindIt.Scripts.Core.Main.Utility.DragObj; // Добавили пространство имен
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
        
        [Header("---Keyboard---")]
        [SerializeField] private float keyboardSpeed = 10f;

        private Camera _camera;
        private Vector3 _dragOrigin;
        public static CameraView2D instance { get; private set; }

        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public bool IsPanning;
        public bool StopCameraFunc;

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); }
            else { instance = this; }

            _camera = Camera.main;
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            ScaleOverflowCamera();
        }

        private void Update()
        {
            if (StopCameraFunc) return;
            
            PanCamera();
            KeyboardPan(); 
            ZoomCamera();

            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                ScaleOverflowCamera();
            }
        }

        private void KeyboardPan()
        {
            if (!_enablePan) return;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                float speedMultiplier = _camera.orthographicSize / zoomMax;
                Vector3 move = new Vector3(horizontal, vertical, 0) * (keyboardSpeed * speedMultiplier * Time.deltaTime);
                Vector3 targetPos = _camera.transform.position + move;

                _camera.transform.position = _infinitePan ? targetPos : ClampCamera(targetPos);
                IsPanning = true;
            }
            else if (!Input.GetMouseButton(0))
            {
                IsPanning = false;
            }
        }

        private void PanCamera()
        {
            if (!_enablePan) return;

            // БЛОКИРОВКА МЫШИ: Если мы тянем объект, мышиный Pan не работает
            if (DragObj.IsAnyObjectDragging) 
            {
                IsPanning = false;
                return; 
            }

            if (Input.GetMouseButtonDown(0))
            {
                _dragOrigin = _camera.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(0))
            {
                var dragDifference = _dragOrigin - _camera.ScreenToWorldPoint(Input.mousePosition);

                if (dragDifference.sqrMagnitude > Mathf.Epsilon) 
                {
                    Vector3 targetPos = _camera.transform.position + dragDifference;
                    _camera.transform.position = _infinitePan ? targetPos : ClampCamera(targetPos);
                    IsPanning = true; 
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.1f && Mathf.Abs(Input.GetAxisRaw("Vertical")) < 0.1f)
                {
                    IsPanning = false; 
                }
            }
        }

        private void ZoomCamera()
        {
            if (!_enableZoom) return;
            float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(zoomDelta) > 0.01f) Zoom(zoomDelta);
            MobileTouchZoom();
            if (!_infinitePan) _camera.transform.position = ClampCamera(_camera.transform.position);
        }

        private void MobileTouchZoom()
        {
            if (Input.touchCount != 2) return;
            var touch0 = Input.GetTouch(0);
            var touch1 = Input.GetTouch(1);
            var prevMagnitude = ((touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition)).magnitude;
            var currentMagnitude = (touch0.position - touch1.position).magnitude;
            float diff = currentMagnitude - prevMagnitude;
            if (Mathf.Abs(diff) > 0.01f) Zoom(diff * 0.01f);
        }

        private void Zoom(float increment) => _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - increment, zoomMin, zoomMax);

        private void ScaleOverflowCamera()
        {
            if (_camera == null || backgroundSprite == null) return;
            float spriteWidth = backgroundSprite.sprite.textureRect.width;
            if (_camera.pixelWidth > spriteWidth)
            {
                float aspectOverrun = (_camera.aspect - 1.7f) / 0.4375f;
                zoomMax = Mathf.Max(zoomMax - aspectOverrun, zoomMin);
            }
        }

        private Vector3 ClampCamera(Vector3 targetPosition)
        {
            var orthographicSize = _camera.orthographicSize;
            var camWidth = orthographicSize * _camera.aspect;
            var pos = backgroundSprite.transform.position;
            var bounds = backgroundSprite.bounds;

            float minX, minY, maxX, maxY;

            if (_camera.orthographicSize < zoomMax + zoomPan && _autoPanBoundary)
            {
                minX = pos.x - bounds.size.x / 2f + camWidth;
                minY = pos.y - bounds.size.y / 2f + orthographicSize;
                maxX = pos.x + bounds.size.x / 2f - camWidth;
                maxY = pos.y + bounds.size.y / 2f - orthographicSize;
            }
            else
            {
                minX = _panMinX; minY = _panMinY; maxX = _panMaxX; maxY = _panMaxY;
            }

            return new Vector3(Mathf.Clamp(targetPosition.x, minX, maxX), Mathf.Clamp(targetPosition.y, minY, maxY), targetPosition.z);
        }

        public void SetStopCameraFunc(bool stopCameraFunc) => StopCameraFunc = stopCameraFunc;
        public static void SetEnablePanAndZoom(bool value) { if (instance) { instance._enablePan = value; instance._enableZoom = value; } }
    }
}