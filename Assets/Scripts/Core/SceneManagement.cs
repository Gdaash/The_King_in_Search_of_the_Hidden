using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // 1. Метод для перезагрузки текущей сцены
    public void RestartScene()
    {
        Time.timeScale = 1f; // На всякий случай сбрасываем паузу
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    // 2. Метод для запуска сцены по ИМЕНИ (удобно для кнопок)
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // 3. Метод для запуска сцены по ИНДЕКСУ (номеру в Build Settings)
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
}