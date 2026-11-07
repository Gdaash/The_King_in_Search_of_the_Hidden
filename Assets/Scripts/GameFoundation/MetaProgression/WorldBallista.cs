using GameFoundation.Audio;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Health))]
    public sealed class WorldBallista : MonoBehaviour
    {
        [SerializeField] private Transform rotatingHead;
        [SerializeField] private EnemyProjectile boltPrefab;
        [SerializeField, Min(.1f)] private float attackRange = 5f;
        [SerializeField, Min(.1f)] private float attackInterval = 3f;
        [SerializeField, Min(0f)] private float attackDamage = 35f;
        [SerializeField, Min(1f)] private float turnSpeed = 360f;
        private Health health;
        private Collider2D target;
        private float nextSearch, nextShot;
        public WorldEngineer Operator { get; private set; }

        internal bool TryEnter(WorldEngineer engineer)
        {
            if (!IsAlive || Operator != null) return false;
            Operator = engineer;
            return true;
        }

        internal void Release(WorldEngineer engineer)
        {
            if (Operator == engineer) Operator = null;
        }

        private void OnDisable() => Operator?.LeaveBallista();

        public bool IsAlive => health != null && !health.IsDead && isActiveAndEnabled;

        private void Awake() => health = GetComponent<Health>();

        private void Update()
        {
            if (Time.timeScale <= 0f || !IsAlive || Operator == null || !Operator.CanOperate || health.IsStunned || rotatingHead == null || boltPrefab == null) return;
            if (Time.time >= nextSearch)
            {
                nextSearch = Time.time + .15f;
                target = CombatTargetRegistry.ClosestCollider("Enemy1", transform.position, attackRange);
            }
            if (target == null || !CombatTargetRegistry.IsAlive(target.transform)) return;
            Vector2 direction = (Vector2)target.bounds.center - (Vector2)rotatingHead.position;
            if (direction.sqrMagnitude < .001f || direction.sqrMagnitude > attackRange * attackRange) return;
            var aim = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            rotatingHead.rotation = Quaternion.RotateTowards(rotatingHead.rotation, aim, turnSpeed * Time.deltaTime);
            if (Quaternion.Angle(rotatingHead.rotation, aim) > 3f || Time.time < nextShot || !Operator.HasLightForShot()) return;

            nextShot = Time.time + attackInterval;
            var bolt = Instantiate(boltPrefab, rotatingHead.position, aim);
            bolt.SetupHit(direction.normalized, "Enemy1", attackDamage, DamageType.Physical, transform);
            GameAudioController.PlayAt(GameAudioCue.BowAttack, transform.position, .75f, .75f, .9f, .1f);
        }
    }
}
