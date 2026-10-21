using UnityEngine;
using GameFoundation.Localization;
using GameFoundation.MetaProgression;

namespace GameFoundation.UI
{
    public sealed class WaveProvokeButton : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Text label, reward;
        [SerializeField] private UnityEngine.UI.Image crystalIcon;
        private AlarmSystem alarm;
        private int displayedReward = -1;
        private string language;
        public UnityEngine.UI.Button Button => button;
        public UnityEngine.UI.Text RewardLabel => reward;
        public void Bind(AlarmSystem source)
        {
            alarm = source;
            button.onClick.RemoveListener(Provoke);
            button.onClick.AddListener(Provoke);
            Refresh();
        }
        private void Provoke() { alarm?.TryProvokeWave(); Refresh(); }
        public void Refresh()
        {
            if (button == null) return;
            bool available = alarm != null && alarm.CanProvoke;
            button.interactable = available;
            if (gameObject.activeSelf != available) gameObject.SetActive(available);
            int amount = alarm != null ? alarm.ProvokeReward : 0;
            if (amount != displayedReward)
            {
                displayedReward = amount;
                if (reward != null) reward.text = "+" + amount;
            }
            string currentLanguage = LocalizationService.Instance?.Language ?? "ru";
            if (language != currentLanguage)
            {
                language = currentLanguage;
                if (label != null) label.text = GameText.Get("Спровоцировать нападение");
            }
            var resource = PortalTowerProgression.Instance?.ExperienceCrystalResource;
            if (resource != null && crystalIcon != null && crystalIcon.sprite != resource.resourceIcon)
                ResourceIconSizing.Apply(crystalIcon, resource.resourceIcon);
        }
    }
}
