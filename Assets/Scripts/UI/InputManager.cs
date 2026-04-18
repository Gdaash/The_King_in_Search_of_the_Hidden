using UnityEngine;
using System.Collections.Generic;

public class ButtonVisibilityManager : MonoBehaviour
{
    private PauseButton[] _allButtons;

    void Start()
    {
        // Находим все скрипты PauseButton на сцене (даже на выключенных объектах)
        _allButtons = Resources.FindObjectsOfTypeAll<PauseButton>();
        
        // Скрываем их при старте
        SetButtonsActive(false);
    }

    void Update()
    {
        // Проверяем нажатие левого или правого Alt
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            SetButtonsActive(true);
        }

        // Проверяем отпускание клавиши
        if (Input.GetKeyUp(KeyCode.LeftAlt) || Input.GetKeyUp(KeyCode.RightAlt))
        {
            SetButtonsActive(false);
        }
    }

    private void SetButtonsActive(bool state)
    {
        foreach (var button in _allButtons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(state);
            }
        }
    }
}