using UnityEngine;

namespace GameFoundation.Base
{
    public sealed class BuildingGlowPulse : MonoBehaviour
    {
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private Vector2 alphaRange = new(.5f, 1f);
        [SerializeField] private Vector2 durationRange = new(1f, 2f);
        private float elapsed, duration, from, to;
        private void Awake() { if (glow != null) glow.enabled = false; from = alphaRange.x; to = alphaRange.y; NextDuration(); }
        private void OnEnable() => BuildingUpgradeService.Changed += Refresh;
        private void Start() => Refresh();
        private void OnDisable() { BuildingUpgradeService.Changed -= Refresh; if (glow != null) glow.enabled = false; }
        private void Refresh() { if (glow != null) glow.enabled = construction != null && construction.IsBuilt; }
        private void NextDuration() { duration = Random.Range(durationRange.x, durationRange.y); elapsed = 0; }
        private void Update()
        {
            if (glow == null || !glow.enabled) return;
            elapsed += Time.deltaTime;
            Color c = glow.color; c.a = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, elapsed / duration)); glow.color = c;
            if (elapsed >= duration) { from = to; to = to == alphaRange.y ? alphaRange.x : alphaRange.y; NextDuration(); }
        }
    }
}
