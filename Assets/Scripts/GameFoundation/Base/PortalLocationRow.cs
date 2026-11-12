using GameFoundation.MetaProgression;
using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class PortalLocationRow : MonoBehaviour
    {
        public PortalLocationDefinition location;
        public Button button;
        public Image surface, border, selection, lockIcon;
        public Text title, status;
        public GameObject demoOverlay;
        private PortalPopupView owner;
        private PopupChoiceRowStyle rowStyle;
        private PopupChoicePrice priceA, priceB;
        private Text requiredUpgrades;

        private void OnEnable()
        {
            button.onClick.RemoveListener(Choose);
            button.onClick.AddListener(Choose);
        }
        public void Bind(PortalPopupView view) => owner = view;
        private void Choose() => owner?.Select(location);
        private void OnDisable() => button.onClick.RemoveListener(Choose);

        public void Refresh(PortalLocationDefinition current)
        {
            // Locked destinations remain selectable so their requirements can be inspected.
            button.interactable = true;
            if (rowStyle == null)
            {
                surface.enabled = false;
                if (selection != null) selection.enabled = false;
                status.fontSize = 22;
                title.fontSize = Mathf.Max(22, title.fontSize - 1);
                rowStyle = new PopupChoiceRowStyle(button, border);
                ConfigureDetails();
            }
            bool unlocked = PortalProgression.IsUnlocked(location);
            bool next = PortalProgression.IsNextDestination(location);
            bool affordable = next && BuildingUpgradeService.CanUpgrade(PortalProgression.BuildingId);
            title.text = PortalProgression.Name(location);
            title.color = unlocked || next ? new Color(.94f, .91f, .82f) : new Color(.68f, .63f, .72f);
            (status.text, status.color) = PopupChoiceRowStyle.PurchaseStatus(unlocked, !next, affordable, portal: true);
            if (lockIcon != null) lockIcon.enabled = !unlocked;
            requiredUpgrades.gameObject.SetActive(!unlocked);
            requiredUpgrades.text = Mathf.Max(0, location.RequiredPortalLevel - PortalProgression.Level - 1).ToString();
            var price = unlocked ? null : PortalProgression.AccessPrice(location);
            priceA.Set(price?.resourceA, price?.costA ?? 0);
            priceB.Set(price?.resourceB, price?.costB ?? 0);
            float rightInset = price == null ? 18 : 174;
            PopupChoiceRowStyle.PlaceCaptions(title, status, 64, rightInset, rightInset);
            if (button.TryGetComponent<BaseDisabledButtonHint>(out var hint)) hint.enabled = false;
            if (demoOverlay != null) demoOverlay.SetActive(false);
            rowStyle.SetSelected(current == location);
        }

        private void ConfigureDetails()
        {
            var root = (RectTransform)button.transform;
            var font = Resources.Load<TMP_FontAsset>("Fonts/Pixellari Reading UI");
            priceA = new PopupChoicePrice(root, font, 98);
            priceB = new PopupChoicePrice(root, font, 18);
            if (lockIcon != null)
            {
                lockIcon.sprite = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme").lockIconSprite;
                ResourceIconSizing.Apply(lockIcon, lockIcon.sprite);
                PlaceLeft(lockIcon.rectTransform, 34, 12);
            }
            requiredUpgrades = new GameObject("Previous upgrades", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            requiredUpgrades.transform.SetParent(root, false);
            requiredUpgrades.rectTransform.sizeDelta = new Vector2(38, 24);
            PlaceLeft(requiredUpgrades.rectTransform, 34, -16);
            requiredUpgrades.font = status.font;
            requiredUpgrades.fontSize = 22;
            requiredUpgrades.alignment = TextAnchor.MiddleCenter;
            requiredUpgrades.raycastTarget = false;
            QuestReadingText.Ensure(requiredUpgrades, font);
        }

        private static void PlaceLeft(RectTransform rect, float x, float y)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
            rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = new Vector2(x, y);
        }
    }
}
