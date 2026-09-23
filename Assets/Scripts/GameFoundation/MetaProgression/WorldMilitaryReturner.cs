using System;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WorldMilitaryReturner : MonoBehaviour
    {
        private Transform portal;
        private Action<GameObject> arrived;
        private Rigidbody2D body;
        private float speed;
        private float initialScaleX;
        private EnemyVisuals meleeVisuals;
        private EnemyVisuals_Ranged rangedVisuals;

        public void Begin(Transform targetPortal, Action<GameObject> onArrived)
        {
            portal = targetPortal;
            arrived = onArrived;
            body = GetComponent<Rigidbody2D>();
            initialScaleX = Mathf.Abs(transform.localScale.x);
            WorldMilitaryArrivalMover arrivalMover = GetComponent<WorldMilitaryArrivalMover>();
            if (arrivalMover != null)
            {
                arrivalMover.enabled = false;
                Destroy(arrivalMover);
            }

            EnemyAI melee = GetComponent<EnemyAI>();
            if (melee != null) melee.enabled = false;
            EnemyAI_Ranged ranged = GetComponent<EnemyAI_Ranged>();
            if (ranged != null) ranged.enabled = false;
            EnemyMovement movement = GetComponent<EnemyMovement>();
            speed = movement != null ? movement.CurrentSpeed : 3f;
            if (movement != null) movement.enabled = false;

            meleeVisuals = GetComponent<EnemyVisuals>();
            rangedVisuals = GetComponent<EnemyVisuals_Ranged>();
            if (meleeVisuals != null) meleeVisuals.SetMoving(true);
            if (rangedVisuals != null) rangedVisuals.SetMoving(true);
        }

        private void FixedUpdate()
        {
            if (portal == null || body == null) return;
            Vector2 delta = (Vector2)portal.position - body.position;
            if (delta.sqrMagnitude <= 0.16f)
            {
                body.linearVelocity = Vector2.zero;
                Action<GameObject> callback = arrived;
                arrived = null;
                callback?.Invoke(gameObject);
                return;
            }

            Vector2 moveDirection = PlayerHexNavigation.ResolveDirection(body.position, delta.normalized);
            body.linearVelocity = moveDirection * speed;
            if (Mathf.Abs(body.linearVelocity.x) > 0.05f)
            {
                float direction = body.linearVelocity.x > 0f ? 1f : -1f;
                transform.localScale = new Vector3(initialScaleX * direction, transform.localScale.y, transform.localScale.z);
            }
        }
    }
}
