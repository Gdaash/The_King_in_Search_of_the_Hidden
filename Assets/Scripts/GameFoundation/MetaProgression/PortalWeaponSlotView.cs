using GameFoundation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace GameFoundation.MetaProgression
{
    public sealed class PortalWeaponSlotView : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image icon;
        public TMP_Text number;
        public UnifiedButtonFeedback feedback;
        public PortalTowerBalance.Weapon DraggedWeapon { get; private set; }
        private PortalWeaponSlots owner;
        private int index;
        private UnityEngine.UI.Button purchase;
        private TMP_Text purchaseLabel;

        internal void SetPurchase(bool visible, bool affordable, bool canBuy)
        {
            if (visible && purchase == null)
            {
                var theme = Resources.Load<ButtonVisualTheme>("UI/ButtonVisualTheme");
                var root = new GameObject("Buy weapon slot", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                var rect = (RectTransform)root.transform;
                rect.SetParent(transform, false);
                rect.sizeDelta = new Vector2(50, 50);
                rect.anchoredPosition = Vector2.zero;
                var image = root.GetComponent<UnityEngine.UI.Image>();
                image.sprite = theme.textButtonSprite;
                image.type = UnityEngine.UI.Image.Type.Sliced;
                purchase = root.GetComponent<UnityEngine.UI.Button>();
                purchase.targetGraphic = image;
                purchase.transition = UnityEngine.UI.Selectable.Transition.None;
                root.AddComponent<UnifiedButtonFeedback>();
                purchase.onClick.AddListener(() => owner.BuySlot(index));
                var hover = root.AddComponent<EventTrigger>();
                var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => ShowPurchaseHint());
                hover.triggers.Add(enter);
                var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => TooltipManager.Instance?.Hide());
                hover.triggers.Add(exit);
                var label = new GameObject("Plus", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(rect, false);
                purchaseLabel = label.GetComponent<TMP_Text>();
                purchaseLabel.rectTransform.anchorMin = Vector2.zero;
                purchaseLabel.rectTransform.anchorMax = Vector2.one;
                purchaseLabel.rectTransform.offsetMin = purchaseLabel.rectTransform.offsetMax = Vector2.zero;
                purchaseLabel.font = number.font;
                purchaseLabel.fontStyle = FontStyles.Bold;
                purchaseLabel.fontSize = 36;
                purchaseLabel.alignment = TextAlignmentOptions.Center;
                purchaseLabel.raycastTarget = false;
                purchaseLabel.text = "+";
            }
            if (purchase == null) return;
            purchase.gameObject.SetActive(visible);
            purchase.interactable = affordable && canBuy;
            purchaseLabel.color = affordable ? new Color(.55f, .85f, .48f) : new Color(.99f, .49f, .50f);
        }
        private bool dragging;
        private int lastDragFrame = -1;
        // The frame is one source pixel thick, displayed at 2 Canvas units per pixel.
        private const float FrameThickness = 2f;
        private static readonly Color FillColor = new Color32(24, 19, 28, 192);
        private static readonly Color ReloadColor = new Color32(243, 192, 84, 255);
        private RectTransform[] reloadEdges;
        private CanvasGroup practiceInput;
        private bool previousIgnoreParentGroups;
        private UnityEngine.UI.Outline practiceOutline;

        internal void SetPracticeHighlight(bool highlighted)
        {
            if (highlighted)
            {
                if (practiceInput != null) return;
                if (!button.TryGetComponent(out practiceInput))
                    practiceInput = button.gameObject.AddComponent<CanvasGroup>();
                previousIgnoreParentGroups = practiceInput.ignoreParentGroups;
                practiceInput.ignoreParentGroups = true;
                if (practiceOutline == null)
                {
                    practiceOutline = button.targetGraphic.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    practiceOutline.effectColor = ReloadColor;
                    practiceOutline.effectDistance = new Vector2(4, 4);
                    practiceOutline.useGraphicAlpha = false;
                }
                practiceOutline.enabled = true;
            }
            else if (practiceInput != null)
            {
                practiceInput.ignoreParentGroups = previousIgnoreParentGroups;
                practiceInput = null;
                if (practiceOutline != null) practiceOutline.enabled = false;
            }
        }

        private void Awake()
        {
            var fill = AddDecoration("Slot fill", FillColor);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.one * FrameThickness;
            fill.offsetMax = -fill.offsetMin;
            fill.SetAsFirstSibling();
        }

        public void SetReloadProgress(float progress)
        {
            if (reloadEdges == null)
            {
                if (progress <= 0) return;
                reloadEdges = new[] {
                    CreateEdge(Vector2.up, Vector2.up),
                    CreateEdge(Vector2.one, Vector2.one),
                    CreateEdge(Vector2.right, Vector2.right),
                    CreateEdge(Vector2.zero, Vector2.zero)
                };
            }
            var size = ((RectTransform)transform).rect.size - Vector2.one * (2 * FrameThickness);
            float side = size.y - 2 * FrameThickness;
            float distance = Mathf.Floor(progress * (2 * size.x + 2 * side) / FrameThickness) * FrameThickness;
            DrawEdge(0, new Vector2(FrameThickness, -FrameThickness), Mathf.Clamp(distance, 0, size.x), true);
            DrawEdge(1, new Vector2(-FrameThickness, -2 * FrameThickness), Mathf.Clamp(distance - size.x, 0, side), false);
            DrawEdge(2, new Vector2(-FrameThickness, FrameThickness), Mathf.Clamp(distance - size.x - side, 0, size.x), true);
            DrawEdge(3, new Vector2(FrameThickness, 2 * FrameThickness), Mathf.Clamp(distance - 2 * size.x - side, 0, side), false);
        }

        private RectTransform CreateEdge(Vector2 anchor, Vector2 pivot)
        {
            var edge = AddDecoration("Reload edge", ReloadColor);
            edge.anchorMin = edge.anchorMax = anchor;
            edge.pivot = pivot;
            return edge;
        }

        private void DrawEdge(int index, Vector2 position, float length, bool horizontal)
        {
            var edge = reloadEdges[index];
            edge.gameObject.SetActive(length > 0);
            edge.anchoredPosition = position;
            edge.sizeDelta = horizontal ? new Vector2(length, FrameThickness) : new Vector2(FrameThickness, length);
        }

        private RectTransform AddDecoration(string name, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)).GetComponent<UnityEngine.UI.Image>();
            image.transform.SetParent(transform, false);
            image.color = color;
            image.raycastTarget = false;
            return image.rectTransform;
        }
        public void Bind(PortalWeaponSlots panel, int slot)
        {
            if (owner == panel && index == slot) return;
            owner = panel; index = slot;
            button.onClick.RemoveAllListeners(); button.onClick.AddListener(() => { if (!dragging && Time.frameCount > lastDragFrame) owner.Activate(index); });
        }
        public void OnDrop(PointerEventData data)
        {
            if (data.pointerDrag == null || owner == null) return;
            var row = data.pointerDrag.GetComponent<PortalArsenalWeaponRow>();
            var slot = data.pointerDrag.GetComponent<PortalWeaponSlotView>();
            if (row != null) owner.Assign(index, row.Weapon);
            else if (slot != null) owner.Assign(index, slot.DraggedWeapon);
        }
        public void OnBeginDrag(PointerEventData data)
        { DraggedWeapon = PortalWeaponLoadout.Get(index); dragging = owner != null && owner.BeginDrag(DraggedWeapon, data); }
        public void OnDrag(PointerEventData data) { if (dragging) owner.MoveDrag(data); }
        public void OnEndDrag(PointerEventData data) { if (dragging) { owner.EndDrag(); lastDragFrame = Time.frameCount; } dragging = false; }
        public void OnPointerEnter(PointerEventData data)
        {
            if (owner == null || TooltipManager.Instance == null) return;
            var target = (RectTransform)transform;
            if (purchase != null && purchase.gameObject.activeSelf) return;
            if (index >= PortalWeaponLoadout.SlotCount)
                TooltipManager.Instance.Show(PortalArsenalPopup.T("slots.locked", "Заблокировано"), target);
            else
                TooltipManager.Instance.Show(owner.Hint(index), target);
        }
        private void ShowPurchaseHint()
        {
            TooltipManager.Instance?.Show(PortalArsenalPopup.T("slots.buy", "Купить ячейку"),
                (RectTransform)purchase.transform, priceIcon: PortalWeaponLoadout.SlotResource?.resourceIcon,
                price: PortalWeaponLoadout.SlotPrice, priceAffordable: PortalWeaponLoadout.CanAffordSlot);
        }

        public void OnPointerExit(PointerEventData data) => TooltipManager.Instance?.Hide();
    }
}
