using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.UI
{
    /// <summary>Shared available-action treatment. The button visual retains its independent hover animation.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class BuildingButtonHighlight : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private ButtonVisualTheme theme;
        [SerializeField] private RisingLabelStars stars;
        private Button button;
        private bool available;
        public bool Available => available;
        public Sprite StarSprite => stars != null ? stars.StarSprite : null;
        public Color StarColor => theme != null ? theme.positiveLabelColor : Color.white;

        public void SetAvailable(bool value) { available = value; Refresh(); }
        private void OnEnable() { button = GetComponent<Button>(); Refresh(); }
        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (label == null || theme == null) return;
            if (button == null) button = GetComponent<Button>();
            bool clickable = button != null && button.IsInteractable();
            label.color = !clickable ? theme.unavailableLabelColor
                : available ? theme.positiveLabelColor : theme.neutralLabelColor;
            label.rectTransform.localScale = Vector3.one;
            if (stars != null) stars.Emit = clickable && available && label.isActiveAndEnabled;
            if (stars != null) stars.color = theme.positiveLabelColor;
        }
        private void OnDisable()
        {
            if (label != null) label.rectTransform.localScale = Vector3.one;
            if (stars != null) stars.Emit = false;
        }
    }
}
