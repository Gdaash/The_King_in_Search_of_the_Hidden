using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Projects the existing building controls onto the PSD artwork without moving its sprites.</summary>
    public sealed class WorldBuildingButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string artworkRoot = "Base Scene Artwork";
        [SerializeField] private string[] artworkLayers;
        [SerializeField] private Vector2 constructionPixelPosition;
        [SerializeField] private Vector2 documentSize = new(640, 480);
        [SerializeField] private float pixelsPerUnit = 32;
        [SerializeField] private Vector2 upgradeOffset = new(0, -44);
        [SerializeField] private Color hoverTint = new(1.18f, 1.18f, 1.18f, 1);
        [Tooltip("Shared lit material used by every unbuilt building. Edit its Tint to change all buildings together.")]
        [SerializeField] private Material unbuiltMaterial;
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private RectTransform constructionButton;
        [SerializeField] private RectTransform upgradeButton;
        [SerializeField] private RectTransform actionMarker;
        private readonly List<SpriteRenderer> sprites = new();
        private readonly List<Color> originalColors = new();
        private readonly List<Material> originalMaterials = new();
        private RectTransform rect;
        private Transform artwork;
        private bool hovered;
        public GameObject PointerTarget => !Built && constructionButton != null ? constructionButton.gameObject : gameObject;
        private bool Built => construction == null || construction.IsBuilt;

        private void Awake() => Resolve();
        private void OnEnable() { BuildingUpgradeService.Changed += Refresh; }
        private void Start() { Refresh(); UpdatePosition(); }
        private void OnDisable() { BuildingUpgradeService.Changed -= Refresh; SetHover(false); }
        public void Resolve()
        {
            rect = (RectTransform)transform;
            var root = GameObject.Find(artworkRoot);
            if (root == null) return;
            artwork = root.transform;
            sprites.Clear(); originalColors.Clear(); originalMaterials.Clear();
            foreach (string layer in artworkLayers)
            {
                var child = artwork.Find(layer);
                var sprite = child != null ? child.GetComponent<SpriteRenderer>() : null;
                if (sprite == null) { Debug.LogError("Missing building artwork: " + layer, this); continue; }
                var hit = child.GetComponent<WorldBuildingHitArea>();
                if (hit != null) hit.Owner = this;
                sprites.Add(sprite); originalColors.Add(sprite.color); originalMaterials.Add(sprite.sharedMaterial);
            }
        }
        private void Refresh()
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                sprites[i].gameObject.SetActive(true);
                sprites[i].sharedMaterial = !Built && unbuiltMaterial != null ? unbuiltMaterial : originalMaterials[i];
            }
            if (TryGetComponent<Image>(out var image)) image.raycastTarget = false;
            if (!Built) SetHover(false);
        }
        private void LateUpdate() => UpdatePosition();
        private float ScreenScale => Mathf.Min(Screen.width / documentSize.x, Screen.height / documentSize.y);
        private Vector2 PixelToScreen(Vector2 p) =>
            (new Vector2(Screen.width, Screen.height) - documentSize * ScreenScale) * .5f +
            new Vector2(p.x, documentSize.y - p.y) * ScreenScale;
        private Vector2 WorldToPixel(Vector3 world)
        {
            var local = artwork.InverseTransformPoint(world);
            return new Vector2(local.x * pixelsPerUnit + documentSize.x * .5f, documentSize.y * .5f - local.y * pixelsPerUnit);
        }
        private Vector2 LocalPoint(RectTransform parent, Vector2 screen)
        {
            var canvas = GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen,
                canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out var local);
            return local;
        }
        public void UpdatePosition()
        {
            if (rect == null || artwork == null || sprites.Count == 0) return;
            Bounds bounds = sprites[0].bounds;
            foreach (var sprite in sprites) bounds.Encapsulate(sprite.bounds);
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            Vector2 min = LocalPoint(parent, PixelToScreen(WorldToPixel(bounds.min)));
            Vector2 max = LocalPoint(parent, PixelToScreen(WorldToPixel(bounds.max)));
            rect.localPosition = (min + max) * .5f;
            rect.sizeDelta = max - min;
            Vector2 constructionScreen = PixelToScreen(constructionPixelPosition);
            Vector2 constructionLocal = LocalPoint(rect, constructionScreen);
            if (constructionButton != null) constructionButton.anchoredPosition = constructionLocal;
            if (upgradeButton != null) upgradeButton.anchoredPosition = constructionLocal + upgradeOffset;
            if (actionMarker != null) actionMarker.anchoredPosition = new Vector2(-4, -4);
        }
        public void OnPointerEnter(PointerEventData _) { if (Built) SetHover(true); }
        public void OnPointerExit(PointerEventData _) => SetHover(false);
        private void SetHover(bool value)
        {
            hovered = value;
            for (int i = 0; i < sprites.Count; i++) if (sprites[i] != null) sprites[i].color = originalColors[i] * (hovered ? hoverTint : Color.white);
        }
    }
}
