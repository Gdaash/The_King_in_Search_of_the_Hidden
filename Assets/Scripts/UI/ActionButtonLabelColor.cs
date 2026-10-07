using UnityEngine;
using UnityEngine.UI;
namespace GameFoundation.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ActionButtonLabelColor : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private TMPro.TMP_Text tmpLabel;
        [SerializeField] private ButtonVisualTheme theme;
        [SerializeField] private bool negative;
        [SerializeField] private bool neutral;
        private Button button;
        public bool ActionAvailable { get; set; } = true;
        public bool Negative { get => negative; set { negative = value; Refresh(); } }
        private void OnEnable() { button = GetComponent<Button>(); Refresh(); }
        private void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (button == null) button = GetComponent<Button>();
            if (theme == null) return;
            var color = !button.IsInteractable() || !ActionAvailable ? theme.unavailableLabelColor
                : neutral ? theme.neutralLabelColor : negative ? theme.negativeLabelColor : theme.positiveLabelColor;
            if (label != null) label.color = color;
            if (tmpLabel != null) tmpLabel.color = color;
        }
    }
}
