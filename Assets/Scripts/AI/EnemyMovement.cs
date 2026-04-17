using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float speedVariation = 0.7f;

    [Header("Настройки разворота")]
    [SerializeField] private float flipThreshold = 0.1f; 

    private Rigidbody2D _rb;
    private IEnemyAI _ai; 
    private bool _canMove = false;
    private float _initialScaleX;
    private float _finalSpeed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _ai = GetComponent<IEnemyAI>(); 
        _initialScaleX = Mathf.Abs(transform.localScale.x);
        
        // Настройки физики для плавного прохождения сквозь всё
        _rb.gravityScale = 0;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Start() => _finalSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);

    public void SetMove(bool state) 
    {
        _canMove = state;
        if (!state) 
        {
            _rb.linearVelocity = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        // Если движение запрещено или цели нет — стоим
        if (!_canMove || _ai == null || _ai.GetTarget() == null) 
        { 
            _rb.linearVelocity = Vector2.zero; 
            return; 
        }

        Vector2 targetPos = _ai.GetTarget().position;
        Vector2 currentPos = _rb.position;

        // Рассчитываем вектор направления напрямую к цели
        Vector2 direction = (targetPos - currentPos);
        float distance = direction.magnitude;

        // Если мы уже очень близко (погрешность), останавливаемся
        if (distance < 0.1f)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        // Движение строго по прямой линии
        _rb.linearVelocity = direction.normalized * _finalSpeed;
        
        // Разворот спрайта (лево/право)
        HandleFlip();
    }

    private void HandleFlip()
    {
        if (Mathf.Abs(_rb.linearVelocity.x) > flipThreshold)
        {
            float newScaleX = _rb.linearVelocity.x > 0 ? _initialScaleX : -_initialScaleX;
            transform.localScale = new Vector3(newScaleX, transform.localScale.y, transform.localScale.z);
        }
    }
}