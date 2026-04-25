using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ResetAllManager : MonoBehaviour
{
    [Header("Ссылки на все статы юнитов")]
    [SerializeField] private List<GlobalStats> allStats;

    [Header("Настройки")]
    [SerializeField] private bool reloadSceneAfterReset = true;

    public void ResetEverything()
    {
        // 1. Стираем абсолютно все записи PlayerPrefs (короны, покупки кнопок, бонусы статов)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // 2. Сбрасываем значения в самих ScriptableObjects (чтобы изменения применились мгновенно)
        foreach (var stat in allStats)
        {
            if (stat != null)
            {
                stat.ResetProgress();
            }
        }

        Debug.Log("<color=red>ВЕСЬ ПРОГРЕСС СБРОШЕН!</color>");

        // 3. Перезагружаем сцену, чтобы UI и менеджеры обновили данные с нуля
        if (reloadSceneAfterReset)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
