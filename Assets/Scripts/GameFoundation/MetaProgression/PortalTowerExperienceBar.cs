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
        private CanvasGroup progressionVisibility;
        private Image incomingFill;
        private Image debtFill;
        private Material debtMaterial;
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
        private void OnDestroy()
        {
            if (debtMaterial != null) Destroy(debtMaterial);
        }
        private void Refresh()
        {
            if (progressionVisibility == null) progressionVisibility = GetComponent<CanvasGroup>();
            if (progressionVisibility == null) progressionVisibility = gameObject.AddComponent<CanvasGroup>();
            progressionVisibility.alpha = progression.CrystalTechnologyAvailable ? 1 : 0;
            progressionVisibility.interactable = progression.CrystalTechnologyAvailable;
            progressionVisibility.blocksRaycasts = progression.CrystalTechnologyAvailable;
            if (!progression.CrystalTechnologyAvailable) return;
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
            if (debtFill == null)
            {
                var go = new GameObject("Crystal Debt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                debtFill = go.GetComponent<Image>();
                var rect = debtFill.rectTransform; var source = fill.rectTransform;
                rect.SetParent(source.parent, false);
                rect.anchorMin = source.anchorMin; rect.anchorMax = source.anchorMax; rect.pivot = source.pivot;
                rect.sizeDelta = source.sizeDelta; rect.anchoredPosition = source.anchoredPosition;
                rect.localScale = source.localScale; rect.localRotation = source.localRotation;
                rect.SetSiblingIndex(source.GetSiblingIndex() + 1);
                debtFill.sprite = fill.sprite; debtFill.type = fill.type;
                debtFill.fillMethod = fill.fillMethod; debtFill.fillOrigin = fill.fillOrigin;
                debtFill.fillClockwise = fill.fillClockwise; debtFill.raycastTarget = false;
                if (incomingFillMaterial != null)
                {
                    debtMaterial = new Material(incomingFillMaterial);
                    debtMaterial.SetColor("_Color", new Color(.65f, .65f, .65f, 1));
                    debtMaterial.name = "Portal crystal debt tint";
                    debtFill.material = debtMaterial;
                }
                else debtFill.color = new Color(.65f,.65f,.65f,1);
            }
            debtFill.fillAmount = Mathf.Clamp01((float)progression.CrystalDebt / progression.RequiredExperience);
            debtFill.enabled = progression.CrystalDebt > 0;
            if (debtFill.sprite == null)
            {
                debtFill.rectTransform.anchorMax = new Vector2(debtFill.fillAmount, 1);
                debtFill.rectTransform.offsetMin = debtFill.rectTransform.offsetMax = Vector2.zero;
            }
            incomingFill.fillAmount=Mathf.Clamp01((float)progression.PreviewExperience(progression.PendingExperience)/progression.RequiredExperience);
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
            label.text = LaboratoryUpgradeList.Tr("portal_tower.experience", GameFoundation.Localization.GameText.Get("Башня портала")) + "  " +
                LaboratoryUpgradeList.Tr("laboratory.level", GameFoundation.Localization.GameText.Get("ур.")) + progression.Level + "   " + progression.Experience + " / " + progression.RequiredExperience;
            if (progression.CrystalDebt > 0)
                label.text += (language != null && language.Language == "en" ? "  Debt: " : "  Долг: ") + progression.CrystalDebt;
        }
    }
}
