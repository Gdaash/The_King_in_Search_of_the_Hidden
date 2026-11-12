using GameFoundation.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>One authored row represents all levels of a scientific upgrade.</summary>
    public sealed class LaboratoryUpgradeRow : MonoBehaviour
    {
        public string groupId;
        public Button purchaseButton;
        public Image border, surface, selection, lockIcon, costIcon;
        public Text titleLabel, statusLabel, requirementLabel, costLabel;
        public GameObject price;
        public UpgradeLevelPips levelPips;
        [Header("Состояния рамки")]
        public Color lockedColor = new(.29f, .25f, .33f);
        public Color unaffordableColor = new(.69f, .29f, .32f);
        public Color affordableColor = new(.38f, .65f, .44f);
        public Color purchasedColor = new(.68f, .58f, .36f);
        public ScientificUpgradeTable.Entry Entry { get; private set; }
        public bool IsComplete { get; private set; }
        public bool CanPurchase { get; private set; }
        public BaseDisabledButtonHint.Reason PurchaseBlock { get; private set; }
        private LaboratoryUpgradeList owner;
        private bool pipsResolved;
        private PopupChoiceRowStyle rowStyle;

        private void Awake() => purchaseButton.onClick.AddListener(Purchase);
        private void OnDestroy() => purchaseButton.onClick.RemoveListener(Purchase);
        public void Bind(LaboratoryUpgradeList view) => owner = view;
        public void Refresh(ScientificUpgradeTable.Entry entry, int bought, int total, GlobalStats stats)
        {
            Entry = entry;
            if (!pipsResolved)
            {
                // Older prefab variants contain an added indicator over the inherited one.
                var indicators = GetComponentsInChildren<UpgradeLevelPips>(true);
                if (levelPips == null && indicators.Length > 0) levelPips = indicators[0];
                foreach (var indicator in indicators)
                    if (indicator != levelPips) indicator.gameObject.SetActive(false);
                pipsResolved = true;
            }
            if (levelPips != null) levelPips.Display(bought, total);
            var portal = owner.portalProgression;
            var upgrade = portal != null ? portal.Balance.Find(entry.id) : null;
            IsComplete = portal != null ? portal.IsComplete(upgrade) : bought == total;
            bool unlocked = portal != null ? portal.IsUnlocked(upgrade) : stats.IsUpgradeUnlocked(entry);
            bool affordable = portal != null ? portal.CanPurchase(upgrade) : stats.CanPurchaseUpgrade(entry);
            lockIcon.color = unlocked || IsComplete ? new Color(.85f, .77f, .55f) : new Color(.64f, .59f, .68f);
            requirementLabel.text = (portal != null ? bought : entry.requiredPurchases).ToString();
            if (portal != null) ResourceIconSizing.Apply(lockIcon, upgrade.icon);
            titleLabel.text = owner.Title(entry) + "  " + LaboratoryUpgradeList.Tr("laboratory.level", GameFoundation.Localization.GameText.Get("ур.")) + entry.level;
            titleLabel.color = IsComplete ? purchasedColor : unlocked ? new Color(.94f, .91f, .82f) : new Color(.64f, .60f, .67f);
            (statusLabel.text, statusLabel.color) = PopupChoiceRowStyle.PurchaseStatus(IsComplete, !unlocked, affordable);
            if (total > 1 || total == 0) statusLabel.text += "  ·  " + bought + "/" + (total == 0 ? "∞" : total.ToString());
            price.SetActive(true);
            ResourceIconSizing.Apply(costIcon, portal != null ? portal.Balance.levelPointIcon : entry.costResource != null ? entry.costResource.resourceIcon : null);
            costIcon.enabled = costIcon.sprite != null;
            costLabel.text = entry.cost.ToString();
            bool sufficient = portal != null ? portal.LevelPoints > 0 : entry.costResource == null || GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(entry.costResource) >= entry.cost;
            costLabel.color = sufficient ? new Color(.55f, .85f, .35f) : new Color(1f, .3f, .35f);
            // Selection is always available; spending is exclusively the detail-panel action.
            CanPurchase = !IsComplete && unlocked && affordable;
            PurchaseBlock = IsComplete ? BaseDisabledButtonHint.Reason.Maximum : !unlocked ? BaseDisabledButtonHint.Reason.Research : BaseDisabledButtonHint.Reason.Resources;
            purchaseButton.interactable = true;
            if (purchaseButton.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = false;
            var actionColor = purchaseButton.GetComponent<GameFoundation.UI.ActionButtonLabelColor>();
            if (actionColor != null) actionColor.enabled = false;
        }
        public void SetSelected(bool selected)
        {
            if (rowStyle == null)
            {
                surface.enabled = false;
                selection.enabled = false;
                rowStyle = new PopupChoiceRowStyle(purchaseButton, border);
                if (owner.portalProgression == null)
                {
                    var card = border.rectTransform;
                    card.offsetMin = new Vector2(9, card.offsetMin.y);
                }
                MoveInside(lockIcon.rectTransform, new Vector2(34, 15));
                MoveInside(requirementLabel.rectTransform, new Vector2(34, -16));
                foreach (var rect in new[] { titleLabel.rectTransform, statusLabel.rectTransform })
                {
                    rect.anchoredPosition += new Vector2(42, 0);
                    rect.sizeDelta -= new Vector2(42, 0);
                }
                if (levelPips != null)
                    ((RectTransform)levelPips.transform).anchoredPosition += new Vector2(42, 0);
            }
            rowStyle.SetSelected(selected);
        }

        private void MoveInside(RectTransform rect, Vector2 position)
        {
            rect.SetParent(border.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
        }
        private void Purchase()
        {
            if (owner == null || Entry == null) return;
            owner.Select(this);

        }
    }
}
