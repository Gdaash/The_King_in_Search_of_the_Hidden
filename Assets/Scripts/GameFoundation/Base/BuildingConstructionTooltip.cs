using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class BuildingConstructionTooltip : MonoBehaviour
    {
        public static BuildingConstructionTooltip Instance { get; private set; }

        [SerializeField] private Text title;
        [SerializeField] private Text description;
        [SerializeField] private Text priceLabel;
        [SerializeField] private Image woodIcon;
        [SerializeField] private Text woodAmount;
        [SerializeField] private Image stoneIcon;
        [SerializeField] private Text stoneAmount;
        [SerializeField] private Vector2 cursorOffset = new Vector2(24f, -24f);
        [SerializeField] private float screenMargin = 12f;

        private CanvasGroup group;
        private RectTransform rect;

        private void Awake()
        {
            Instance = this;
            group = GetComponent<CanvasGroup>();
            rect = transform as RectTransform;
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string buildingName, string function, string price,
            ResourceType wood, int woodCost, ResourceType stone, int stoneCost)
        {
            if (title != null) title.text = buildingName;
            if (description != null) description.text = function;
            if (priceLabel != null) priceLabel.text = price;
            SetResource(woodIcon, woodAmount, wood, woodCost);
            SetResource(stoneIcon, stoneAmount, stone, stoneCost);
            group.alpha = 1f;
            group.blocksRaycasts = false;
            transform.SetAsLastSibling();
            UpdatePosition();
        }

        public void Hide()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void Update()
        {
            if (group != null && group.alpha > 0f) UpdatePosition();
        }

        private static void SetResource(Image image, Text amount, ResourceType resource, int cost)
        {
            if (image != null)
            {
                ResourceIconSizing.Apply(image, resource != null ? resource.resourceIcon : null);
                image.enabled = image.sprite != null;
            }
            if (amount != null) amount.text = cost.ToString();
        }

        private void UpdatePosition()
        {
            if (rect == null) return;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera : null;
            RectTransform parent = rect.parent as RectTransform;
            if (parent == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
                (Vector2)Input.mousePosition + cursorOffset, camera, out Vector2 local);
            rect.anchoredPosition = local;
            Canvas.ForceUpdateCanvases();

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 correction = Vector2.zero;
            if (corners[0].x < screenMargin) correction.x += screenMargin - corners[0].x;
            if (corners[2].x > Screen.width - screenMargin) correction.x -= corners[2].x - (Screen.width - screenMargin);
            if (corners[0].y < screenMargin) correction.y += screenMargin - corners[0].y;
            if (corners[2].y > Screen.height - screenMargin) correction.y -= corners[2].y - (Screen.height - screenMargin);
            rect.position += (Vector3)correction;
        }
    }
}
