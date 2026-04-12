using UnityEngine;

public class BuildButton : MonoBehaviour
{
    [Header("Настройки")]
    public GameObject blueprintPrefab; 

    public void SelectBuilding()
    {
        if (blueprintPrefab == null)
        {
            Debug.LogError($"[BuildButton] На кнопке {gameObject.name} не назначен префаб чертежа!");
            return;
        }

        // Передаем здание менеджеру размещения, но панель НЕ трогаем
        if (BuildingPlacer.Instance != null)
        {
            BuildingPlacer.Instance.SetBlueprint(blueprintPrefab);
            Debug.Log($"[BuildButton] Выбрано здание: {blueprintPrefab.name}. Панель оставлена открытой.");
        }
        else
        {
            Debug.LogError("[BuildButton] BuildingPlacer.Instance не найден на сцене!");
        }
    }
}
