using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class WorldMilitaryArrivalMover : MonoBehaviour
    {
        private Vector2 destination;
        private float speed;
        private Rigidbody2D body;
        private EnemyAI melee;
        private EnemyAI_Ranged ranged;
        private EnemyMovement movement;
        private EnemyVisuals meleeVisuals;
        private EnemyVisuals_Ranged rangedVisuals;
        private float initialScaleX;

        public void Begin(Vector2 target)
        {
            destination = target;
            body = GetComponent<Rigidbody2D>();
            initialScaleX = Mathf.Abs(transform.localScale.x);
            melee = GetComponent<EnemyAI>();
            ranged = GetComponent<EnemyAI_Ranged>();
            movement = GetComponent<EnemyMovement>();
            speed = movement != null ? movement.CurrentSpeed : 3f;
            meleeVisuals = GetComponent<EnemyVisuals>();
            rangedVisuals = GetComponent<EnemyVisuals_Ranged>();
            if (melee != null) melee.enabled = false;
            if (ranged != null) ranged.enabled = false;
            if (melee != null) melee.SetHomePoint(destination);
            if (ranged != null) ranged.SetHomePoint(destination);
            if (movement != null) movement.enabled = false;
            if (meleeVisuals != null) meleeVisuals.SetMoving(true);
            if (rangedVisuals != null) rangedVisuals.SetMoving(true);
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            Vector2 delta = destination - body.position;
            if (delta.sqrMagnitude <= 0.04f)
            {
                body.linearVelocity = Vector2.zero;
                if (meleeVisuals != null) meleeVisuals.SetMoving(false);
                if (rangedVisuals != null) rangedVisuals.SetMoving(false);
                if (melee != null) melee.enabled = true;
                if (ranged != null) ranged.enabled = true;
                if (movement != null) movement.enabled = true;
                Destroy(this);
                return;
            }

            Vector2 direction = delta.normalized;
            body.linearVelocity = direction * speed;
            if (Mathf.Abs(direction.x) > 0.05f)
                transform.localScale = new Vector3(initialScaleX * Mathf.Sign(direction.x), transform.localScale.y, transform.localScale.z);
        }
    }
}
