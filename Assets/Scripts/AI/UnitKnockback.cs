using UnityEngine;
using GameFoundation.Combat;

/// <summary>A short physics displacement; normal AI movement resumes after the push.</summary>
[DefaultExecutionOrder(200)]
public sealed class UnitKnockback : MonoBehaviour
{
    public enum Weight { Medium = 0, Light = 1, Heavy = 2 }
    private Rigidbody2D body;
    private Vector2 direction;
    private float remaining, speed;
    private const float Duration=.18f;
    public static float DistanceFor(Weight weight) => weight==Weight.Light?.36f:weight==Weight.Heavy?.09f:.20f;
    public static void Push(Health target, Vector2 away)
    {
        if(target==null||target.IsDead||away.sqrMagnitude<.001f)return;
        var rb=target.GetComponent<Rigidbody2D>();if(rb==null||rb.bodyType!=RigidbodyType2D.Dynamic)return;
        var motion=target.GetComponent<UnitKnockback>()??target.gameObject.AddComponent<UnitKnockback>();
        var combat=target.GetComponent<Combatant>();
        motion.body=rb;motion.direction=away.normalized;motion.remaining=Duration;
        motion.speed=DistanceFor(combat!=null?combat.knockbackWeight:Weight.Medium)/Duration;
    }
    private void FixedUpdate()
    {
        if(remaining<=0||body==null)return;
        body.linearVelocity=direction*speed;remaining=Mathf.Max(0,remaining-Time.fixedDeltaTime);
    }
    private void OnDisable(){remaining=0;if(body!=null)body.linearVelocity=Vector2.zero;}
}
