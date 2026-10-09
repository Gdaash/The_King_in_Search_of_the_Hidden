using System.Collections;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed partial class WorldFlashlightAvailability
    {
        [Header("Light dragging")]
        [SerializeField, Min(0)] private float activationDelay = .1f;
        [SerializeField] private Vector3 activationButtonWorldOffset = new(0, -3, 0);
        [SerializeField, Min(1)] private float dragThresholdPixels = 6f;
        private FlashlightActivationButton activationButton;
        private int pendingDragLight = -1, draggedLight = -1;
        private Vector2 dragPointerDown, dragOrigin, dragOffset;
        private bool pointerStartedInWorld, pointerMoved;
        private bool dragHadAssignment;
        public bool LightsActivated { get; private set; }
        public bool CanActivateLights => !escaped && !LightsActivated && lights.Length > 0 &&
            GameSpeedControls.SimulationSpeed > 0;
        public bool IsDragging => draggedLight >= 0;
        public Vector2 DragPosition => draggedLight >= 0 ? lights[draggedLight].beam.MarkerPosition : Vector2.zero;
        public bool IsDraggingFlag(LogisticFlag flag) => draggedLight >= 0 && lights[draggedLight].beam.Flag == flag;

        private void InitializeDragControls()
        {
            for (int i = 0; i < lights.Length; i++)
                lights[i].position = lights[i].beam.MarkerPosition;
            if (lights.Length == 0) return;
            activationButton = lights[0].beam.ActivationButton;
            if (activationButton == null)
            {
                Debug.LogError("Flashlight prefab requires its light activation button.", this);
                return;
            }
            activationButton.Bind(this);
            activationButton.transform.position = lights[0].beam.transform.position + activationButtonWorldOffset;
            activationButton.gameObject.SetActive(true);
        }

        public void ActivateAvailableLights()
        {
            if (escaped || LightsActivated || lights.Length == 0 || GameSpeedControls.SimulationSpeed <= 0) return;
            LightsActivated = true;
            if (activationButton != null) activationButton.gameObject.SetActive(false);
            StartCoroutine(ActivateBeams());
        }

        private IEnumerator ActivateBeams()
        {
            for (int i = 0; i < lights.Length && !escaped; i++)
            {
                lights[i].beam.ShowIdle(lights[i].position);
                TryStartAtLight(i);
                if (activationDelay > 0) yield return new WaitForSeconds(activationDelay);
            }
        }

        private bool CanDragLight(int index) => !escaped && LightsActivated &&
            GameSpeedControls.SimulationSpeed > 0 && index >= 0 && index < lights.Length &&
            lights[index].beam.MarkerVisible;

        // This is also exercised by the Play Mode audit with screen-space pointer sequences.
        internal void ProcessDragPointer(Vector2 screenPosition, bool pressed, bool held, bool released)
        {
            if (escaped || !LightsActivated || worldCamera == null || GameSpeedControls.SimulationSpeed <= 0)
            { CancelDrag(); return; }
            bool onScreen = worldCamera.pixelRect.Contains(screenPosition);
            bool blocked = !onScreen || IsPointerOverBlockingUI(screenPosition);
            Vector2 point = worldCamera.ScreenToWorldPoint(screenPosition);
            if (pressed)
            {
                pendingDragLight = -1;
                pointerStartedInWorld = !blocked;
                pointerMoved = false;
                dragPointerDown = screenPosition;
                if (!blocked)
                {
                    float nearest = float.MaxValue;
                    for (int i = 0; i < lights.Length; i++)
                    {
                        if (!CanDragLight(i) || !lights[i].beam.ContainsMarkerPoint(point)) continue;
                        float distance = (point - lights[i].beam.MarkerPosition).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; pendingDragLight = i; }
                    }
                    if (pendingDragLight >= 0) dragOffset = lights[pendingDragLight].beam.MarkerPosition - point;
                }
            }
            if (pointerStartedInWorld && (held || released) &&
                (screenPosition - dragPointerDown).sqrMagnitude >= dragThresholdPixels * dragThresholdPixels)
                pointerMoved = true;
            if (pendingDragLight >= 0 && (held || released) && pointerMoved)
            {
                int index = pendingDragLight;
                pendingDragLight = -1;
                if (!blocked) BeginDrag(index);
            }
            bool wasDragging = draggedLight >= 0;
            if (wasDragging)
            {
                if (!blocked && (held || released)) DragTo(point + dragOffset);
                if (released)
                {
                    if (blocked) CancelDrag();
                    else DropAt(point + dragOffset);
                }
                // Losing mouse capture must never leave a beam attached to the cursor.
                else if (!held && !pressed) CancelDrag();
            }
            if (released)
            {
                if (pointerStartedInWorld && !pointerMoved && !blocked && !wasDragging)
                    StartAtIlluminatedHex(point);
                pendingDragLight = -1;
                pointerStartedInWorld = false;
            }
        }

        public bool BeginDrag(int index)
        {
            if (draggedLight >= 0 || !CanDragLight(index)) return false;
            dragOrigin = lights[index].beam.MarkerPosition;
            dragHadAssignment = lights[index].occupied;
            lights[index].restartPending = false;
            if (lights[index].occupied)
            {
                bool pausing = lights[index].working ||
                    (lights[index].requester != null && lights[index].requester.IsCyclePaused);
                Release(index);
                GameFoundation.UI.GameNotifications.Post(pausing ? "Работа приостановлена" : "Действие отменено");
            }
            draggedLight = index;
            lights[index].beam.ShowIdle(dragOrigin);
            return true;
        }

        public void DragTo(Vector2 point)
        {
            if (draggedLight >= 0 && CanDragLight(draggedLight)) lights[draggedLight].beam.ShowIdle(point);
        }

        public bool DropAt(Vector2 point)
        {
            if (draggedLight < 0) return false;
            int index = draggedLight;
            if (!CanDragLight(index)) { CancelDrag(); return false; }
            var target = GetHoverTarget(point);
            draggedLight = -1;
            pendingDragLight = -1;
            if (target.State == HoverState.Available && target.LightIndex == index)
            {
                TargetClicked?.Invoke(target);
                return target.Hex != null ? OpenHex(index, target.Hex) : ReserveBuilding(index, target.Requester);
            }
            // Empty ground parks the free light. An invalid/occupied target returns it without starting an action.
            lights[index].position = target.Anchor == null ? SnapToOpenHex(point) : dragOrigin;
            lights[index].beam.ShowIdle(lights[index].position);
            if (target.Anchor != null) RestoreDragOrigin(index);
            return false;
        }

        private static Vector2 SnapToOpenHex(Vector2 point)
        {
            Vector2 result = point;
            float nearest = float.MaxValue;
            foreach (var magnet in HexMagnet.ActiveInstances)
            {
                if (magnet == null || !magnet.IsPointOverMagnet(point)) continue;
                float distance = (point - (Vector2)magnet.transform.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; result = magnet.transform.position; }
            }
            return result;
        }

        public void CancelDrag()
        {
            pointerStartedInWorld = false;
            pendingDragLight = -1;
            if (draggedLight < 0) return;
            int index = draggedLight;
            draggedLight = -1;
            RestoreDragOrigin(index);
        }

        private void RestoreDragOrigin(int index)
        {
            var assignment = lights[index];
            assignment.position = dragOrigin;
            if (escaped) return;
            assignment.beam.ShowIdle(dragOrigin);
            if (!dragHadAssignment) return;
            var target = ResolveTarget(dragOrigin, index);
            if (target.State != HoverState.Available || target.LightIndex != index) return;
            // Reattach even during global pause; the simulation clock still keeps the timer frozen.
            if (target.Hex != null) OpenHex(index, target.Hex);
            else ReserveBuilding(index, target.Requester);
        }

        private void OnApplicationFocus(bool focused) { if (!focused) CancelDrag(); }
    }
}
