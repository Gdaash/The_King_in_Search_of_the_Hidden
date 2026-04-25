using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro; // Убедитесь, что используете TextMeshPro UGUI
using System;

public class SkillButton : MonoBehaviour
{
    [Header("Настройки сохранения")]
    public string skillID; // Уникальный ID, например "Archer_HP_1"

    [Header("Настройки покупки")]
    public int cost = 50;
    public bool isPurchased = false;
    public bool isUnlocked = false; 

    [Header("Ссылки на UI компоненты")]
    public Button uiButton;
    public Image iconImage;
    public TextMeshProUGUI costText; // Поле для текста "Цена / Баланс"

    [Header("Цвета текста цены")]
    public Color lockedColor = Color.gray;
    public Color canAffordColor = Color.green;
    public Color cantAffordColor = Color.red;

    [Header("Связи дерева")]
    public SkillButton[] nextSkills; 

    [Header("События")]
    public UnityEvent OnSkillPurchased; 
    public static event Action OnPurchaseFailed;

    private void Awake()
    {
        LoadState();
    }

    private void OnEnable()
    {
        // Подписываемся на изменение баланса, чтобы текст обновлялся мгновенно
        CrownManager.OnCrownsChanged += RefreshCostDisplay;
    }

    private void OnDisable()
    {
        CrownManager.OnCrownsChanged -= RefreshCostDisplay;
    }

    private void Start()
    {
        UpdateUIState();
        if (CrownManager.Instance != null) 
            RefreshCostDisplay(CrownManager.Instance.CurrentCrowns);
    }

    private void LoadState()
    {
        if (string.IsNullOrEmpty(skillID)) return;

        if (PlayerPrefs.GetInt(skillID + "_Purchased", 0) == 1)
        {
            isPurchased = true;
            isUnlocked = true;
            // Открываем путь дальше по дереву
            foreach (var skill in nextSkills) skill.SetUnlocked(true);
        }
    }

    public void TryPurchase()
    {
        if (isPurchased || !isUnlocked) return;

        if (CrownManager.Instance.TrySpendCrowns(cost))
        {
            CompletePurchase();
        }
        else
        {
            OnPurchaseFailed?.Invoke(); // Вызовет красный бамп в CrownUI
        }
    }

    private void CompletePurchase()
    {
        isPurchased = true;
        
        if (!string.IsNullOrEmpty(skillID))
        {
            PlayerPrefs.SetInt(skillID + "_Purchased", 1);
            PlayerPrefs.Save();
        }

        OnSkillPurchased?.Invoke();

        foreach (var skill in nextSkills)
        {
            skill.SetUnlocked(true);
        }
        
        UpdateUIState();
        if (CrownManager.Instance != null) 
            RefreshCostDisplay(CrownManager.Instance.CurrentCrowns);
    }

    public void SetUnlocked(bool state)
    {
        if (isPurchased) return;
        isUnlocked = state;
        UpdateUIState();
        if (CrownManager.Instance != null) 
            RefreshCostDisplay(CrownManager.Instance.CurrentCrowns);
    }

    private void RefreshCostDisplay(int currentBalance)
    {
        if (costText == null) return;

        // После покупки текст исчезает совсем
        if (isPurchased)
        {
            costText.gameObject.SetActive(false);
            return;
        }

        costText.gameObject.SetActive(true);
        costText.text = $"{cost} / {currentBalance}";

        // Логика цвета текста
        if (!isUnlocked)
        {
            costText.color = lockedColor;
        }
        else
        {
            costText.color = (currentBalance >= cost) ? canAffordColor : cantAffordColor;
        }
    }

    private void UpdateUIState()
    {
        if (uiButton == null) return;

        // Кнопка активна только если разблокирована и не куплена
        uiButton.interactable = isUnlocked && !isPurchased;
        
        if (iconImage != null)
        {
            if (isPurchased) iconImage.color = Color.gray; 
            else if (!isUnlocked) iconImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            else iconImage.color = Color.white;
        }
    }
}
