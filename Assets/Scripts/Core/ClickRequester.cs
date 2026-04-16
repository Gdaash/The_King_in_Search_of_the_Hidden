using UnityEngine;
using System.Linq;

public class ClickRequester : ResourceRequester
{
    [Header("Настройки клика")]
    [SerializeField] private Sprite mouseSprite; // Сюда перетащите спрайт мышки в инспекторе

    // Полностью заменяем метод включения скрипта
    protected override void OnEnable()
    {
        // Не добавляем в список AllRequesters, чтобы боты-носильщики не пытались сюда прийти
        if (iconsContainer != null) _containerBasePos = iconsContainer.localPosition;
        UpdateIndicator();
    }

    protected override void OnDisable()
    {
        // Базовый метод не вызываем
    }

    // Переписываем индикатор: теперь он рисует только мышки
    public override void UpdateIndicator()
    {
        if (iconsContainer == null || iconPrefab == null) return;

        // Очистка старых иконок
        foreach (var icon in _activeIcons) Destroy(icon);
        _activeIcons.Clear();

        // Если производство запущено — скрываем всё
        if (_isProcessing)
        {
            iconsContainer.gameObject.SetActive(false);
            return;
        }

        // Считаем сколько кликов осталось (сумма всех RequiredAmount в списке рецепта)
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

        // Создаем иконки мыши
        float totalWidth = (totalClicksNeeded - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < totalClicksNeeded; i++)
        {
            GameObject newIcon = Instantiate(iconPrefab, iconsContainer);
            newIcon.transform.localPosition = new Vector3(startX + (i * iconSpacing), 0, 0);
            
            // Устанавливаем спрайт мыши вместо ресурса
            if (newIcon.TryGetComponent(out SpriteRenderer sr))
            {
                sr.sprite = mouseSprite;
            }
            _activeIcons.Add(newIcon);
        }
    }

    // Логика клика
    private void OnMouseDown()
    {
        if (_isProcessing) return;

        // Ищем любое требование, которое еще не заполнено
        var req = requirements.FirstOrDefault(r => r.currentAmount < r.requiredAmount);
        
        if (req != null)
        {
            req.currentAmount++;
            OnResourceReceived?.Invoke();
            
            // Проверяем, не пора ли запускать производство
            CheckCompletion();
            // Обновляем количество мышек над головой
            UpdateIndicator();
        }
    }
}