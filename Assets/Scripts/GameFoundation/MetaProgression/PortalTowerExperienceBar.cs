using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameFoundation.Base;

namespace GameFoundation.MetaProgression
{
    public sealed class PortalTowerExperienceBar : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PortalTowerProgression progression;
        public Image fill;
        public Text label;
        public Material incomingFillMaterial;
        public RectTransform energyMarkers;
        public Sprite energyDividerSprite;
        private readonly System.Collections.Generic.List<Image> energyCrystals = new();
        private readonly System.Collections.Generic.List<RectTransform> energyTicks = new();
        public RectTransform TopCrystal { get; private set; }
        private CanvasGroup progressionVisibility;
        private Image incomingFill;
        private Image debtFill;
        private Material debtMaterial;
        private GameFoundation.Localization.LocalizationService language;
        private void OnEnable()
        {
            progression.Changed += Refresh;
            progression.EnergyChanged += Refresh;
            progression.EnergyLightCountChanged += ShowLightChange;
            language = GameFoundation.Localization.LocalizationService.Instance;
            if (language != null) language.LanguageChanged += Refresh;
            Refresh();
        }
        private void OnDisable()
        {
            progression.Changed -= Refresh;
            progression.EnergyChanged -= Refresh;
            progression.EnergyLightCountChanged -= ShowLightChange;
            if (language != null) language.LanguageChanged -= Refresh;
        }
        private void ShowLightChange(int change) =>
            AlarmSystem.Instance?.ShowEnergyLightNotification(change, progression.LightCount, progression.ExperienceCrystalResource?.resourceIcon, (RectTransform)transform, TopCrystal);

        private void Start()
        {
            if (language == null)
            {
                language = GameFoundation.Localization.LocalizationService.Instance;
                if (language != null) language.LanguageChanged += Refresh;
            }
            Refresh();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!progression.UsesEnergyLoadout || !progression.CrystalTechnologyAvailable) return;
            // Pointer-enter also bubbles through the bar when a crystal is hovered.
            // Keep the crystal's threshold tooltip instead of replacing it with the total.
            if (eventData?.pointerCurrentRaycast.gameObject != null &&
                eventData.pointerCurrentRaycast.gameObject.GetComponent<ResourceTooltipTrigger>() != null) return;
            var amount = string.Format(PortalArsenalPopup.T("energy.value", "Энергия: {0} / {1}"),
                progression.Energy, progression.EnergyCapacity);
            TooltipManager.Instance?.Show(amount + "\n" + string.Format(
                PortalArsenalPopup.T("energy.conversion", "1 кристалл = {0} ед. энергии"),
                progression.Balance.energyPerCrystal), (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (progression.UsesEnergyLoadout) TooltipManager.Instance?.Hide();
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
            label.gameObject.SetActive(false);
            if (TryGetComponent<Image>(out var frame)) frame.raycastTarget = progression.UsesEnergyLoadout;
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
            if (progression.UsesEnergyLoadout) { RefreshEnergy(); return; }
            if (energyMarkers != null) energyMarkers.gameObject.SetActive(false);
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
                label.text += GameFoundation.Localization.GameText.Get("  Долг: ") + progression.CrystalDebt;
        }
        private void RefreshEnergy()
        {
            debtFill.enabled = false;
            SetFill(fill, (float)progression.Energy / progression.EnergyCapacity);
            SetFill(incomingFill, Mathf.Clamp01((float)(progression.Energy + progression.PendingExperience * progression.Balance.energyPerCrystal) / progression.EnergyCapacity));
            if (energyMarkers == null) return;
            energyMarkers.gameObject.SetActive(true);
            var icon = progression.ExperienceCrystalResource?.resourceIcon;
            if (energyTicks.Count == 0)
            {
                var end = CreateEnergyTick("Capacity tick", false);
                end.anchorMin = end.anchorMax = new Vector2(1, .5f);
                end.anchoredPosition = Vector2.zero;
                for (int i = 0; i < WorldFlashlightAvailability.MaximumCount - 1; i++)
                {
                    var root = CreateEnergyTick("Light threshold " + (i + 1), true);
                    var crystal = new GameObject("Crystal", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                    crystal.transform.SetParent(root, false); crystal.raycastTarget = true;
                    crystal.gameObject.AddComponent<ResourceTooltipTrigger>();
                    ResourceIconSizing.Apply(crystal, icon); crystal.rectTransform.anchoredPosition = new Vector2(0,28);
                    energyTicks.Add(root); energyCrystals.Add(crystal);
                }
            }
            TopCrystal = null;
            for (int i = 0; i < energyTicks.Count; i++)
            {
                int threshold = PortalEnergyRules.LightThreshold(progression.Balance, i, progression.CrystalEfficiencyRank);
                var crystalRect = energyCrystals[i].rectTransform;
                energyCrystals[i].GetComponent<ResourceTooltipTrigger>().Initialize(
                    string.Format(PortalArsenalPopup.T("energy.threshold", "Энергия для зажигания: {0}"), threshold), crystalRect);
                energyTicks[i].gameObject.SetActive(threshold <= progression.EnergyCapacity);
                energyTicks[i].anchorMin = energyTicks[i].anchorMax = new Vector2((float)threshold / progression.EnergyCapacity,.5f);
                energyTicks[i].anchoredPosition = Vector2.zero;
                if (TopCrystal == null && i >= progression.PermanentLightCount - 1 && energyTicks[i].gameObject.activeSelf)
                    TopCrystal = energyCrystals[i].rectTransform;
                energyCrystals[i].color = progression.Energy >= threshold ? Color.white : new Color(.4f,.36f,.46f);
            }
        }
        private RectTransform CreateEnergyTick(string name, bool flipped)
        {
            var root = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(energyMarkers, false);
            root.sizeDelta = Vector2.zero;
            var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            divider.transform.SetParent(root, false);
            divider.sprite = energyDividerSprite;
            divider.raycastTarget = false;
            divider.rectTransform.sizeDelta = energyDividerSprite != null ? energyDividerSprite.rect.size * 2 : new Vector2(2, 24);
            divider.rectTransform.localScale = new Vector3(1, flipped ? -1 : 1, 1);
            return root;
        }

        private static void SetFill(Image image, float amount)
        {
            image.fillAmount = amount;
            if (image.sprite == null) { image.rectTransform.anchorMax = new Vector2(amount,1); image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero; }
        }
    }
}
