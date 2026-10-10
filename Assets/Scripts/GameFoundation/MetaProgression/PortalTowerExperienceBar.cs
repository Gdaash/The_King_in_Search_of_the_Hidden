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
        public Material incomingFillMaterial;
        private Image incomingFill;
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
            if(incomingFill==null)
            {
                var go=new GameObject("Incoming XP",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                incomingFill=go.GetComponent<Image>();
                var rect=incomingFill.rectTransform;var source=fill.rectTransform;
                rect.SetParent(source.parent,false);
                rect.anchorMin=source.anchorMin;rect.anchorMax=source.anchorMax;rect.pivot=source.pivot;
                rect.sizeDelta=source.sizeDelta;rect.anchoredPosition=source.anchoredPosition;
                rect.localScale=source.localScale;rect.localRotation=source.localRotation;
                rect.SetSiblingIndex(source.GetSiblingIndex());
                incomingFill.sprite=fill.sprite;incomingFill.type=fill.type;
                incomingFill.material=incomingFillMaterial;
                incomingFill.fillMethod=fill.fillMethod;incomingFill.fillOrigin=fill.fillOrigin;
                incomingFill.fillClockwise=fill.fillClockwise;
                incomingFill.color=Color.white;incomingFill.raycastTarget=false;
            }
            incomingFill.fillAmount=Mathf.Clamp01((float)(progression.Experience+progression.PendingExperience)/progression.RequiredExperience);
            if(incomingFill.sprite==null)
            {
                incomingFill.rectTransform.anchorMax=new Vector2(incomingFill.fillAmount,1);
                incomingFill.rectTransform.offsetMin=incomingFill.rectTransform.offsetMax=Vector2.zero;
            }
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
