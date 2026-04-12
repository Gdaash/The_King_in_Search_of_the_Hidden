using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance;
    
    [Header("Настройки сетки")]
    [SerializeField] private float gridSize = 1.0f;
    [SerializeField] private bool useGrid = true;

    [Header("Текущий объект")]
    [SerializeField] private GameObject _previewObject; 
    private PlacementValidator _validator;

    void Awake() 
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SetBlueprint(GameObject prefab)
    {
        if (prefab == null) return;
        
        // Если уже что-то выбрали, удаляем старый фантом перед созданием нового
        if (_previewObject != null) Destroy(_previewObject);

        _previewObject = Instantiate(prefab);
        _validator = _previewObject.GetComponent<PlacementValidator>();
        
        // 1. Получаем ссылки на компоненты
        ConstructionSite site = _previewObject.GetComponent<ConstructionSite>();
        ResourceRequester req = _previewObject.GetComponent<ResourceRequester>();

        // 2. Выключаем их, чтобы чертеж не работал, пока он на мышке
        if (site != null) 
        {
            site.isPlaced = false; 
            site.enabled = false;
        }

        if (req != null) 
        {
            req.enabled = false;
        }

        UpdatePreviewPosition(); 
        Debug.Log("BuildingPlacer: Фантом создан. Логика стройки и запросов временно выключена.");
    }

    void Update()
    {
        if (_previewObject == null) return;

        UpdatePreviewPosition();

        // Отмена на Правую Кнопку Мыши
        if (Input.GetMouseButtonDown(1)) {
            Destroy(_previewObject);
            _previewObject = null;
            return;
        }

        // Установка на Левую Кнопку Мыши
        if (Input.GetMouseButtonDown(0)) {
            if (_validator == null || _validator.IsValid) {
                PlaceBuilding();
            } else {
                Debug.Log("BuildingPlacer: Место занято!");
            }
        }
    }

    private void UpdatePreviewPosition()
    {
        if (_previewObject == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        if (useGrid)
        {
            mousePos.x = Mathf.Round(mousePos.x / gridSize) * gridSize;
            mousePos.y = Mathf.Round(mousePos.y / gridSize) * gridSize;
        }

        _previewObject.transform.position = mousePos;
    }

    private void PlaceBuilding()
    {
        if (_previewObject == null) return;

        ConstructionSite site = _previewObject.GetComponent<ConstructionSite>();
        ResourceRequester req = _previewObject.GetComponent<ResourceRequester>();

        // 1. Активируем стройку
        if (site != null) 
        {
            site.isPlaced = true; 
            site.enabled = true;  
            site.SetupLogic(); 
        }

        // 2. Активируем заказчика ресурсов (иконка появится и носильщики увидят цель)
        if (req != null)
        {
            req.enabled = true;
        }

        // Возвращаем нормальный цвет
        if (_previewObject.TryGetComponent(out SpriteRenderer sr)) 
            sr.color = Color.white;

        // Удаляем скрипт проверки места
        if (_previewObject.TryGetComponent(out PlacementValidator pv)) 
            Destroy(pv);

        Debug.Log("BuildingPlacer: Здание установлено на землю.");
        _previewObject = null; 
    }
}