using UnityEngine;

public class CameraView2D : MonoBehaviour
{
    [Header("Границы карты")]
    [Tooltip("Спрайт фона, используемый для автоматического расчета границ камеры. Если не назначен, будут использованы ручные настройки ниже.")]
    public SpriteRenderer backgroundSprite;

    [Header("Настройки зума")]
    public bool enableZoom = true;
    public float zoomMin = 2f;
    public float zoomMax = 5.4f;
    public float zoomPan = 0f;

    [Header("Настройки перемещения")]
    public bool enablePan = true;
    public bool infinitePan = false;
    public bool autoPanBoundary = true;
    
    [Tooltip("Ручные границы, если autoPanBoundary выключен или нет backgroundSprite")]
    public float panMinX, panMinY;
    public float panMaxX, panMaxY;
    
    [Header("Управление с клавиатуры")]
    [SerializeField] private float keyboardSpeed = 10f;

    private Camera _camera;
    public static CameraView2D instance { get; private set; }

    // ВЕРНУТО: флаг состояния панорамирования для совместимости с другими скриптами
    public bool IsPanning;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    public bool StopCameraFunc;

    private void Awake()
    {
        if (instance != null && instance != this) 
        { 
            Destroy(gameObject); 
            return;
        }
        
        instance = this;
        _camera = Camera.main;
        
        if (_camera == null)
        {
            Debug.LogError("CameraView2D: Не найдена главная камера (Camera.main)!");
            return;
        }

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;
        
        ScaleOverflowCamera();
    }

    private void Update()
    {
        if (StopCameraFunc || _camera == null) return;
        
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
        if (!enablePan) 
        {
            IsPanning = false;
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
        {
            float speedMultiplier = _camera.orthographicSize / zoomMax;
            Vector3 move = new Vector3(horizontal, vertical, 0) * (keyboardSpeed * speedMultiplier * Time.deltaTime);
            Vector3 targetPos = _camera.transform.position + move;

            _camera.transform.position = infinitePan ? targetPos : ClampCamera(targetPos);
            IsPanning = true;
        }
        else
        {
            // Если клавиши не нажаты — камера не панорамируется
            IsPanning = false;
        }
    }

    private void ZoomCamera()
    {
        if (!enableZoom) return;
        
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(zoomDelta) > 0.01f) 
        {
            ApplyZoom(zoomDelta);
        }
        
        MobileTouchZoom();
        
        if (!infinitePan) 
        {
            _camera.transform.position = ClampCamera(_camera.transform.position);
        }
    }

    private void MobileTouchZoom()
    {
        if (Input.touchCount != 2) return;
        
        var touch0 = Input.GetTouch(0);
        var touch1 = Input.GetTouch(1);
        
        var prevMagnitude = ((touch0.position - touch0.deltaPosition) - (touch1.position - touch1.deltaPosition)).magnitude;
        var currentMagnitude = (touch0.position - touch1.position).magnitude;
        float diff = currentMagnitude - prevMagnitude;
        
        if (Mathf.Abs(diff) > 0.01f) 
        {
            ApplyZoom(diff * 0.01f);
        }
    }

    private void ApplyZoom(float increment) 
    {
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize - increment, zoomMin, zoomMax);
    }

    private void ScaleOverflowCamera()
    {
        if (_camera == null || backgroundSprite == null || backgroundSprite.sprite == null) return;
        
        float worldWidth = backgroundSprite.bounds.size.x;

        if (_camera.orthographicSize * _camera.aspect * 2f > worldWidth)
        {
            float aspectOverrun = (_camera.aspect - 1.7f) / 0.4375f;
            zoomMax = Mathf.Max(zoomMax - aspectOverrun, zoomMin);
            _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, zoomMin, zoomMax);
        }
    }

    private Vector3 ClampCamera(Vector3 targetPosition)
    {
        if (infinitePan) return targetPosition;

        float orthographicSize = _camera.orthographicSize;
        float camWidth = orthographicSize * _camera.aspect;
        
        float minX, minY, maxX, maxY;

        if (backgroundSprite != null && autoPanBoundary)
        {
            Vector3 pos = backgroundSprite.transform.position;
            Bounds bounds = backgroundSprite.bounds;

            minX = pos.x - bounds.size.x / 2f + camWidth;
            minY = pos.y - bounds.size.y / 2f + orthographicSize;
            maxX = pos.x + bounds.size.x / 2f - camWidth;
            maxY = pos.y + bounds.size.y / 2f - orthographicSize;
        }
        else
        {
            minX = panMinX; 
            minY = panMinY; 
            maxX = panMaxX; 
            maxY = panMaxY;
        }

        return new Vector3(
            Mathf.Clamp(targetPosition.x, minX, maxX), 
            Mathf.Clamp(targetPosition.y, minY, maxY), 
            targetPosition.z
        );
    }

    public void SetStopCameraFunc(bool stop) => StopCameraFunc = stop;
    
    public static void SetEnablePanAndZoom(bool value) 
    { 
        if (instance) 
        { 
            instance.enablePan = value; 
            instance.enableZoom = value; 
        } 
    }
}