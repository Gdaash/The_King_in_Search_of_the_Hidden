using UnityEngine;
using System.Linq;

public class ClickRequester : ResourceRequester
{
    [Header("Настройки клика")]
    [SerializeField] private Sprite mouseSprite; 
    
    [Header("Авто-клик (Рабочий)")]
    [SerializeField] private float autoClickInterval = 1.0f; // Раз в сколько секунд кликает рабочий
    private float _nextAutoClickTime;

    protected override void OnEnable()
    {
        base.OnEnable();
    }

    protected override void Update()
    {
        base.Update(); // Сохраняем логику регенерации и прочего из родителя

        // Если здание активно, не в процессе производства и пришло время клика
        if (gameObject.activeInHierarchy && !_isProcessing && Time.time >= _nextAutoClickTime)
        {
            if (HasAutoClickWorker())
            {
                DoClick();
                _nextAutoClickTime = Time.time + autoClickInterval;
            }
        }
    }

    // Проверка наличия рабочего в зоне коллайдера
    private bool HasAutoClickWorker()
    {
        if (GetComponent<BoxCollider2D>() == null) return false;

        Vector2 searchSize = Vector2.Scale(GetComponent<BoxCollider2D>().size, transform.localScale);
        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, searchSize, 0);
        
        foreach (var hit in hits)
        {
            if (hit.GetComponent<AutoClickWorker>() != null) return true;
        }
        return false;
    }

    private void OnMouseDown()
    {
        if (_isProcessing) return;
        DoClick();
    }

    // Вынесли логику клика в отдельный метод, чтобы вызывали и мышь, и рабочий
    private void DoClick()
    {
        var req = requirements.FirstOrDefault(r => r.currentAmount < r.requiredAmount);
        
        if (req != null)
        {
            req.currentAmount++;
            
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

    public override void UpdateIndicator()
    {
        if (iconsContainer == null || iconPrefab == null) return;

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
}