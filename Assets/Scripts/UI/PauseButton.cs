using UnityEngine;

public class PauseButton : MonoBehaviour 
{
    private ResourceRequester _requester;

    void Start() 
    {
        // Ищем скрипт здания в родительских объектах
        _requester = GetComponentInParent<ResourceRequester>();
        
        // Авто-настройка коллайдера, если забыли
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
            Debug.Log($"[PauseButton] Добавлен коллайдер на {gameObject.name}");
        }
    }

    // Этот метод сработает, если нажать на коллайдер объекта
    private void OnMouseDown() 
    {
        if (_requester != null) 
        {
            _requester.TogglePause();
            // Небольшой визуальный отклик (пульсация)
            StopAllCoroutines();
            StartCoroutine(ClickFeedback());
        }
    }

    private System.Collections.IEnumerator ClickFeedback()
    {
        transform.localScale = Vector3.one * 0.8f;
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;
    }
}