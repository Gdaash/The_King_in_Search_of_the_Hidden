using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    // Метод для вызова через OnClick() у Button
    public void RestartScene()
    {
        // Получаем индекс текущей активной сцены
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        
        // Загружаем её заново
        SceneManager.LoadScene(currentSceneIndex);
    }
}