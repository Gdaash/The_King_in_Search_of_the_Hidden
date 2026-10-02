using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.Base
{
    /// <summary>Positions visible building buttons beside the PSD artwork and applies the unbuilt material.</summary>
    public sealed class WorldBuildingButton : MonoBehaviour
    {
        [SerializeField] private string artworkRoot = "Base Scene Artwork";
        [Tooltip("Keep the position authored in the scene for World Space buttons.")]
        [SerializeField] private bool manuallyPositioned;
        [SerializeField] private string[] artworkLayers;
        [SerializeField] private Vector2 constructionPixelPosition;
        [SerializeField] private Vector2 buttonSize = new(236, 64);
        [SerializeField] private Vector2 documentSize = new(640, 480);
        [SerializeField] private Vector2 upgradeOffset = new(0, -86);
        [Tooltip("Shared lit material used by every unbuilt building. Edit its Tint to change all buildings together.")]
        [SerializeField] private Material unbuiltMaterial;
        [SerializeField] private BaseBuildingConstruction construction;
        [SerializeField] private RectTransform constructionButton;
        [SerializeField] private RectTransform upgradeButton;
        [SerializeField] private RectTransform actionMarker;
        private readonly List<SpriteRenderer> sprites = new();
        private readonly List<Material> originalMaterials = new();
        private RectTransform rect;
        private Transform artwork;
        public GameObject PointerTarget => !Built && constructionButton != null ? constructionButton.gameObject : gameObject;
        private bool Built => construction == null || construction.IsBuilt;

        private void Awake() => Resolve();
        private void OnEnable() { BuildingUpgradeService.Changed += Refresh; }
        private void Start() { Refresh(); UpdatePosition(); }
        private void OnDisable() { BuildingUpgradeService.Changed -= Refresh; }
        public void Resolve()
        {
            rect = (RectTransform)transform;
            var root = GameObject.Find(artworkRoot);
            if (root == null) return;
            artwork = root.transform;
            sprites.Clear(); originalMaterials.Clear();
            foreach (string layer in artworkLayers)
            {
                var child = artwork.Find(layer);
                var sprite = child != null ? child.GetComponent<SpriteRenderer>() : null;
                if (sprite == null) { Debug.LogError("Missing building artwork: " + layer, this); continue; }
                var hit = child.GetComponent<WorldBuildingHitArea>();
                if (hit != null) hit.Owner = null;
                sprites.Add(sprite); originalMaterials.Add(sprite.sharedMaterial);
            }
        }
        private void Refresh()
        {
            for (int i = 0; i < sprites.Count; i++)
            {
                sprites[i].gameObject.SetActive(true);
                sprites[i].sharedMaterial = !Built && unbuiltMaterial != null ? unbuiltMaterial : originalMaterials[i];
            }
            if (TryGetComponent<Image>(out var image)) { image.enabled = Built; image.raycastTarget = Built; }
            var label = transform.Find("Label");
            if (label != null) label.gameObject.SetActive(Built);
        }
        private void LateUpdate() => UpdatePosition();
        private float ScreenScale => Mathf.Min(Screen.width / documentSize.x, Screen.height / documentSize.y);
        private Vector2 PixelToScreen(Vector2 p) =>
            (new Vector2(Screen.width, Screen.height) - documentSize * ScreenScale) * .5f +
            new Vector2(p.x, documentSize.y - p.y) * ScreenScale;
        private Vector2 LocalPoint(RectTransform parent, Vector2 screen)
        {
            var canvas = GetComponentInParent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen,
                canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out var local);
            return local;
        }
        public void UpdatePosition()
        {
            if (manuallyPositioned) return;
            if (rect == null || artwork == null || sprites.Count == 0) return;
            var parent = rect.parent as RectTransform;
            if (parent == null) return;
            Vector2 constructionScreen = PixelToScreen(constructionPixelPosition);
            rect.localPosition = LocalPoint(parent, constructionScreen);
            rect.sizeDelta = buttonSize;
            if (constructionButton != null) constructionButton.anchoredPosition = Vector2.zero;
            if (upgradeButton != null) upgradeButton.anchoredPosition = upgradeOffset;
            if (actionMarker != null) actionMarker.anchoredPosition = new Vector2(8, 8);
        }
    }
}
