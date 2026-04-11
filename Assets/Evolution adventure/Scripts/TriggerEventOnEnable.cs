using UnityEngine;
using UnityEngine.Events; // Необходим для UnityEvent

namespace Evolution_adventure.Scripts
{
public class EventOnEnable : MonoBehaviour
{
    [Header("События при включении")]
    public UnityEvent onEnabledEvent; // Эвент в инспекторе

    private void OnEnable()
    {
        // Запускает все функции, добавленные в инспекторе
        onEnabledEvent?.Invoke(); 
    }
}
}