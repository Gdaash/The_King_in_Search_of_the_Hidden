using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class WarehouseCartPurchaseView : MonoBehaviour
    {
        [Header("Изготовление телеги")]
        [SerializeField] private ResourceType wood;
        [SerializeField] private ResourceType cart;
        public const int DefaultWoodCost = 6;
        [SerializeField, Min(1)] private int woodCost = DefaultWoodCost;

        [Header("Элементы окна")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Text buyLabelText;
        [SerializeField] private Image woodIcon;
        [SerializeField] private Text woodCostText;
        [SerializeField] private Image cartIcon;
        [SerializeField] private Text cartAmountText;

        [Header("Цвета цены")]
        [SerializeField] private Color enoughWoodColor = new Color(0.55f, 0.85f, 0.35f);
        [SerializeField] private Color notEnoughWoodColor = new Color(0.95f, 0.38f, 0.35f);

        [Header("Остальные цвета")]
        [SerializeField] private Color buyLabelColor = new Color(0.94f, 0.91f, 0.82f);
        [SerializeField] private Color cartAmountColor = new Color(0.94f, 0.91f, 0.82f);
        [SerializeField] private Color woodIconColor = Color.white;
        [SerializeField] private Color cartIconColor = Color.white;
        [SerializeField] private Color buttonNormalColor = Color.white;
        [SerializeField] private Color buttonHighlightedColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField] private Color buttonPressedColor = new Color(0.78f, 0.72f, 0.78f);
        [SerializeField] private Color buttonDisabledColor = new Color(0.42f, 0.39f, 0.43f);

        public int WoodCost => woodCost;
        public bool CanBuyCart => CraftProduction.CanQueue(CraftProduction.Carpentry, wood, woodCost);

        public bool TryBuy() => CraftProduction.Queue(CraftProduction.Carpentry, wood, woodCost, cart, 1, 2);

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        }

        private void OnResourceChanged(ResourceType type, int _)
        {
            if (type == wood || type == cart)
                Refresh();
        }

        internal void RefreshProduction() => Refresh();

        private void Refresh()
        {
            GlobalResourceManager manager = GlobalResourceManager.Instance;
            bool canBuy = CanBuyCart;

            ApplyPalette();
            if (buyButton != null) buyButton.interactable = canBuy;
            BaseDisabledButtonHint.Set(buyButton, BaseDisabledButtonHint.Reason.Resources,
                reasonText: CraftProduction.StartBlockReason(CraftProduction.Carpentry));
            if (woodCostText != null)
            {
                woodCostText.text = woodCost.ToString();
                woodCostText.color = canBuy ? enoughWoodColor : notEnoughWoodColor;
            }
            if (cartAmountText != null)
                cartAmountText.text = manager != null && cart != null
                    ? manager.GetResourceAmount(cart).ToString()
                    : "0";
            if (woodIcon != null && wood != null) ResourceIconSizing.Apply(woodIcon, wood.resourceIcon);
            if (cartIcon != null && cart != null) ResourceIconSizing.Apply(cartIcon, cart.resourceIcon);
        }

        private void OnValidate()
        {
            if (woodCostText != null) woodCostText.text = woodCost.ToString();
            if (woodIcon != null && wood != null) ResourceIconSizing.Apply(woodIcon, wood.resourceIcon);
            if (cartIcon != null && cart != null) ResourceIconSizing.Apply(cartIcon, cart.resourceIcon);
            ApplyPalette();
        }

        private void ApplyPalette()
        {
            if (buyLabelText != null) buyLabelText.color = buyLabelColor;
            if (cartAmountText != null) cartAmountText.color = cartAmountColor;
            if (woodIcon != null) woodIcon.color = woodIconColor;
            if (cartIcon != null) cartIcon.color = cartIconColor;
            if (buyButton == null) return;

            ColorBlock colors = buyButton.colors;
            colors.normalColor = buttonNormalColor;
            colors.highlightedColor = buttonHighlightedColor;
            colors.pressedColor = buttonPressedColor;
            colors.disabledColor = buttonDisabledColor;
            buyButton.colors = colors;
        }
    }
}
