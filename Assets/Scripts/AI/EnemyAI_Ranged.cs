using UnityEngine;
using System.Collections;

public class EnemyVisuals_Ranged : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform spriteParent; 
    [SerializeField] private EnemyAI_Ranged ai;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;

    [Header("Анимация отдачи")]
    [SerializeField] private float kickbackDist = 0.3f;
    [SerializeField] private float shootSpeed = 10f;

    [Header("Порог разворота")]
    [SerializeField] private float flipThreshold = 0.1f; // Порог дистанции по X для разворота к цели

    private Vector3 _startPos;
    private SpriteRenderer _sr;
    private Color _origCol;

    void Start() 
    {
        if (spriteParent != null) _startPos = spriteParent.localPosition;
        _sr = spriteParent?.GetComponent<SpriteRenderer>();
        if (_sr) _origCol = _sr.color;
    }

    void Update()
    {
        HandleFlip();
    }

    private void HandleFlip()
    {
        Transform target = ai.GetTarget();
        if (target == null) return;

        // Считаем разницу по X между целью и врагом
        float diffX = target.position.x - transform.position.x;

        // Разворачиваемся только если цель отошла от центральной оси на flipThreshold
        if (Mathf.Abs(diffX) > flipThreshold)
        {
            float targetScaleX = diffX > 0 ? Mathf.Abs(transform.localScale.x) : -Mathf.Abs(transform.localScale.x);
            
            if (!Mathf.Approximately(transform.localScale.x, targetScaleX))
            {
                transform.localScale = new Vector3(targetScaleX, transform.localScale.y, transform.localScale.z);
            }
        }
    }

    public void StartShoot() => StartCoroutine(ShootRoutine());

    private IEnumerator ShootRoutine() 
    {
        Transform target = ai.GetTarget();
        if (target == null) 
        { 
            ai.FinishAttack(); 
            yield break; 
        }

        if (_sr) _sr.color = Color.yellow; 
        yield return new WaitForSeconds(0.3f);

        Vector3 worldDir = (target.position - shootPoint.position).normalized;
        
        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        if (proj.TryGetComponent<EnemyProjectile>(out var p)) 
        {
            p.Setup(worldDir, ai.GetTargetTag()); 
        }

        // Расчет отдачи
        Vector3 kickbackWorldPos = transform.position - worldDir * kickbackDist;
        Vector3 kickbackLocalPos = transform.InverseTransformPoint(kickbackWorldPos);
        
        float p_val = 0;
        while (p_val < 1f) 
        {
            p_val += Time.deltaTime * shootSpeed;
            spriteParent.localPosition = Vector3.Lerp(_startPos, kickbackLocalPos, p_val);
            yield return null;
        }

        p_val = 0;
        while (p_val < 1f) 
        {
            p_val += Time.deltaTime * shootSpeed * 0.5f;
            spriteParent.localPosition = Vector3.Lerp(kickbackLocalPos, _startPos, p_val);
            yield return null;
        }
        
        spriteParent.localPosition = _startPos;
        if (_sr) _sr.color = _origCol;
        ai.FinishAttack();
    }
}