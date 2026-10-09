using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Добавлено для работы со списками

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
    [SerializeField, Min(0f)] private float windupDuration = 0.2f;

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

        if (windupDuration > 0f) yield return new WaitForSeconds(windupDuration);

        target = tower.GetTarget();
        if (target == null) { tower.FinishAttack(); yield break; }

        if (shootPoint == null) { tower.FinishAttack(); yield break; }
        Vector3 worldDir = (target.position - shootPoint.position).normalized;
        
        if (projectilePrefab)
        {
            GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
            if (proj.TryGetComponent<EnemyProjectile>(out var p)) 
            {
                // ПОЛУЧАЕМ СПИСОК УРОНА
                // Вариант А: Если в ArcherTower есть метод GetStats() (как у лучника)
                var stats = tower.GetStats(); 
                
                if (stats != null)
                {
                    p.Setup(worldDir, targetTag, stats.damageSettings, tower.transform);
                }
                else
                {
                    // Если статы не назначены, передаем пустой список, чтобы не было ошибки
                    p.Setup(worldDir, targetTag, new List<GlobalStats.DamageInfo>());
                }
            }
        }

        // Анимация отдачи
        if (spriteTransform)
        {
            Vector3 localDir = spriteTransform.parent != null ? spriteTransform.parent.InverseTransformVector(worldDir) : worldDir;
            Vector3 kickbackPos = _startPos - localDir * kickbackDist;
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
