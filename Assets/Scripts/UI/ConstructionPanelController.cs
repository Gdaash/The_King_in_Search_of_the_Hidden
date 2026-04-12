using UnityEngine;
using UnityEngine.EventSystems; // Важно для сброса фокуса

public class ConstructionPanelController : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameObject constructionPanel;
    [SerializeField] private KeyCode toggleKey = KeyCode.B;

    void Start()
    {
        if (constructionPanel != null)
        {
            constructionPanel.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (constructionPanel != null)
        {
            bool nextState = !constructionPanel.activeSelf;
            constructionPanel.SetActive(nextState);
            
            if (!nextState) ResetUIFocus();
        }
    }

    public void HidePanel()
    {
        if (constructionPanel != null)
        {
            constructionPanel.SetActive(false);
            ResetUIFocus();
        }
    }

    private void ResetUIFocus()
    {
        // Снимаем выделение с кнопок, чтобы мышь вернулась в мир
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}