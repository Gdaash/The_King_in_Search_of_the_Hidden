using UnityEngine;
using UnityEngine.UI;
namespace GameFoundation.Base
{
    public sealed class RoyalDecreeRow : MonoBehaviour
    {
        public RoyalDecreeDefinition decree;
        public RoyalDecreePopupView popup;
        public Text title, description, state, buttonLabel, priceAmount;
        public Image priceIcon;
        public GameObject price;
        public Button toggle;
        private void OnEnable()
        {
            toggle.onClick.RemoveListener(Click); toggle.onClick.AddListener(Click);
            GlobalResourceManager.OnResourceChanged += ResourceChanged;
            RoyalDecreeService.Changed += DecreeChanged;
            Refresh();
        }
        private void OnDisable()
        {
            if (toggle) toggle.onClick.RemoveListener(Click);
            GlobalResourceManager.OnResourceChanged -= ResourceChanged;
            RoyalDecreeService.Changed -= DecreeChanged;
        }
        private void ResourceChanged(ResourceType type, int _) { if (decree != null && type == decree.influence) Refresh(); }
        private void DecreeChanged(string id, bool _) { if (decree != null && id == decree.id) Refresh(); }
        public void Refresh()
        {
            if (decree == null) return;
            bool active = RoyalDecreeService.IsEnabled(decree.id);
            title.text = decree.title; description.text = decree.description;
            state.text = active ? "Указ включён" : "Указ выключен";
            state.color = active ? new Color(.38f,.9f,.48f) : new Color(.66f,.61f,.72f);
            buttonLabel.text = active ? "Отключить" : "Включить";
            price.SetActive(!active);
            priceAmount.text = decree.activationCost.ToString();
            ResourceIconSizing.Apply(priceIcon, decree.influence != null ? decree.influence.resourceIcon : null);
            bool affordable = GlobalResourceManager.Instance != null && decree.influence != null &&
                GlobalResourceManager.Instance.GetResourceAmount(decree.influence) >= decree.activationCost;
            priceAmount.color = affordable ? new Color(.38f,.9f,.48f) : new Color(1,.38f,.38f);
            toggle.interactable = !ForestForagingService.IsPending && (active || affordable);
            var tint = toggle.GetComponent<GameFoundation.UI.ActionButtonLabelColor>();
            if (tint != null) tint.Negative = active;
        }
        public void Click()
        {
            if (decree == null || ForestForagingService.IsPending) return;
            if (RoyalDecreeService.IsEnabled(decree.id)) popup.ConfirmDisable(this);
            else RoyalDecreeService.TryEnable(decree);
            Refresh();
        }
    }
}
