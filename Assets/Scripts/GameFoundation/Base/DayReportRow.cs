using GameFoundation.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    public sealed class DayReportRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text runLabel;
        [SerializeField] private TMP_Text gainedLabel;
        [SerializeField] private TMP_Text spentLabel;
        [SerializeField] private TMP_Text endLabel;

        public void SetData(ResourceType type, DayResourceLedger.Entry entry)
        {
            if (icon != null)
            {
                ResourceIconSizing.Apply(icon, type != null ? type.defaultCarrySprite : null);
                icon.enabled = icon.sprite != null;
            }
            if (nameLabel != null) nameLabel.text = type != null && !string.IsNullOrWhiteSpace(type.resourceName) ? type.resourceName : entry.resource;
            if (startLabel != null) startLabel.text = entry.start.ToString();
            if (runLabel != null)
            {
                runLabel.text = entry.runChange > 0 ? $"+{entry.runChange}" : entry.runChange.ToString();
                runLabel.color = entry.runChange > 0 ? new Color(.38f, .9f, .48f) : entry.runChange < 0 ? new Color(1f, .38f, .38f) : Color.white;
            }
            if (gainedLabel != null) gainedLabel.text = entry.baseGained > 0 ? $"+{entry.baseGained}" : "0";
            if (spentLabel != null) spentLabel.text = entry.baseSpent > 0 ? $"−{entry.baseSpent}" : "0";
            if (endLabel != null) endLabel.text = entry.end.ToString();
        }
    }
}
