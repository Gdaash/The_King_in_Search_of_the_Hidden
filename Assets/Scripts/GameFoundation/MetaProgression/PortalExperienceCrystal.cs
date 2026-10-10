using GameFoundation.Audio;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    /// <summary>Cosmetic XP flight; uses unscaled time so a level-up pause cannot leave crystals hanging.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PortalExperienceCrystal : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float flightDuration = .85f;
        [SerializeField, Min(0)] private float arcHeight = .8f;
        [SerializeField, Min(0)] private float scatterRadius = .35f;
        private Transform destination;
        private Vector3 origin, scatter, originalScale;
        private float elapsed, delay;
        private bool arrivalSound;
        public void Launch(Vector3 position, Transform target, SpriteRenderer towerRenderer, float launchDelay, bool playArrivalSound)
        {
            origin = position; destination = target; delay = launchDelay; arrivalSound = playArrivalSound;
            scatter = (Vector3)Random.insideUnitCircle * scatterRadius;
            originalScale = transform.localScale;
            transform.position = position;
            if (towerRenderer != null)
            {
                var sprite = GetComponent<SpriteRenderer>();
                sprite.sortingLayerID = towerRenderer.sortingLayerID;
                sprite.sortingOrder = towerRenderer.sortingOrder + 200;
            }
        }
        private void Update()
        {
            if (destination == null) { Destroy(gameObject); return; }
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01((elapsed - delay) / flightDuration);
            float travel = t * t * (3 - 2 * t);
            Vector3 end = destination.position;
            Vector3 control = (origin + end) * .5f + Vector3.up * arcHeight + scatter;
            transform.position = (1 - travel) * (1 - travel) * origin + 2 * (1 - travel) * travel * control + travel * travel * end;
            float scale = t < .12f ? Mathf.Lerp(.65f, 1f, t / .12f) : t > .8f ? Mathf.Lerp(1f, .15f, (t - .8f) / .2f) : 1f;
            transform.localScale = originalScale * scale;
            if (t < 1) return;
            if (arrivalSound) GameAudioController.PlayAt(GameAudioCue.ResourceGain, end, .3f, 1.1f, 1.2f, .1f);
            Destroy(gameObject);
        }
    }
}
