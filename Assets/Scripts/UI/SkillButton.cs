using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Настройки сохранения")]
    public string skillID; 

    [Header("Настройки покупки")]
    public int cost = 50;
    public bool isPurchased = false;
    public bool isUnlocked = false; 

    [Header("Описание")]
    [TextArea(3, 5)] public string description; // Текст описания для этой кнопки

    [Header("Ссылки на UI компоненты")]
    public Button uiButton;
    public Image iconImage;
    public Text costText; 

    [Header("Визуальные эффекты")]
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

    private void Awake()
    {
        _baseScale = transform.localScale;
        _targetScale = _baseScale;
        if (!string.IsNullOrEmpty(skillID))
            isPurchased = PlayerPrefs.GetInt(skillID + "_Purchased", 0) == 1;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * animationSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isUnlocked && !isPurchased) _targetScale = _baseScale * hoverScale;
        
        // ПОКАЗЫВАЕМ ОПИСАНИЕ
        if (TooltipManager.Instance != null && isUnlocked)
            TooltipManager.Instance.Show(description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetScale = _baseScale;
        
        // СКРЫВАЕМ ОПИСАНИЕ
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isUnlocked && !isPurchased) _targetScale = _baseScale * clickScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isUnlocked && !isPurchased) _targetScale = _baseScale * hoverScale;
    }

    private void OnEnable()
    {
        CrownManager.OnCrownsChanged += RefreshCostDisplay;
    }

    private void OnDisable()
    {
        CrownManager.OnCrownsChanged -= RefreshCostDisplay;
    }

    private void Start()
    {
        RefreshStatus();
    }

    public void RefreshStatus()
    {
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
        if (CrownManager.Instance != null) RefreshCostDisplay(CrownManager.Instance.CurrentCrowns);
    }

    public void TryPurchase()
    {
        if (isPurchased || !isUnlocked) return;
        if (CrownManager.Instance.TrySpendCrowns(cost)) CompletePurchase();
        else OnPurchaseFailed?.Invoke();
    }

    private void CompletePurchase()
    {
        isPurchased = true;
        _targetScale = _baseScale;
        if (TooltipManager.Instance != null) TooltipManager.Instance.Hide(); // Скрываем при покупке

        if (!string.IsNullOrEmpty(skillID))
        {
            PlayerPrefs.SetInt(skillID + "_Purchased", 1);
            PlayerPrefs.Save();
        }

        OnSkillPurchased?.Invoke();
        foreach (var skill in nextSkills) if (skill != null) skill.SetUnlocked(true);
        UpdateUIState();
        if (CrownManager.Instance != null) RefreshCostDisplay(CrownManager.Instance.CurrentCrowns);
    }

    public void SetUnlocked(bool state)
    {
        if (isPurchased) return;
        isUnlocked = state;
        UpdateUIState();
        if (CrownManager.Instance != null) RefreshCostDisplay(CrownManager.Instance.CurrentCrowns);
    }

    private void RefreshCostDisplay(int currentBalance)
    {
        if (costText == null) return;
        if (isPurchased) { costText.gameObject.SetActive(false); return; }
        costText.gameObject.SetActive(true);
        costText.text = cost + " / " + currentBalance;
        if (!isUnlocked) costText.color = lockedColor;
        else costText.color = (currentBalance >= cost) ? canAffordColor : cantAffordColor;
    }

    private void UpdateUIState()
    {
        if (uiButton == null) return;
        uiButton.interactable = isUnlocked && !isPurchased;
        if (iconImage != null)
        {
            if (isPurchased) iconImage.color = Color.gray; 
            else if (!isUnlocked) iconImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            else iconImage.color = Color.white;
        }
    }
}
