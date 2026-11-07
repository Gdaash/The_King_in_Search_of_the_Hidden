using System;
using System.Collections.Generic;
using GameFoundation.Saves;
using UnityEngine;
namespace GameFoundation.MetaProgression
{
    public static class WorldMilitaryLightRules
    {
        public static bool BlocksWork(Vector2 point)
        {
            foreach (var flag in LogisticFlag.ActiveInstances)
            {
                if (flag == null || !flag.isActiveAndEnabled || !flag.MilitaryMode) continue;
                var hex = flag.MilitaryHex;
                if (hex != null && hex.GetComponent<HexMagnet>().IsPointOverMagnet(point)) return true;
            }
            return false;
        }
    }
    public sealed partial class WorldFlashlightAvailability
    {
        public event Action MilitaryPositionsChanged;
        private WorldMilitaryDeploymentController military;
        public const string MilitaryTutorialKey = "foundation.tutorial.military_light.seen";
        public bool MilitaryHintKnown => SaveSlotPrefs.GetInt(MilitaryTutorialKey, 0) != 0;
        public IReadOnlyList<Vector2> MilitaryPositions
        {
            get
            {
                var result = new List<Vector2>();
                if (escaped) return result;
                foreach (var light in lights)
                    if (light.military && light.beam.MarkerVisible && !result.Contains(light.position)) result.Add(light.position);
                return result;
            }
        }
        private bool HasTroops
        {
            get
            {
                if (military == null) military = FindFirstObjectByType<WorldMilitaryDeploymentController>();
                if (military == null || military.gameObject.scene != gameObject.scene) return false;
                foreach (var unit in military.ActiveUnits)
                    if (unit != null && unit.GetComponent<WorldMilitaryReturner>() == null &&
                        (!unit.TryGetComponent<Health>(out var hp) || !hp.IsDead)) return true;
                return false;
            }
        }
        internal static bool MilitaryGroundIsOpen(Vector2 point)
        {
            // Frontier availability is not exploration. Closed visuals and magnets can be siblings.
            foreach (var closed in HexLightUnlocker.ActiveInstances)
                if (closed != null && closed.isActiveAndEnabled && !closed.IsUnlocked() && closed.IsPointOverHex(point)) return false;
            foreach (var blocker in HexBlocker.ActiveInstances)
            {
                if (blocker == null || !blocker.isActiveAndEnabled || blocker.IsExplored) continue;
                var collider = blocker.GetComponent<Collider2D>();
                if ((collider != null && collider.enabled && collider.OverlapPoint(point)) ||
                    ((Vector2)blocker.transform.position - point).sqrMagnitude < .75f * .75f) return false;
            }
            foreach (var hit in Physics2D.OverlapPointAll(point))
                if (hit != null && hit.GetComponent<Gex>() != null) return false;
            return true;
        }
        private void ValidateMilitaryGround()
        {
            bool changed = false;
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (!light.military || OpenMilitaryHex(light.position) != null) continue;
                light.military = false;
                light.beam.Flag.SetMilitaryMode(false);
                changed = true;
            }
            if (changed) MilitaryPositionsChanged?.Invoke();
        }
        internal static Transform OpenMilitaryHex(Vector2 point)
        {
            if (!MilitaryGroundIsOpen(point)) return null;
            Transform best = null;
            float nearest = float.PositiveInfinity;
            foreach (var hex in HexMagnet.ActiveInstances)
            {
                if (hex == null || !hex.isActiveAndEnabled || !hex.IsPointOverMagnet(point) || !MilitaryGroundIsOpen(hex.transform.position)) continue;
                var blocker = hex.GetComponentInParent<HexBlocker>();
                if (blocker != null && blocker.IsBlocked) continue;
                var unlocker = hex.GetComponentInChildren<HexLightUnlocker>();
                if (unlocker != null && !unlocker.IsUnlocked()) continue;
                float distance = (point - (Vector2)hex.transform.position).sqrMagnitude;
                if (distance < nearest) { nearest = distance; best = hex.transform; }
            }
            return best;
        }
        private int MilitaryLightAt(Vector2 point)
        {
            if (escaped || !LightsActivated || IsDragging) return -1;
            int best = -1; float nearest = float.PositiveInfinity;
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                if (!light.beam.ContainsMarkerPoint(point) || light.hex != null || OpenMilitaryHex(light.beam.MarkerPosition) == null) continue;
                float d = (light.beam.MarkerPosition - point).sqrMagnitude;
                if (d < nearest) { nearest = d; best = i; }
            }
            return best;
        }
        public bool CanToggleMilitaryAt(Vector2 point)
        {
            int index = MilitaryLightAt(point);
            return index >= 0 && (lights[index].military || HasTroops);
        }
        public bool ToggleMilitaryAt(Vector2 point)
        {
            if ((FirstRunLightTutorial.BlocksInput && !FirstRunLightTutorial.AllowsMilitaryToggle) ||
                ModalBlocksLightInput || PortalWeaponController.OwnsPointer) return false;
            int index = MilitaryLightAt(point);
            if (index < 0 || (!lights[index].military && !HasTroops)) return false;
            var light = lights[index];
            bool enabled = !light.military;
            if (enabled)
            {
                var anchor = OpenMilitaryHex(light.beam.MarkerPosition);
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i].occupied && anchor.GetComponent<HexMagnet>().IsPointOverMagnet(lights[i].position)) Release(i);
            }
            else if (light.occupied) Release(index);
            light.military = enabled;
            light.restartPending = !enabled && autoRepeat;
            light.restartAfterFrame = Time.frameCount + 1;
            light.position = OpenMilitaryHex(light.beam.MarkerPosition).position;
            light.beam.Flag.SetMilitaryMode(enabled, enabled ? OpenMilitaryHex(light.position) : null);
            light.beam.ShowIdle(light.position);
            if (enabled)
                foreach (var requester in ResourceRequester.ActiveInstances)
                    if (requester != null && WorldMilitaryLightRules.BlocksWork(requester.transform.position))
                    { requester.SetCrystalFlag(null); requester.RecallIdleHumans(); }
            OrderManager.Instance?.ForceUpdateOrders();
            MilitaryPositionsChanged?.Invoke();
            if (enabled) FindFirstObjectByType<FirstRunLightTutorial>()?.CompleteMilitaryExplanation();
            return true;
        }
        internal void ProcessMilitaryPointer(Vector2 screenPosition, bool pressed, bool released)
        {
            if (worldCamera == null) return;
            bool allowed = (!FirstRunLightTutorial.BlocksInput || FirstRunLightTutorial.AllowsMilitaryToggle) &&
                !IsDragging && worldCamera.pixelRect.Contains(screenPosition) && !IsPointerOverBlockingUI(screenPosition);
            if (pressed) { rightPointerDown = screenPosition; rightPointerStartedInWorld = allowed; }
            if (!released) return;
            if (rightPointerStartedInWorld && allowed && (screenPosition - rightPointerDown).sqrMagnitude < 64)
                ToggleMilitaryAt(worldCamera.ScreenToWorldPoint(screenPosition));
            rightPointerStartedInWorld = false;
        }
        private bool firstOpeningPrepared;
        private void PrepareMilitaryTutorialFirstHex(HexBlocker hex)
        {
            if (hex == null || firstOpeningPrepared) return;
            firstOpeningPrepared = true;
            bool eligible = PlaytestSettings.ForceMilitaryLightTutorial || (!MilitaryHintKnown && !PlaytestSettings.TutorialsAndQuestsDisabled);
            // The basic gathering tutorial owns its guaranteed forest. This rule belongs to the later military lesson.
            if (!eligible || !HasTroops || FirstRunLightTutorial.BlocksInput) return;
            hex.AssignContent(null, 0, false);
        }
        private bool militaryTutorialShown, militaryTutorialPending;
        private Vector2 militaryTutorialHexPosition;
        private float militaryTutorialReadyAt;
        private void UpdateMilitaryTutorial()
        {
            if (!militaryTutorialPending || militaryTutorialShown || ((!PlaytestSettings.ForceMilitaryLightTutorial) && (MilitaryHintKnown || PlaytestSettings.TutorialsAndQuestsDisabled)) || !HasTroops || !HasVisibleLight ||
                FirstRunLightTutorial.BlocksInput || PortalTowerProgression.IsChoosingUpgrade || IsDragging) return;
            // Completion fires before the closing cover finishes its dissolve animation.
            foreach (var cover in HexBlocker.ActiveInstances)
                if (cover != null && cover.gameObject.activeInHierarchy &&
                    ((Vector2)cover.transform.position - militaryTutorialHexPosition).sqrMagnitude < .75f * .75f)
                { militaryTutorialReadyAt = Time.time + .15f; return; }
            if (Time.time < militaryTutorialReadyAt) return;
            bool hasOpenLight = false;
            foreach (var light in lights)
                if (CanToggleMilitaryAt(light.beam.MarkerPosition)) { hasOpenLight = true; break; }
            if (!hasOpenLight) return;
            // An explored hex with troops queues this dialogue until other UI releases input.
            var tutorial = FindFirstObjectByType<FirstRunLightTutorial>();
            if (tutorial != null && tutorial.TryExplainMilitary()) militaryTutorialShown = true;
        }
    }
}
