using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>One authored row represents all levels of a scientific upgrade.</summary>
    public sealed class LaboratoryUpgradeRow : MonoBehaviour, IPointerEnterHandler
    {
        public string groupId;
        public Button purchaseButton;
        public Image border, surface, selection, lockIcon, costIcon;
        public Text titleLabel, statusLabel, requirementLabel, costLabel;
        public GameObject price;
        [Header("Состояния рамки")]
        public Color lockedColor = new(.29f, .25f, .33f);
        public Color unaffordableColor = new(.69f, .29f, .32f);
        public Color affordableColor = new(.38f, .65f, .44f);
        public Color purchasedColor = new(.68f, .58f, .36f);
        public ScientificUpgradeTable.Entry Entry { get; private set; }
        public bool IsComplete { get; private set; }
        private LaboratoryUpgradeList owner;

        private void Awake() => purchaseButton.onClick.AddListener(Purchase);
        private void OnDestroy() => purchaseButton.onClick.RemoveListener(Purchase);
        public void Bind(LaboratoryUpgradeList view) => owner = view;
        public void Refresh(ScientificUpgradeTable.Entry entry, int bought, int total, GlobalStats stats)
        {
            Entry = entry;
            IsComplete = bought == total;
            bool unlocked = stats.IsUpgradeUnlocked(entry);
            bool affordable = stats.CanPurchaseUpgrade(entry);
            Color tint = IsComplete ? purchasedColor : !unlocked ? lockedColor : affordable ? affordableColor : unaffordableColor;
            border.color = tint;
            lockIcon.color = unlocked || IsComplete ? new Color(.85f, .77f, .55f) : new Color(.64f, .59f, .68f);
            requirementLabel.text = entry.requiredPurchases.ToString();
            titleLabel.text = owner.Title(entry) + "  " + LaboratoryUpgradeList.Tr("laboratory.level", "ур.") + entry.level;
            titleLabel.color = IsComplete ? purchasedColor : unlocked ? new Color(.94f, .91f, .82f) : new Color(.64f, .60f, .67f);
            statusLabel.text = IsComplete ? LaboratoryUpgradeList.Tr("laboratory.complete", "Изучено") :
                !unlocked ? LaboratoryUpgradeList.Tr("laboratory.locked", "Заблокировано") :
                affordable ? LaboratoryUpgradeList.Tr("laboratory.available", "Можно изучить") : LaboratoryUpgradeList.Tr("laboratory.no_resources", "Не хватает ресурсов");
            if (total > 1) statusLabel.text += "  ·  " + bought + "/" + total;
            statusLabel.color = !unlocked && !IsComplete ? new Color(.64f, .59f, .68f) : tint;
            price.SetActive(true);
            ResourceIconSizing.Apply(costIcon, entry.costResource != null ? entry.costResource.resourceIcon : null);
            costIcon.enabled = costIcon.sprite != null;
            costLabel.text = entry.cost.ToString();
            costLabel.color = IsComplete ? new Color(.64f, .59f, .68f) : new Color(.94f, .91f, .82f);
            // A locked row still receives hover for its explanation.
            purchaseButton.interactable = !IsComplete && unlocked;
            var actionColor = purchaseButton.GetComponent<GameFoundation.UI.ActionButtonLabelColor>();
            if (actionColor != null) actionColor.ActionAvailable = !IsComplete && affordable && unlocked;
        }
        public void SetSelected(bool selected)
        {
            selection.enabled = selected;
            surface.color = selected ? new Color(.20f, .16f, .24f) : new Color(.16f, .13f, .19f);
        }
        public void OnPointerEnter(PointerEventData eventData) => owner?.Select(this);
        private void Purchase()
        {
            if (owner == null || Entry == null) return;
            owner.Select(this);
            owner.Purchase(Entry.id);
        }
    }
}
