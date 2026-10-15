using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Quests
{
    public sealed class QuestPanelExpansion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform detailsContent;
        [SerializeField] private LayoutElement detailsLayout;
        [SerializeField] private CanvasGroup detailsGroup;
        [SerializeField, Min(.01f)] private float duration = .25f;
        private bool hovered;
        private bool keepExpanded;
        private float progress;
        private bool IsWorld => gameObject.scene.name == "World";
        [SerializeField] private UnityEngine.UI.Button toggleButton;
        [SerializeField] private RectTransform toggleArrow;
        [SerializeField] private bool showObjectives = true;
        public bool Hovered => hovered || keepExpanded;
        public bool ShowObjectives => showObjectives;
        public float ExpansionAmount => progress * progress * (3 - 2 * progress);
        public void ToggleObjectives()
        {
            showObjectives = !showObjectives;
            if (IsWorld) hovered = showObjectives;
            Apply();
        }
        public void OnPointerEnter(PointerEventData _) { if (!IsWorld) SetExpanded(true); }
        public void OnPointerExit(PointerEventData _) { if (!IsWorld) SetExpanded(false); }
        public void SetExpanded(bool expanded, bool immediately = false)
        {
            if (IsWorld) return;
            if (keepExpanded) expanded = true;
            hovered = expanded;
            if (immediately) { progress = expanded ? 1 : 0; Apply(); }
        }
        public void KeepExpanded(bool value)
        {
            if (IsWorld) return;
            keepExpanded = value;
            if (value) SetExpanded(true, true);
        }
        private void OnDisable() { if(toggleButton) toggleButton.onClick.RemoveListener(ToggleObjectives); hovered = false; progress = 0; Apply(); }
        private void OnEnable() { if (IsWorld) { showObjectives = false; keepExpanded = false; } if(toggleButton) toggleButton.onClick.AddListener(ToggleObjectives); hovered = false; progress = 0; Apply(); }
        private void LateUpdate()
        {
            progress = Mathf.MoveTowards(progress, hovered ? 1 : 0, Time.unscaledDeltaTime / duration);
            Apply();
        }
        private void Apply()
        {
            bool visible = showObjectives || hovered || keepExpanded;
            var background = GetComponent<UnityEngine.UI.Image>();
            if (background) background.enabled = visible;
            foreach (string name in new[] { "Resource Summary", "Days Remaining" })
            {
                var child = transform.Find(name);
                var quest = GetComponent<QuestPanel>();
                if (child) child.gameObject.SetActive(visible && (name != "Days Remaining" || (quest ? quest.HasDeadline : child.gameObject.activeSelf)));
            }
            if(toggleArrow) toggleArrow.localEulerAngles = new Vector3(0,0,showObjectives?0:180);
            if (detailsContent == null || detailsLayout == null || detailsGroup == null) return;
            float eased = progress * progress * (3 - 2 * progress);
            float height = LayoutUtility.GetPreferredHeight(detailsContent) * eased;
            if (!Mathf.Approximately(detailsLayout.preferredHeight, height)) detailsLayout.preferredHeight = height;
            detailsGroup.alpha = eased;
            if (IsWorld) { detailsGroup.blocksRaycasts = hovered; detailsGroup.interactable = hovered; }
        }
    }
}
