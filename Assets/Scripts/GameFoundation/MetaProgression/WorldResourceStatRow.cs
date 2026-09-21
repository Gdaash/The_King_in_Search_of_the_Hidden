using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    public sealed class WorldResourceStatRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text resourceLabel;
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private TMP_Text afterLabel;
        [SerializeField] private TMP_Text changeLabel;

        public void SetData(ResourceType resource, int before, int after)
        {
            if (icon != null)
            {
                icon.sprite = resource.defaultCarrySprite;
                icon.enabled = icon.sprite != null;
            }
            if (resourceLabel != null)
                resourceLabel.text = string.IsNullOrWhiteSpace(resource.resourceName) ? resource.name : resource.resourceName;
            if (amountLabel != null) amountLabel.text = before.ToString();
            if (afterLabel != null) afterLabel.text = after.ToString();
            if (changeLabel == null) return;
            int value = after - before;
            changeLabel.text = value > 0 ? $"+{value}" : value.ToString();
            changeLabel.color = value > 0 ? new Color(0.38f, 0.9f, 0.48f)
                : value < 0 ? new Color(1f, 0.38f, 0.38f)
                : new Color(0.7f, 0.75f, 0.8f);
        }
    }
}
