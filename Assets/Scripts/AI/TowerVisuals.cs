using GameFoundation.Combat;
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
    public Transform ShootPoint => shootPoint;

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

        float attackSpeed = tower.PortalProgression != null ? tower.PortalProgression.AttackSpeedMultiplier : 1f;
        if (windupDuration > 0f) yield return new WaitForSeconds(windupDuration / attackSpeed);

        target = tower.GetTarget();
        if (target == null || (tower.PortalProgression != null && tower.PortalProgression.ExpeditionEnded)) { tower.FinishAttack(); yield break; }

        if (shootPoint == null) { tower.FinishAttack(); yield break; }
        Vector3 worldDir = (target.position - shootPoint.position).normalized;
        
        if (projectilePrefab)
        {
            var combat=GetComponent<Combatant>();
            int count = tower.PortalProgression != null ? tower.PortalProgression.ProjectileCount : combat?.weapon?.count??1;
            for (int i = 0; i < count; i++)
            {
                // Parallel shots stay aimed at the target, with a small transverse separation.
                Vector3 offset = new Vector3(-worldDir.y, worldDir.x) * ((i - (count - 1) * .5f) * .09f);
                GameObject proj = Instantiate(projectilePrefab, shootPoint.position + offset, Quaternion.identity);
                if (proj.TryGetComponent<EnemyProjectile>(out var p))
                {
                    var progression=tower.PortalProgression;
                    if(progression!=null)
                    {
                        var weapon=progression.Balance.FindWeapon(GameFoundation.MetaProgression.PortalTowerBalance.Weapon.Bolts);
                        p.SetupHit(worldDir,targetTag,(weapon.damage+(tower.GetStats()?.damageSettings.Find(d=>d.type==weapon.damageType)?.bonusDamage??0))*progression.DamageMultiplier,weapon.damageType,tower.transform,
                            progression.Multiplier(weapon.weapon,GameFoundation.MetaProgression.PortalTowerBalance.Effect.ExpansionSpeed),tower.CurrentRange+1);
                    }
                    else if(combat!=null && combat.weapon!=null)
                        p.SetupHit(worldDir,targetTag,combat.Damage,combat.weapon.damageType,tower.transform);
                    else
                    {
                        var stats=tower.GetStats();
                        p.Setup(worldDir,targetTag,stats!=null?stats.damageSettings:new List<GlobalStats.DamageInfo>(),tower.transform);
                    }
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
                p += Time.deltaTime * shootSpeed * attackSpeed;
                spriteTransform.localPosition = Vector3.Lerp(_startPos, kickbackPos, p);
                yield return null;
            }
            p = 0;
            while (p < 1f) {
                p += Time.deltaTime * shootSpeed * attackSpeed * 0.5f;
                spriteTransform.localPosition = Vector3.Lerp(kickbackPos, _startPos, p);
                yield return null;
            }
            spriteTransform.localPosition = _startPos;
        }

        tower.FinishAttack(); 
    }
}
