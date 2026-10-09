using GameFoundation.MetaProgression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace GameFoundation.Base
{
    public sealed class DayReportRow : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private TMP_Text runLabel;
        [FormerlySerializedAs("gainedLabel")]
        [SerializeField] private TMP_Text shelterLabel;
        [SerializeField] private TMP_Text endLabel;

        public void SetData(ResourceType type, DayResourceLedger.Entry entry)
        {
            if (icon != null)
            {
                ResourceIconSizing.Apply(icon, type != null ? type.resourceIcon : null);
                icon.enabled = icon.sprite != null;
            }
            if (nameLabel != null) nameLabel.text = type != null && !string.IsNullOrWhiteSpace(type.DisplayName) ? type.DisplayName : entry.resource;
            if (startLabel != null) startLabel.text = entry.start.ToString();
            if (runLabel != null)
            {
                runLabel.text = entry.runChange > 0 ? $"+{entry.runChange}" : entry.runChange.ToString();
                runLabel.color = entry.runChange > 0 ? new Color(.38f, .9f, .48f) : entry.runChange < 0 ? new Color(1f, .38f, .38f) : Color.white;
            }
            if (shelterLabel != null)
            {
                long change = entry.ShelterChange;
                shelterLabel.text = change > 0 ? $"+{change}" : change < 0 ? $"−{-change}" : "0";
                shelterLabel.color = change > 0 ? new Color(.38f, .9f, .48f)
                    : change < 0 ? new Color(1f, .38f, .38f) : new Color(.66f, .61f, .72f);
            }
            if (endLabel != null)
            {
                long change = (long)entry.end - entry.start;
                string delta = change > 0 ? $"+{change}" : change < 0 ? $"−{-change}" : "0";
                Color tint = change > 0 ? new Color(.38f, .9f, .48f)
                    : change < 0 ? new Color(1f, .38f, .38f) : new Color(.66f, .61f, .72f);
                endLabel.richText = true;
                endLabel.text = $"{entry.end} <color=#{ColorUtility.ToHtmlStringRGB(tint)}>({delta})</color>";
            }
        }
    }
}
