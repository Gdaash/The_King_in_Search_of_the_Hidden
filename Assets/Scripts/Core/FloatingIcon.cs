using UnityEngine;

public class FloatingIcon : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float duration = 1.2f;
    private SpriteRenderer _sr;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        // Уничтожаем объект через заданное время
        Destroy(gameObject, duration);
    }

    void Update()
    {
        // Движение вверх
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

        // Плавное исчезновение (fade out)
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a -= Time.deltaTime / duration;
            _sr.color = c;
        }
    }
}