using UnityEngine;
using TMPro; // Не забудь, что нужен TextMeshPro

public class DamageText : MonoBehaviour
{
    [Header("Настройки анимации")]
    [SerializeField] private float moveSpeed = 1.5f;   // Скорость полета вверх
    [SerializeField] private float fadeDuration = 1.0f; // Время жизни текста
    
    private TextMeshPro _textMesh;
    private Color _initialColor;
    private float _timer;

    void Awake()
    {
        // Ищем компонент TextMeshPro (3D, не UI!)
        _textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(string text, Color color)
    {
        if (_textMesh == null) _textMesh = GetComponent<TextMeshPro>();
        
        _textMesh.text = text;
        _textMesh.color = color;
        _initialColor = color;
        _timer = fadeDuration;
    }

    void Update()
    {
        // Движение вверх
        transform.position += Vector3.up * (moveSpeed * Time.deltaTime);

        // Плавное исчезновение (альфа-канал)
        _timer -= Time.deltaTime;
        float alpha = _timer / fadeDuration;
        _textMesh.color = new Color(_initialColor.r, _initialColor.g, _initialColor.b, alpha);

        // Удаление объекта, когда он полностью исчезнет
        if (_timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}