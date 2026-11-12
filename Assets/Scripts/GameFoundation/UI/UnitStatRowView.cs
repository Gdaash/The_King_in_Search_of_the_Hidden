using System;
using GameFoundation.Combat;
using System.Globalization;
using UnityEngine;

namespace GameFoundation.UI
{
    public sealed class UnitStatRowView : MonoBehaviour
    {
        [Serializable]
        private sealed class DefenseCell
        {
            public DamageType type;
            public UnityEngine.UI.Image icon;
            public UnityEngine.UI.Text amount;
        }
        [SerializeField] private UnityEngine.UI.Image icon;
        [SerializeField] private UnityEngine.UI.Text label;
        [SerializeField] private UnityEngine.UI.Text value;
        [SerializeField] private UnityEngine.UI.Image background;
        [SerializeField] private Color statIconColor = new(.78f, .7f, .88f);
        [SerializeField] private RectTransform defenseGroup;
        [SerializeField] private DefenseCell[] defenseCells;
        [SerializeField] private float normalLabelRightInset = 194f;
        [SerializeField] private float defenseLabelRightInset = 406f;

        public void Present(Sprite sprite, string caption, string amount, Color valueColor, Color stripe, bool resourceIcon = false, bool defense = false)
        {
            if (icon != null)
            {
                ResourceIconSizing.Apply(icon, sprite);
                icon.enabled = sprite != null;
                icon.color = resourceIcon ? Color.white : statIconColor;
            }
            if (defenseGroup != null) defenseGroup.gameObject.SetActive(defense);
            if (label != null)
            {
                label.text = caption;
                label.rectTransform.offsetMax = new Vector2(-(defense ? defenseLabelRightInset : normalLabelRightInset), 0f);
            }
            if (value != null) { value.gameObject.SetActive(!defense); if (amount.Contains("<color=")) value.supportRichText = true; value.text = amount; value.color = valueColor; }
            if (background != null) background.color = stripe;
        }

        public void PresentDefense(Sprite shield, string caption, GlobalStats stats, Color positive, Color negative, Color neutral, Color stripe, Combatant combat=null)
        {
            Present(shield, caption, (combat != null ? combat.Armor : 0).ToString("0.#", CultureInfo.InvariantCulture), neutral, stripe);
        }
    }
}
