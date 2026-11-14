using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GameFoundation.MetaProgression
{
    public sealed partial class PortalArsenalPopup
    {
        private Button enhanceButton;
        private TMP_Text enhanceLabel;
        private void RefreshEnhancements(bool unlocked)
        {
            bool visible = IsBaseArsenal && unlocked && PortalWeaponEnhancements.Table?.Find(preview) != null;
            if (visible && enhanceButton == null) CreateEnhancements();
            if (enhanceButton == null) return;
            enhanceButton.gameObject.SetActive(visible);
            if (!visible) return;
            enhanceButton.interactable = PortalWeaponEnhancements.CanPurchase(preview);
            enhanceLabel.text = PortalWeaponEnhancements.Next(preview) == null ? T("enhance.maximum", "Все усиления изучены") : T("enhance.action", "Усилить");
            enhanceLabel.color = enhanceButton.interactable ? theme.positiveLabelColor : theme.popupSubtitleColor;
            var hint = PortalWeaponEnhancements.Hint(preview);
            GameFoundation.Base.BaseDisabledButtonHint.Set(enhanceButton, GameFoundation.Base.BaseDisabledButtonHint.Reason.Research, reasonText: hint, actionDetail: enhanceButton.interactable ? hint : null);
        }
        private void CreateEnhancements()
        {
            var original = (RectTransform)actionButton.transform;
            var root = new GameObject("Challenge enhancement", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)root.transform;
            rect.SetParent(original.parent, false);
            rect.anchorMin = original.anchorMin; rect.anchorMax = original.anchorMax;
            rect.pivot = original.pivot; rect.offsetMin = original.offsetMin; rect.offsetMax = original.offsetMax;
            var image = root.GetComponent<Image>(); image.sprite = theme.textButtonSprite; image.type = Image.Type.Sliced;
            enhanceButton = root.GetComponent<Button>(); enhanceButton.targetGraphic = image;
            enhanceButton.transition = Selectable.Transition.None;
            root.AddComponent<UnifiedButtonFeedback>();
            enhanceLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            enhanceLabel.transform.SetParent(rect, false);
            enhanceLabel.rectTransform.anchorMin = Vector2.zero; enhanceLabel.rectTransform.anchorMax = Vector2.one;
            enhanceLabel.rectTransform.offsetMin = new Vector2(14, 2); enhanceLabel.rectTransform.offsetMax = new Vector2(-14, -2);
            enhanceLabel.font = actionLabel.font; enhanceLabel.fontSize = 22;
            enhanceLabel.alignment = TextAlignmentOptions.Center; enhanceLabel.raycastTarget = false;
            enhanceButton.onClick.AddListener(() =>
            {
                if (PortalWeaponEnhancements.TryPurchase(preview)) GameFoundation.Audio.GameAudioController.PlayUI(GameFoundation.Audio.GameAudioCue.ContentUnlock);
                Refresh();
            });
        }
    }
}
