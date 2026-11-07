using UnityEngine;

namespace GameFoundation.MetaProgression
{
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public sealed class WorldEngineer : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float constructionSeconds = 30f;
        private WorldMilitaryDeploymentController deployment;
        private Health health;
        private Vector2? site;
        private float elapsed, nextCheck;
        private AutoProgressBar progress;
        private WorldBallista station;
        private Rigidbody2D body;
        private UnitPassThrough passThrough;
        private Collider2D bodyCollider;
        private SpriteRenderer sprite;
        private Canvas healthCanvas;
        internal bool CanOperate => station != null && health != null && !health.IsDead &&
            deployment != null && deployment.CanBuildBallista && GetComponent<WorldMilitaryReturner>() == null;
        internal bool HasLightForShot() => site.HasValue && deployment.IsBallistaSiteLit(site.Value);
        internal bool HasSite => site.HasValue;
        internal bool Reserves(Vector2 point) => site.HasValue && health != null && !health.IsDead &&
            isActiveAndEnabled && (site.Value - point).sqrMagnitude < .1f &&
            GetComponent<WorldMilitaryReturner>() == null;

        internal void Initialize(WorldMilitaryDeploymentController owner)
        {
            deployment = owner;
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            passThrough = GetComponent<UnitPassThrough>();
            bodyCollider = GetComponent<Collider2D>();
            sprite = health.BodyRenderer;
            healthCanvas = GetComponentInChildren<Canvas>();
        }

        private void Update()
        {
            if (deployment == null || health == null || health.IsDead || !deployment.CanBuildBallista ||
                GetComponent<WorldMilitaryReturner>() != null)
            {
                LeaveBallista();
                ClearSite();
                return;
            }
            if (Time.timeScale <= 0f) return;

            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + .25f;
                if (site.HasValue && (!deployment.IsBallistaSiteLit(site.Value) || (station != null && !station.IsAlive)))
                {
                    if (station != null) LeaveBallista();
                    else
                    {
                        ClearSite();
                        deployment.MoveEngineer(this, null);
                    }
                    return;
                }
                if (!site.HasValue && GetComponent<WorldMilitaryArrivalMover>() == null &&
                    deployment.FindBallistaSite(this, out var destination))
                {
                    site = destination;
                    deployment.MoveEngineer(this, site);
                }
            }

            if (station != null || !site.HasValue || health.IsStunned || GetComponent<WorldMilitaryArrivalMover>() != null ||
                ((Vector2)transform.position - site.Value).sqrMagnitude > .15f * .15f) return;
            var existing = deployment.BallistaAt(site.Value);
            if (existing != null)
            {
                if (!EnterBallista(existing))
                {
                    ClearSite();
                    deployment.MoveEngineer(this, null);
                }
                return;
            }
            if (!deployment.HasBallistaOrder(site.Value) && !deployment.OrderBallista(site.Value))
            {
                ClearSite();
                deployment.MoveEngineer(this, null);
                return;
            }
            if (!deployment.BallistaMaterialsReady(site.Value)) return;
            elapsed += Time.deltaTime;
            ShowProgress();
            if (elapsed < constructionSeconds) return;
            var completed = deployment.CompleteBallista(site.Value);
            if (completed != null && EnterBallista(completed)) return;
            ClearSite();
            deployment.MoveEngineer(this, null);
        }

        internal void CancelOrderAt(Vector2 point)
        {
            if (station != null || !Reserves(point)) return;
            ClearSite();
            deployment.MoveEngineer(this, null);
        }

        private void ShowProgress()
        {
            if (progress == null)
            {
                var prefab = Resources.Load<GameObject>("Engineering/Defense Progress");
                if (prefab == null) return;
                var marker = Instantiate(prefab, site.Value + Vector2.down * .88f, Quaternion.identity);
                progress = marker.GetComponent<AutoProgressBar>();
            }
            progress.SetProgress(elapsed / constructionSeconds);
        }

        private bool EnterBallista(WorldBallista ballista)
        {
            if (!ballista.TryEnter(this)) return false;
            station = ballista;
            RemoveProgress();
            body.linearVelocity = Vector2.zero;
            if (passThrough != null) passThrough.enabled = false;
            body.simulated = false;
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (sprite != null) sprite.forceRenderingOff = true;
            if (healthCanvas != null) healthCanvas.enabled = false;
            health.enabled = false;
            return true;
        }

        internal void LeaveBallista()
        {
            if (station == null) return;
            station.Release(this);
            station = null;
            body.simulated = true;
            if (bodyCollider != null) bodyCollider.enabled = true;
            if (passThrough != null) passThrough.enabled = true;
            if (sprite != null) sprite.forceRenderingOff = false;
            if (healthCanvas != null) healthCanvas.enabled = true;
            health.enabled = true;
            ClearSite();
            if (isActiveAndEnabled && deployment != null) deployment.MoveEngineer(this, null);
        }

        private void RemoveProgress()
        {
            if (progress != null) Destroy(progress.gameObject);
            progress = null;
        }

        private void ClearSite()
        {
            site = null;
            elapsed = 0;
            RemoveProgress();
        }

        private void OnDisable()
        {
            LeaveBallista();
            ClearSite();
        }
    }
}
