using UnityEngine;

/// <summary>A vulnerable reach fighter that takes a short backward step after a successful jab.</summary>
[RequireComponent(typeof(EnemyAI),typeof(EnemyMovement))]
public sealed class SpearmanTactics : MonoBehaviour
{
    [SerializeField,Min(.05f)] private float retreatDuration=.55f;
    [SerializeField,Range(.1f,1f)] private float safeRangeFraction=.95f;
    private EnemyAI ai;
    private Health health;
    private Transform hitTarget;
    private float retreatUntil;
    private void Awake(){ai=GetComponent<EnemyAI>();health=GetComponent<Health>();}
    public void OnHit(Health target)
    {
        if(target==null)return;
        UnitKnockback.Push(target,(Vector2)(target.transform.position-transform.position));
        hitTarget=target.IsDead?null:target.transform;
    }
    public void AttackFinished()
    {
        if(hitTarget!=null)retreatUntil=Time.time+retreatDuration;
        hitTarget=null;
    }
    public bool RetreatFrom(Transform target)
    {
        return isActiveAndEnabled && target!=null && Time.time<retreatUntil &&
            (health==null||!health.IsDead) &&
            Vector2.Distance(transform.position,target.position)<ai.BaseAttackRange*GameFoundation.MetaProgression.MilitaryExperience.Multiplier(this)*safeRangeFraction;
    }
    private void OnDisable(){hitTarget=null;retreatUntil=0;}
}
