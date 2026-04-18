using UnityEngine;
using System.Linq;

public class ClickRequester : ResourceRequester
{
    [Header("Настройки клика")]
    [SerializeField] private Sprite mouseSprite; 

    protected override void OnEnable()
    {
        // Базовый метод теперь не работает со списками, поэтому вызываем его смело
        base.OnEnable();
    }

    // Удалено 'override', так как в родителе метода больше нет
    private void OnDisable()
    {
        // Оставляем пустым или удаляем совсем
    }

    public override void UpdateIndicator()
    {
        if (iconsContainer == null || iconPrefab == null) return;

        // Очистка старых иконок (с проверкой на null для Unity 6)
        foreach (var icon in _activeIcons) if(icon) Destroy(icon);
        _activeIcons.Clear();

        if (_isProcessing || !gameObject.activeInHierarchy)
        {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        int totalClicksNeeded = 0;
        foreach (var req in requirements)
        {
            totalClicksNeeded += (req.requiredAmount - req.currentAmount);
        }

        if (totalClicksNeeded <= 0)
        {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        iconsContainer.gameObject.SetActive(true);

        float totalWidth = (totalClicksNeeded - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < totalClicksNeeded; i++)
        {
            GameObject newIcon = Instantiate(iconPrefab, iconsContainer);
            newIcon.transform.localPosition = new Vector3(startX + (i * iconSpacing), 0, 0);
            
            if (newIcon.TryGetComponent(out SpriteRenderer sr))
            {
                sr.sprite = mouseSprite;
            }
            _activeIcons.Add(newIcon);
        }
    }

    private void OnMouseDown()
    {
        if (_isProcessing) return;

        var req = requirements.FirstOrDefault(r => r.currentAmount < r.requiredAmount);
        
        if (req != null)
        {
            req.currentAmount++;
            
            // Вызываем события получения ресурсов
            OnResourceReceived?.Invoke();
            req.OnOneUnitDelivered?.Invoke();

            if (req.currentAmount >= req.requiredAmount)
            {
                req.OnAllUnitsDelivered?.Invoke();
            }
            
            CheckCompletion();
            UpdateIndicator();
        }
    }
}