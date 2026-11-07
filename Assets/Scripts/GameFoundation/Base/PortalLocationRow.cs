using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class PortalLocationRow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
    {
        public PortalLocationDefinition location;
        public Button button;
        public Image surface, border, selection, lockIcon;
        public Text title, status;
        public GameObject demoOverlay;
        public Color selectedColor = new(.74f, .64f, .42f);
        public Color normalColor = new(.35f, .30f, .39f);
        private PortalPopupView owner;
        private bool hovering;
        private bool selected;
        private GameFoundation.UI.PriceButtonLayout priceLayout;
        private readonly Image[] priceIcons = new Image[2];
        private readonly Text[] priceAmounts = new Text[2];

        private void OnEnable()
        {
            button.onClick.RemoveListener(Choose);
            button.onClick.AddListener(Choose);
        }
        public void Bind(PortalPopupView view) => owner = view;
        private void Choose()
        {
            if (owner == null) return;
            if (PortalProgression.IsUnlocked(location) || PortalProgression.TryUnlockDestination(location))
                owner.Select(location);
        }
        public void OnSelect(BaseEventData _) => owner?.Select(location);
        public void OnPointerEnter(PointerEventData _) { hovering = true; UpdateColor(); }
        public void OnPointerExit(PointerEventData _) { hovering = false; UpdateColor(); }
        private void OnDisable() { button.onClick.RemoveListener(Choose); hovering = false; }

        public void Refresh(PortalLocationDefinition current)
        {
            selected = current == location;
            bool unlocked = PortalProgression.IsUnlocked(location);
            bool next = PortalProgression.IsNextDestination(location);
            bool affordable = next && BuildingUpgradeService.CanUpgrade(PortalProgression.BuildingId);
            title.text = PortalProgression.Name(location);
            title.color = unlocked || next ? new Color(.94f, .91f, .82f) : new Color(.68f, .63f, .72f);
            status.text = unlocked ? PortalPopupView.Tr("base.portal.available", "Доступно") : PortalPopupView.Tr("base.portal.unlock_location", "Открыть");
            status.color = unlocked || affordable ? new Color(.58f, .77f, .52f) : new Color(.68f, .63f, .72f);
            if (lockIcon != null) lockIcon.enabled = !unlocked && !next;
            if (selection != null) selection.enabled = false;
            button.interactable = unlocked || affordable;
            BaseDisabledButtonHint.Set(button, next ? BaseDisabledButtonHint.Reason.Resources : BaseDisabledButtonHint.Reason.PortalLevel);
            if (demoOverlay != null) demoOverlay.SetActive(false);
            var price = unlocked ? null : PortalProgression.AccessPrice(location);
            EnsurePriceLayout();
            SetPrice(0, price?.resourceA, price?.costA ?? 0);
            SetPrice(1, price?.resourceB, price?.costB ?? 0);
            priceLayout.Refresh();
            UpdateColor();
        }

        private void EnsurePriceLayout()
        {
            if (priceLayout != null) return;
            if (lockIcon != null)
            {
                var lockRect = lockIcon.rectTransform;
                lockRect.anchorMin = lockRect.anchorMax = new Vector2(1, .5f);
                lockRect.pivot = new Vector2(.5f, .5f);
                lockRect.anchoredPosition = new Vector2(-31, 20);
            }
            var line = new GameObject("Access price", typeof(RectTransform)).GetComponent<RectTransform>();
            line.SetParent(transform, false);
            line.anchorMin = new Vector2(0, .5f);
            line.anchorMax = new Vector2(1, .5f);
            line.sizeDelta = new Vector2(0, 46);
            line.anchoredPosition = new Vector2(0, -15);
            status.transform.SetParent(line, false);
            priceLayout = gameObject.AddComponent<GameFoundation.UI.PriceButtonLayout>();
            priceLayout.visual = line;
            priceLayout.label = status;
            priceLayout.prices = new RectTransform[4];
            for (int i = 0; i < priceIcons.Length; i++)
            {
                var icon = new GameObject("Resource", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                icon.transform.SetParent(line, false);
                icon.raycastTarget = false;
                priceIcons[i] = icon;
                var amount = new GameObject("Amount", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
                amount.transform.SetParent(line, false);
                amount.font = status.font;
                amount.raycastTarget = false;
                GameFoundation.UI.QuestReadingText.Ensure(amount);
                priceAmounts[i] = amount;
                priceLayout.prices[2 * i] = icon.rectTransform;
                priceLayout.prices[2 * i + 1] = amount.rectTransform;
            }
        }

        private void SetPrice(int index, ResourceType resource, int cost)
        {
            var icon = priceIcons[index];
            var amount = priceAmounts[index];
            bool visible = resource != null && cost > 0;
            icon.enabled = amount.enabled = visible;
            if (!visible) return;
            ResourceIconSizing.Apply(icon, resource.resourceIcon);
            amount.text = cost.ToString();
            bool enough = GlobalResourceManager.Instance != null && GlobalResourceManager.Instance.GetResourceAmount(resource) >= cost;
            amount.color = enough ? new Color(.55f, .85f, .47f) : new Color(1f, .35f, .36f);
        }

        private void UpdateColor()
        {
            border.color = selected ? selectedColor : hovering ? Color.Lerp(normalColor, selectedColor, .5f) : normalColor;
            surface.color = selected || hovering ? new Color(.20f, .16f, .24f) : new Color(.14f, .115f, .17f);
        }
    }
}
