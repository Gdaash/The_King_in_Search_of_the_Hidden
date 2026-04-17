using UnityEngine;
using System.Collections;

public class EnemyVisuals_Ranged : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform spriteParent; 
    [SerializeField] private EnemyAI_Ranged ai;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;

    [Header("Прыжки (Движение)")]
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 12f;

    [Header("Анимация отдачи")]
    [SerializeField] private float kickbackDist = 0.3f;
    [SerializeField] private float shootSpeed = 10f;

    [Header("Порог разворота")]
    [SerializeField] private float flipThreshold = 0.1f; 

    private Vector3 _startPos;
    private bool _isMoving;
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    void Start() 
    {
        if (spriteParent != null) _startPos = spriteParent.localPosition;
    }

    void Update()
    {
        HandleFlip();
        HandleBounce();
    }

    private void HandleBounce()
    {
        // Если реальная скорость почти нулевая, выключаем анимацию
        if (_rb != null && _rb.linearVelocity.magnitude < 0.1f)
        {
            _isMoving = false;
        }

        if (_isMoving && spriteParent != null) 
        {
            float wave = Mathf.Abs(Mathf.Sin(Time.time * bounceSpeed));
            spriteParent.localPosition = _startPos + Vector3.up * (wave * bounceHeight);
        } 
        else if (spriteParent != null)
        {
            // Плавное возвращение в исходную точку при остановке
            spriteParent.localPosition = Vector3.Lerp(spriteParent.localPosition, _startPos, Time.deltaTime * 10f);
        }
    }

    private void HandleFlip()
    {
        if (ai == null) return;
        Transform target = ai.GetTarget();
        if (target == null) return;

        float diffX = target.position.x - transform.position.x;
        if (Mathf.Abs(diffX) > flipThreshold)
        {
            float targetScaleX = diffX > 0 ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x);
            if (!Mathf.Approximately(transform.localScale.x, targetScaleX))
                transform.localScale = new Vector3(targetScaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    // Этот метод вызывается из событий OnMove/OnStop в EnemyAI_Ranged
    public void SetMoving(bool state) => _isMoving = state;

    public void StartShoot() 
    {
        _isMoving = false; // Прекращаем прыжки во время стрельбы
        StartCoroutine(ShootRoutine());
    }

    private IEnumerator ShootRoutine() 
    {
        if (ai == null) yield break;
        Transform target = ai.GetTarget();
        if (target == null) { ai.FinishAttack(); yield break; }

        yield return new WaitForSeconds(0.3f);

        Vector3 worldDir = (target.position - shootPoint.position).normalized;
        if (projectilePrefab)
        {
            GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
            if (proj.TryGetComponent<EnemyProjectile>(out var p)) p.Setup(worldDir, ai.GetTargetTag()); 
        }

        if (spriteParent)
        {
            Vector3 kickbackPos = _startPos + transform.InverseTransformDirection(-worldDir * kickbackDist);
            float p = 0;
            while (p < 1f) {
                p += Time.deltaTime * shootSpeed;
                spriteParent.localPosition = Vector3.Lerp(_startPos, kickbackPos, p);
                yield return null;
            }
            p = 0;
            while (p < 1f) {
                p += Time.deltaTime * shootSpeed * 0.5f;
                spriteParent.localPosition = Vector3.Lerp(kickbackPos, _startPos, p);
                yield return null;
            }
            spriteParent.localPosition = _startPos;
        }

        ai.FinishAttack(); 
    }
}