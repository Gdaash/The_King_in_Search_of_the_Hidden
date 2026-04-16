using UnityEngine;
using System.Collections;

public class TowerVisuals : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private ArcherTower tower; 
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private Transform spriteTransform; 

    [Header("Настройки цели")]
    [SerializeField] private string targetTag = "Enemy1"; 

    [Header("Анимация отдачи")]
    [SerializeField] private float kickbackDist = 0.3f;
    [SerializeField] private float shootSpeed = 10f;

    private Vector3 _startPos;

    void Start() 
    {
        if (spriteTransform == null) spriteTransform = transform;
        
        _startPos = spriteTransform.localPosition;

        if (tower == null) tower = GetComponent<ArcherTower>();
    }

    public void StartShoot() => StartCoroutine(ShootRoutine());

    private IEnumerator ShootRoutine() 
    {
        if (tower == null) yield break;
        Transform target = tower.GetTarget();
        if (target == null) { tower.FinishAttack(); yield break; }

        // Пауза перед выстрелом (подготовка)
        yield return new WaitForSeconds(0.2f);

        // Повторная проверка цели после паузы
        target = tower.GetTarget();
        if (target == null) { tower.FinishAttack(); yield break; }

        Vector3 worldDir = (target.position - shootPoint.position).normalized;
        
        if (projectilePrefab)
        {
            GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
            if (proj.TryGetComponent<EnemyProjectile>(out var p)) 
                p.Setup(worldDir, targetTag);
        }

        // Анимация отдачи
        if (spriteTransform)
        {
            Vector3 kickbackPos = _startPos - (spriteTransform.InverseTransformDirection(worldDir) * kickbackDist);
            float p = 0;
            while (p < 1f) {
                p += Time.deltaTime * shootSpeed;
                spriteTransform.localPosition = Vector3.Lerp(_startPos, kickbackPos, p);
                yield return null;
            }
            p = 0;
            while (p < 1f) {
                p += Time.deltaTime * shootSpeed * 0.5f;
                spriteTransform.localPosition = Vector3.Lerp(kickbackPos, _startPos, p);
                yield return null;
            }
            spriteTransform.localPosition = _startPos;
        }

        tower.FinishAttack(); 
    }
}