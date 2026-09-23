using GameFoundation.MetaProgression;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class PortalSiteButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image portalIcon;
        [SerializeField] private Image costIcon;
        [SerializeField] private Text titleText;
        [SerializeField] private Text costText;

        [Header("Цвета")]
        [SerializeField] private Color titleColor = new Color(0.94f, 0.91f, 0.82f);
        [SerializeField] private Color enoughResourceColor = new Color(0.55f, 0.85f, 0.35f);
        [SerializeField] private Color notEnoughResourceColor = new Color(0.95f, 0.38f, 0.35f);
        [SerializeField] private Color buttonNormalColor = Color.white;
        [SerializeField] private Color buttonHighlightedColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField] private Color buttonPressedColor = new Color(0.78f, 0.72f, 0.78f);
        [SerializeField] private Color buttonDisabledColor = new Color(0.42f, 0.39f, 0.43f);

        public void Refresh(PortalSite site, ResourceType ore, int availableOre, bool enteredToday,
            string levelLabel, string enterLabel)
        {
            if (site == null) return;
            int price = site.difficulty * 5;
            bool canPay = ore != null && availableOre >= price;
            if (button != null)
            {
                button.interactable = !enteredToday && (site.active || canPay);
                var colors = button.colors;
                colors.normalColor = buttonNormalColor;
                colors.highlightedColor = buttonHighlightedColor;
                colors.pressedColor = buttonPressedColor;
                colors.disabledColor = buttonDisabledColor;
                button.colors = colors;
            }
            if (titleText != null)
            {
                titleText.text = site.active ? enterLabel + " · " + site.difficulty : levelLabel + " " + site.difficulty;
                titleText.color = titleColor;
                titleText.rectTransform.anchoredPosition = new Vector2(0f, site.active ? 0f : 23f);
            }
            if (costIcon != null)
            {
                costIcon.gameObject.SetActive(!site.active);
                if (ore != null) ResourceIconSizing.Apply(costIcon, ore.resourceIcon);
            }
            if (costText != null)
            {
                costText.gameObject.SetActive(!site.active);
                costText.text = price.ToString();
                costText.color = canPay ? enoughResourceColor : notEnoughResourceColor;
            }
            if (portalIcon != null) portalIcon.color = Color.white;
        }
    }
}
