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

    private Vector3 _startPos;
    private bool _isMoving;
    private SpriteRenderer _sr;
    private Color _origCol;

    void Start() 
    {
        if (spriteParent != null) _startPos = spriteParent.localPosition;
        _sr = spriteParent?.GetComponent<SpriteRenderer>();
        if (_sr) _origCol = _sr.color;
    }

    // Добавляем Update для постоянного разворота к цели
    void Update()
    {
        HandleFlip();
    }

    private void HandleFlip()
    {
        Transform target = ai.GetTarget();
        if (target == null) return;

        // Если цель слева от врага, scale.x должен быть отрицательным
        // (Предполагается, что спрайт изначально смотрит вправо)
        float direction = target.position.x - transform.position.x;

        if (direction > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else if (direction < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    public void SetMoving(bool state) => _isMoving = state;

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

        // РАСЧЕТ ОТДАЧИ: теперь используем мировое направление, 
        // чтобы отдача всегда была в противоположную сторону от выстрела
        Vector3 kickbackWorldPos = transform.position - worldDir * kickbackDist;
        Vector3 kickbackLocalPos = transform.InverseTransformPoint(kickbackWorldPos);
        
        // Движение спрайта назад
        float p_val = 0;
        while (p_val < 1f) 
        {
            p_val += Time.deltaTime * shootSpeed;
            spriteParent.localPosition = Vector3.Lerp(_startPos, kickbackLocalPos, p_val);
            yield return null;
        }

        // Плавный возврат
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