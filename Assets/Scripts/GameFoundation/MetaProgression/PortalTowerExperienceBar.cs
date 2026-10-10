using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Base;

namespace GameFoundation.MetaProgression
{
    public sealed class PortalTowerExperienceBar : MonoBehaviour
    {
        public PortalTowerProgression progression;
        public Image fill;
        public Text label;
        private GameFoundation.Localization.LocalizationService language;
        private void OnEnable()
        {
            progression.Changed += Refresh;
            language = GameFoundation.Localization.LocalizationService.Instance;
            if (language != null) language.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            progression.Changed -= Refresh;
            if (language != null) language.LanguageChanged -= Refresh;
        }
        private void Start()
        {
            if (language == null)
            {
                language = GameFoundation.Localization.LocalizationService.Instance;
                if (language != null) language.LanguageChanged += Refresh;
            }
            Refresh();
        }
        private void Refresh()
        {
            fill.fillAmount = (float)progression.Experience / progression.RequiredExperience;
            // A sprite-free Image does not respect Filled geometry; resize the flat pixel track.
            if (fill.sprite == null)
            {
                fill.rectTransform.anchorMax = new Vector2(fill.fillAmount, 1);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            }
            label.text = LaboratoryUpgradeList.Tr("portal_tower.experience", "Башня портала") + "  " +
                LaboratoryUpgradeList.Tr("laboratory.level", "ур.") + progression.Level + "   " + progression.Experience + " / " + progression.RequiredExperience;
        }
    }
}
