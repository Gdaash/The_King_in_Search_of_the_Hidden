using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Настройки сохранения")]
    public string skillID;
    [SerializeField] private GlobalStats upgradeStats;

    [Header("Настройки покупки")]
    [Tooltip("Ресурс, который тратится на покупку этого навыка (например, Короны)")]
    [SerializeField] private ResourceType purchaseResourceType;
    
    public int cost = 50;
    public bool isPurchased = false;
    public bool isUnlocked = false;

    [Header("Спрайты состояний")]
    public Sprite lockedSprite;
    public Sprite canAffordSprite;
    public Sprite cantAffordSprite;
    public Sprite purchasedSprite;

    [Header("Описание")]
    public string title;
    [TextArea(3, 5)] public string description;

    [Header("Ссылки на UI компоненты")]
    public Button uiButton;
    public Image targetImage; // Изображение, которое будет менять спрайт (обычно фон кнопки)
    [SerializeField] private Image costResourceIconImage;
    [SerializeField] private Image upgradeIconImage;
    public Text costText;

    [Header("Визуальные эффекты (Бамп)")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float clickScale = 0.9f;
    [SerializeField] private float animationSpeed = 15f;

    [Header("Цвета текста цены")]
    public Color lockedColor = Color.gray;
    public Color canAffordColor = Color.green;
    public Color cantAffordColor = Color.red;

    [Header("Связи дерева")]
    public SkillButton[] nextSkills;

    [Header("События")]
    public UnityEvent OnSkillPurchased;
    public static event Action OnPurchaseFailed;

    private Vector3 _baseScale;
    private Vector3 _targetScale;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
        _rectTransform = GetComponent<RectTransform>();

        if (!string.IsNullOrEmpty(skillID))
        {
            isPurchased = upgradeStats != null && upgradeStats.HasUpgrade(skillID);
        }
    }

    // ИСПРАВЛЕНО: Подписка на новое событие GlobalResourceManager
    private void OnEnable()
    {
        if (upgradeStats == null)
            Debug.LogError($"[SkillButton] Для {name} не назначен GlobalStats с прогрессом улучшений.", this);
        ApplyScientificDefinition();
        GlobalResourceManager.OnResourceChanged += RefreshCostDisplay;
        RefreshStatus();
    }
    private void ApplyScientificDefinition()
    {
        var entry = upgradeStats != null ? upgradeStats.FindUpgradeDefinition(skillID) : null;
        if (entry == null) return;
        title = entry.title;
        description = entry.description;
        purchaseResourceType = entry.costResource;
        cost = entry.cost;
        if (costResourceIconImage == null)
            costResourceIconImage = transform.Find("CostIconBacking/CostResourceIcon")?.GetComponent<Image>();
        if (upgradeIconImage == null)
            upgradeIconImage = transform.Find("UpgradeIcon")?.GetComponent<Image>();
        if (costResourceIconImage != null && purchaseResourceType != null)
        {
            costResourceIconImage.sprite = purchaseResourceType.resourceIcon;
            costResourceIconImage.preserveAspect = true;
        }
    }
    private void OnDisable() => GlobalResourceManager.OnResourceChanged -= RefreshCostDisplay;

    private void Start()
    {
        RefreshStatus();
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked && !isPurchased) _targetScale = _baseScale * hoverScale;
        if (TooltipManager.Instance != null && isUnlocked)
        {
            var key = "skill." + skillID + ".description";
            var translated = GameFoundation.Localization.LocalizationService.Instance?.Get(key);
            var body = string.IsNullOrEmpty(translated) || translated == key ? description : translated;
            TooltipManager.Instance.Show(string.IsNullOrEmpty(title) ? body : "<b>" + title + "</b>\n" + body, _rectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _baseScale;
        if (TooltipManager.Instance != null) TooltipManager.Instance.Hide();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isUnlocked && !isPurchased) _targetScale = _baseScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isUnlocked && !isPurchased) _targetScale = _baseScale * hoverScale;
    }

    public void RefreshStatus()
    {
        if (!string.IsNullOrEmpty(skillID) && upgradeStats != null && upgradeStats.HasUpgrade(skillID))
            isPurchased = true;
        if (isPurchased)
        {
            isUnlocked = true;
            foreach (var skill in nextSkills)
            {
                if (skill != null)
                {
                    skill.SetUnlocked(true);
                    skill.RefreshStatus();
                }
            }
        }

        UpdateUIState();
        
        // ИСПРАВЛЕНО: Получение баланса через новый менеджер
        if (GlobalResourceManager.Instance != null && purchaseResourceType != null) 
        {
            int currentBalance = GlobalResourceManager.Instance.GetResourceAmount(purchaseResourceType);
            RefreshCostDisplay(purchaseResourceType, currentBalance);
        }
    }

    public void TryPurchase()
    {
        if (isPurchased || !isUnlocked || upgradeStats == null) return;
        
        // ИСПРАВЛЕНО: Списание через новый менеджер
        if (GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.TrySpendResource(purchaseResourceType, cost)) 
        {
            CompletePurchase();
        }
        else 
        {
            OnPurchaseFailed?.Invoke();
        }
    }

    private void CompletePurchase()
    {
        isPurchased = true;
        _targetScale = _baseScale;
        if (TooltipManager.Instance != null) TooltipManager.Instance.Hide();

        if (!string.IsNullOrEmpty(skillID))
        {
            upgradeStats.UnlockUpgrade(skillID);
        }

        OnSkillPurchased?.Invoke();
        foreach (var skill in nextSkills) 
        {
            if (skill != null) skill.SetUnlocked(true);
        }
        
        UpdateUIState();
        
        // ИСПРАВЛЕНО: Обновление UI через новый менеджер
        if (GlobalResourceManager.Instance != null && purchaseResourceType != null) 
        {
            int currentBalance = GlobalResourceManager.Instance.GetResourceAmount(purchaseResourceType);
            RefreshCostDisplay(purchaseResourceType, currentBalance);
        }
    }

    public void SetUnlocked(bool state)
    {
        if (isPurchased) return;
        isUnlocked = state;
        UpdateUIState();
        
        // ИСПРАВЛЕНО: Обновление UI через новый менеджер
        if (GlobalResourceManager.Instance != null && purchaseResourceType != null) 
        {
            int currentBalance = GlobalResourceManager.Instance.GetResourceAmount(purchaseResourceType);
            RefreshCostDisplay(purchaseResourceType, currentBalance);
        }
    }

    // ИСПРАВЛЕНО: Сигнатура метода теперь соответствует событию OnResourceChanged
    private void RefreshCostDisplay(ResourceType changedType, int newBalance)
    {
        // Игнорируем изменения других ресурсов, реагируем только на наш
        if (changedType != purchaseResourceType) return;
        if (costText == null) return;

        if (isPurchased)
        {
            costText.gameObject.SetActive(false);
            return;
        }

        costText.gameObject.SetActive(true);
        costText.text = cost + " / " + newBalance;

        if (!isUnlocked) 
            costText.color = lockedColor;
        else 
            costText.color = (newBalance >= cost) ? canAffordColor : cantAffordColor;
        
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        if (uiButton != null) uiButton.interactable = isUnlocked && !isPurchased;
        if (targetImage == null) return;

        if (isPurchased)
        {
            targetImage.sprite = purchasedSprite;
        }
        else if (!isUnlocked)
        {
            targetImage.sprite = lockedSprite;
        }
        else
        {
            // ИСПРАВЛЕНО: Проверка баланса через новый менеджер
            int currentBalance = 0;
            if (GlobalResourceManager.Instance != null && purchaseResourceType != null)
            {
                currentBalance = GlobalResourceManager.Instance.GetResourceAmount(purchaseResourceType);
            }

            if (currentBalance >= cost)
                targetImage.sprite = canAffordSprite;
            else
                targetImage.sprite = cantAffordSprite;
        }
    }
}
