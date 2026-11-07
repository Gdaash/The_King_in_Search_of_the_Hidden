using UnityEngine;

[RequireComponent(typeof(EnemyAI_Ranged))]
public sealed class DarkMageAbility : MonoBehaviour
{
    [SerializeField] private Sprite cloudSprite;
    [SerializeField,Min(.1f)] private float cooldown=15f,castDuration=5f,darknessDuration=5f;
    private EnemyAI_Ranged ai;
    private Health health;
    private EnemyMovement movement;
    private ArcherTower tower;
    private MageDarkCloud casting;
    private float timer,castTime;
    private bool engaged;
    public bool IsCasting => casting!=null;
    private void Awake(){ai=GetComponent<EnemyAI_Ranged>();health=GetComponent<Health>();movement=GetComponent<EnemyMovement>();}
    public bool TryBeginFirstCast(Transform target)
    {
        if(!isActiveAndEnabled||engaged||Time.timeScale<=0||(health!=null&&health.IsDead)||(movement!=null&&movement.IsStunned))return false;
        var candidate=target!=null?target.GetComponentInParent<ArcherTower>():null;
        if(candidate==null||candidate.GetStats()==null||!candidate.GetStats().IsPortalTower)return false;
        tower=candidate;
        if(!InRange)return false;
        engaged=true;
        BeginCast();
        return true;
    }
    private bool InRange => tower!=null&&tower.isActiveAndEnabled&&Vector2.Distance(transform.position,tower.transform.position)<=ai.CurrentAttackRange && !(tower.TryGetComponent<Health>(out var h)&&h.IsDead);
    private void Update()
    {
        if(health!=null&&health.IsDead){Cancel();return;}
        if(Time.deltaTime<=0)return;
        if(casting!=null)
        {
            if(!InRange){Cancel();return;}
            if(movement!=null&&movement.IsStunned)return;
            ai.OnStop?.Invoke();castTime+=Time.deltaTime;casting.transform.position=transform.position+Vector3.up*.8f;casting.Grow(castTime/castDuration);
            if(castTime>=castDuration){casting.Launch(tower,darknessDuration);casting=null;timer=0;}
            return;
        }
        if(!engaged||!InRange)return;
        timer+=Time.deltaTime;
        if(timer<cooldown||ai.GetIsAttacking()||(movement!=null&&movement.IsStunned))return;
        BeginCast();
    }
    private void BeginCast()
    {
        ai.OnStop?.Invoke();
        var root=new GameObject("Dark mage cloud");root.transform.position=transform.position+Vector3.up*.8f;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,gameObject.scene);
        casting=root.AddComponent<MageDarkCloud>();casting.Initialize(cloudSprite);castTime=0;
    }
    private void Cancel(){if(casting!=null)Destroy(casting.gameObject);casting=null;castTime=0;timer=0;}
    private void OnDisable(){Cancel();engaged=false;tower=null;}
}
