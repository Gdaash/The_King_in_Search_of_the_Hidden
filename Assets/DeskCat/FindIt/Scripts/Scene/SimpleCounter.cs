using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic; // Для работы со списками
using TMPro;

// Класс для настройки отдельного события по достижению числа
[System.Serializable]
public class MilestoneEvent
{
    public string name = "New Milestone"; // Для удобства в инспекторе
    public int targetNumber;             // При каком числе сработает
    public UnityEvent action;            // Что произойдет
    [HideInInspector] public bool hasFired = false; // Чтобы не срабатывало дважды до сброса
}

public class SimpleCounter : MonoBehaviour
{
    [Header("Настройки счета")]
    [SerializeField] private int currentValue = 0;
    
    [Header("Интерфейс (3D TextMeshPro)")]
    [SerializeField] private TextMeshPro scoreText; 
    [SerializeField] private string prefix = "Score: ";

    [Header("Список событий по числам")]
    [SerializeField] private List<MilestoneEvent> milestones = new List<MilestoneEvent>();

    [Header("Общие события")]
    public UnityEvent OnValueChanged;

    void Start()
    {
        UpdateUI();
    }

    public void Add(int amount)
    {
        currentValue += amount;
        OnValueChanged?.Invoke();
        
        CheckMilestones(); // Проверяем список событий
        UpdateUI();
    }

    private void CheckMilestones()
    {
        foreach (var milestone in milestones)
        {
            // Если текущий счет равен или больше цели и событие еще не срабатывало
            if (currentValue >= milestone.targetNumber && !milestone.hasFired)
            {
                milestone.action?.Invoke();
                milestone.hasFired = true; // Помечаем как выполненное
            }
        }
    }

    public void ResetCounter()
    {
        currentValue = 0;
        // При сбросе счетчика разрешаем событиям сработать снова
        foreach (var milestone in milestones)
        {
            milestone.hasFired = false;
        }
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = prefix + currentValue.ToString();
        }
    }
}