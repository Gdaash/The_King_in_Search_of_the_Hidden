using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Audio
{
    public sealed class ButtonAudioHook : MonoBehaviour
    {
        private Button button;
        public void Setup(Button value) { button = value; button.onClick.AddListener(Play); }
        private void Play()
        {
            string lower = button.name.ToLowerInvariant();
            GameAudioCue cue = lower.Contains("build") || lower.Contains("buy") || lower.Contains("confirm") ||
                lower.Contains("summon") || lower.Contains("train") || lower.Contains("next day")
                ? GameAudioCue.UiConfirm
                : lower.Contains("settings") || lower.Contains("warehouse") || lower.Contains("laboratory") ||
                  lower.Contains("map") || lower.Contains("fort") || lower.Contains("range") || lower.Contains("play")
                    ? GameAudioCue.UiOpen
                    : GameAudioCue.UiClick;
            GameAudioController.PlayUI(cue);
        }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Play); }
    }

    public sealed class HealthAudioHook : MonoBehaviour
    {
        private Health health;
        private float previous;
        public void Setup(Health value)
        {
            health = value;
            previous = value.NormalizedHealth;
            health.OnHealthChanged.AddListener(OnChanged);
            health.OnDeath.AddListener(OnDeath);
        }
        private void OnChanged(float current)
        {
            if (current < previous) GameAudioController.PlayAt(GameAudioCue.Hit, transform.position, 0.55f, 0.94f, 1.08f, 0.035f);
            previous = current;
        }
        private void OnDeath() => GameAudioController.PlayAt(GameAudioCue.Death, transform.position, 0.8f, 0.92f, 1.06f, 0.05f);
        private void OnDestroy()
        {
            if (health == null) return;
            health.OnHealthChanged.RemoveListener(OnChanged);
            health.OnDeath.RemoveListener(OnDeath);
        }
    }

    public sealed class CombatAudioHook : MonoBehaviour
    {
        private EnemyAI melee;
        private EnemyAI_Ranged ranged;
        private ArcherTower tower;
        private GameAudioCue cue;
        public void Setup(EnemyAI value) { melee = value; cue = GameAudioCue.SwordAttack; melee.OnAttack.AddListener(Play); }
        public void Setup(EnemyAI_Ranged value) { ranged = value; cue = GameAudioCue.BowAttack; ranged.OnAttack.AddListener(Play); }
        public void Setup(ArcherTower value) { tower = value; cue = GameAudioCue.MagicAttack; tower.OnAttack.AddListener(Play); }
        private void Play() => GameAudioController.PlayAt(cue, transform.position, 0.58f, 0.94f, 1.06f, 0.04f);
        private void OnDestroy()
        {
            if (melee != null) melee.OnAttack.RemoveListener(Play);
            if (ranged != null) ranged.OnAttack.RemoveListener(Play);
            if (tower != null) tower.OnAttack.RemoveListener(Play);
        }
    }

    public sealed class TimerAudioHook : MonoBehaviour
    {
        private TimerController timer;
        public void Setup(TimerController value) { timer = value; timer.OnTimerEnd.AddListener(Play); }
        private void Play()
        {
            string lower = transform.root.name.ToLowerInvariant();
            GameAudioCue cue = lower.Contains("forest") ? GameAudioCue.WoodWork : lower.Contains("stone") ? GameAudioCue.StoneWork : GameAudioCue.ProductionComplete;
            GameAudioController.PlayAt(cue, transform.position, 0.5f, 0.95f, 1.05f, 0.08f);
        }
        private void OnDestroy() { if (timer != null) timer.OnTimerEnd.RemoveListener(Play); }
    }

    public sealed class HexAudioHook : MonoBehaviour
    {
        private HexLightUnlocker hex;
        public void Setup(HexLightUnlocker value) { hex = value; hex.OnUnlockCompleteEvent.AddListener(Play); }
        private void Play() => GameAudioController.PlayAt(GameAudioCue.MagicAttack, transform.position, 0.8f, 0.92f, 1.02f, 0.08f);
        private void OnDestroy() { if (hex != null) hex.OnUnlockCompleteEvent.RemoveListener(Play); }
    }

    public sealed class AlarmAudioHook : MonoBehaviour
    {
        private AlarmSystem alarm;
        public void Setup(AlarmSystem value) { alarm = value; alarm.OnThresholdReached.AddListener(OnThreshold); }
        private void OnThreshold(AlarmThreshold _) => GameAudioController.PlayUI(GameAudioCue.Danger, 0.9f, 0.98f, 1.02f, 0.3f);
        private void OnDestroy() { if (alarm != null) alarm.OnThresholdReached.RemoveListener(OnThreshold); }
    }

    public sealed class FootstepAudioHook : MonoBehaviour
    {
        private Vector3 previousPosition;
        private float travelled;
        private bool alternate;
        private void OnEnable() { previousPosition = transform.position; travelled = 0f; }
        private void Update()
        {
            float distance = Vector2.Distance(previousPosition, transform.position);
            previousPosition = transform.position;
            if (distance <= 0.0001f || distance > 1f) return;
            travelled += distance;
            if (travelled < 0.72f) return;
            travelled = 0f;
            alternate = !alternate;
            GameAudioController.PlayAt(alternate ? GameAudioCue.FootstepA : GameAudioCue.FootstepB, transform.position, 0.15f, 0.92f, 1.08f, 0.025f);
        }
    }
}
