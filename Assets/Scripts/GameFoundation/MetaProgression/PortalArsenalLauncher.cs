using GameFoundation.Settings;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameFoundation.MetaProgression
{
    /// <summary>Clicks on the actual portal tower, without taking input from a dragged work light.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class PortalArsenalLauncher : MonoBehaviour
    {
        private PortalTowerProgression progression;
        private ArcherTower tower;
        private SpriteRenderer[] towerSprites;
        private WorldFlashlightAvailability lights;
        private PortalArsenalPopup popup;
        private Vector2 downPosition;
        private bool pressed;
        private bool draggedDuringPress;
        public void Initialize(PortalTowerProgression owner) => progression = owner;
        private void Start()
        {
            foreach (var candidate in FindObjectsByType<ArcherTower>(FindObjectsSortMode.None))
                if (candidate.GetStats() != null && candidate.GetStats().IsPortalTower) { tower = candidate; break; }
            if (tower != null) towerSprites = tower.GetComponentsInChildren<SpriteRenderer>();
            lights = FindFirstObjectByType<WorldFlashlightAvailability>();
        }
        private bool Blocked => progression == null || !progression.UsesArsenal || progression.ExpeditionEnded ||
            PortalTowerProgression.IsChoosingUpgrade || PortalCloudTransition.IsLoading || WorldEscapeController.ResultsActive ||
            FirstRunLightTutorial.BlocksInput || PortalArsenalPopup.IsOpen || FindFirstObjectByType<GameMenuController>() != null;
        private void Update()
        {
            if (!pressed && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonUp(0)) return;
            ProcessPointer(Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0),
                EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(), lights != null && lights.IsDragging);
        }
        internal void ProcessPointer(Vector2 screenPosition, bool down, bool up, bool ui, bool draggingLight)
        {
            if (Blocked) { pressed = false; return; }
            if (down) { downPosition = screenPosition; pressed = !ui; draggedDuringPress = false; }
            if (pressed) draggedDuringPress |= draggingLight;
            if (!up) return;
            bool click = pressed && !ui && !draggedDuringPress && (screenPosition - downPosition).sqrMagnitude < 36;
            pressed = false;
            if (!click || Camera.main == null || tower == null) return;
            Vector2 point = Camera.main.ScreenToWorldPoint(screenPosition);
            foreach (var sprite in towerSprites)
                if (sprite != null && sprite.enabled && sprite.gameObject.activeInHierarchy && sprite.sprite != null &&
                    sprite.bounds.Contains(new Vector3(point.x, point.y, sprite.bounds.center.z))) { Open(); break; }
        }
        public void Open()
        {
            if (Blocked || progression.Balance.arsenalPopupPrefab == null) return;
            if (popup == null) popup = Instantiate(progression.Balance.arsenalPopupPrefab).GetComponent<PortalArsenalPopup>();
            popup.Open(progression, tower);
        }
        private void OnDestroy() { if (popup != null) Destroy(popup.gameObject); }
    }
}
