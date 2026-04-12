using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class ObjectStateChecker : MonoBehaviour
{
    public enum UpdateMode { Manual, EveryFrame, WithInterval }

    [System.Serializable]
    public class ObjectCondition
    {
        public string label;
        public GameObject targetObject;
        public bool expectedState = true;
    }

    [Header("Настройки проверки")]
    [SerializeField] private UpdateMode updateMode = UpdateMode.EveryFrame;
    [SerializeField] private float checkInterval = 0.2f; // Интервал для режима WithInterval

    [Header("Список условий")]
    [SerializeField] private List<ObjectCondition> conditions = new List<ObjectCondition>();

    [Header("События")]
    public UnityEvent OnAllConditionsMet;    
    public UnityEvent OnConditionsNotMet;   

    private bool _lastResult = false; // Чтобы событие не вызывалось каждый кадр, а только при смене состояния
    private float _timer;

    void Update()
    {
        if (updateMode == UpdateMode.EveryFrame)
        {
            CheckAllStates();
        }
        else if (updateMode == UpdateMode.WithInterval)
        {
            _timer += Time.deltaTime;
            if (_timer >= checkInterval)
            {
                CheckAllStates();
                _timer = 0;
            }
        }
    }

    public void CheckAllStates()
    {
        if (conditions.Count == 0) return;

        bool allMet = true;

        foreach (var condition in conditions)
        {
            if (condition.targetObject == null) continue;

            bool currentState = condition.targetObject.activeInHierarchy;
            
            if (currentState != condition.expectedState)
            {
                allMet = false;
                break; 
            }
        }

        // Проверяем, изменилось ли состояние по сравнению с прошлой проверкой
        // Это нужно, чтобы OnAllConditionsMet не спамил каждый кадр
        if (allMet != _lastResult)
        {
            if (allMet) OnAllConditionsMet?.Invoke();
            else OnConditionsNotMet?.Invoke();

            _lastResult = allMet;
        }
    }

    public bool AreAllConditionsMet()
    {
        foreach (var condition in conditions)
        {
            if (condition.targetObject == null) continue;
            if (condition.targetObject.activeInHierarchy != condition.expectedState) return false;
        }
        return true;
    }
}