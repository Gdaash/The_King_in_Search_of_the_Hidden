using UnityEngine;

public class UniversalPingPong : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private Vector3 localDirection = Vector3.up; 
    [SerializeField] private float amplitude = 50f;
    [SerializeField] private float speed = 2f;
    
    [Header("Корректировка")]
    [Tooltip("Дополнительный поворот вектора движения (в градусах)")]
    [SerializeField] private float angleOffset = 0f; 

    [Header("Пауза")]
    [SerializeField] private float pauseDuration = 0.5f;

    private Vector3 _startPos;
    private float _timer;

    void Awake()
    {
        _startPos = transform.localPosition;
    }

    void Update()
    {
        _timer += Time.deltaTime * speed;

        // Плавная волна с замиранием
        float wave = Mathf.Cos(_timer);
        float movementWithPause = Mathf.Clamp(wave * (1f + pauseDuration), -1f, 1f);

        transform.localPosition = _startPos + GetCurrentDirection() * (movementWithPause * amplitude);
    }

    // Вынес расчет направления в отдельный метод, чтобы использовать и в Update, и в Gizmos
    private Vector3 GetCurrentDirection()
    {
        Quaternion offsetRotation = Quaternion.Euler(0, 0, angleOffset);
        return (transform.localRotation * offsetRotation) * localDirection.normalized;
    }

    // Рисует линию траектории в окне Scene
    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? _startPos : transform.localPosition;
        
        // Если мы в UI (World Canvas), координаты могут быть большими, 
        // поэтому используем Matrix, чтобы учитывать масштаб родителя
        Gizmos.matrix = transform.parent != null ? transform.parent.localToWorldMatrix : Matrix4x4.identity;
        
        Vector3 dir = GetCurrentDirection();
        Vector3 posA = start + dir * amplitude;
        Vector3 posB = start - dir * amplitude;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(posA, posB);
        Gizmos.DrawSphere(posA, amplitude * 0.05f);
        Gizmos.DrawSphere(posB, amplitude * 0.05f);
    }
}
