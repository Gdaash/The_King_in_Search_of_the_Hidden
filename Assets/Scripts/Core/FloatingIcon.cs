using UnityEngine;

public class FloatingIcon : MonoBehaviour
{
    private float _moveSpeed;
    private float _duration;
    private SpriteRenderer _sr;
    private float _elapsed;

    // Метод для инициализации из склада
    public void Init(Sprite iconSprite, float speed, float time)
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null) _sr.sprite = iconSprite;
        
        _moveSpeed = speed;
        _duration = time;
        
        Destroy(gameObject, _duration);
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        
        // Движение вверх
        transform.Translate(Vector3.up * _moveSpeed * Time.deltaTime);

        // Плавное исчезновение через альфа-канал
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = Mathf.Lerp(1f, 0f, _elapsed / _duration);
            _sr.color = c;
        }
    }
}