using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameFoundation.MetaProgression
{
    /// <summary>Each light owns one delivery/production cycle and is immediately reusable when it finishes.</summary>
    public sealed partial class WorldFlashlightAvailability : MonoBehaviour
    {
        public const string LegacyUpgradeId = "world.flashlights";
        public const int MaximumCount = 6;
        [SerializeField] private GlobalStats flashlightStats;
        [SerializeField] private GameObject[] flashlights;
        [SerializeField, Min(.1f)] private float buildingSelectionRadius = 1.45f;
        [Header("Automatic work")]
        [SerializeField] private bool autoRepeat = true;
        [SerializeField] private UnityEngine.UI.Toggle autoRepeatToggle;
        public enum LightState { Ready, WaitingForResources, Working }
        public enum HoverState { None, Available, WaitingForResources, Working, RecallOnly, LightsBusy }
        public readonly struct HoverTarget
        {
            public readonly Transform Anchor;
            public readonly HoverState State;
            public readonly LogisticFlag Flag;
            internal readonly HexLightUnlocker Hex;
            internal readonly ResourceRequester Requester;
            internal readonly int LightIndex;
            public bool CanRecallHumans => Requester != null && Requester.CanRecallHumans;
            internal HoverTarget(Transform anchor, HoverState state, HexLightUnlocker hex = null,
                ResourceRequester requester = null, int lightIndex = -1, LogisticFlag flag = null)
            { Anchor = anchor; State = state; Hex = hex; Requester = requester; LightIndex = lightIndex; Flag = flag; }
        }
        public event Action<HoverTarget> TargetClicked;
        private sealed class LightAssignment
        {
            public CrystalLightBeam beam;
            public bool occupied, working;
            public bool restartPending;
            public int restartAfterFrame;
            public float retryAt;
            public HexLightUnlocker hex;
            public ResourceRequester requester;
            public TimerController[] timers;
            public UnityAction completed;
            public Action started;
            public Vector2 position;
            public AlarmSystem alarm;
            public readonly Dictionary<UnityEngine.Object, float> alarmSources = new();
        }
        private LightAssignment[] lights = Array.Empty<LightAssignment>();
        private bool escaped;
        private Camera worldCamera;
        private Vector2 rightPointerDown;
        private bool rightPointerStartedInWorld;
        private readonly List<RaycastResult> uiHits = new();
        private readonly Dictionary<ResourceRequester, TimerController[]> productionTimers = new();
        private readonly HashSet<UnityEngine.Object> pausedAlarmSources = new();
        public int LightCount => lights.Length;
        public bool Escaped => escaped;
        public bool AutoRepeat => autoRepeat;
        public LightState State(int index) => lights[index].occupied
            ? (lights[index].working ? LightState.Working : LightState.WaitingForResources)
            : LightState.Ready;
        private void Awake()
        {
            worldCamera = Camera.main;
            // Light ownership stays authoritative; the two legacy input handlers must not also move the flag.
            foreach (var root in flashlights)
            {
                if (root == null) continue;
                var beam = root.GetComponent<CrystalLightBeam>();
                if (beam != null) beam.Initialize();
            }
        }
        private void Start()
        {
            int count = Mathf.Min(flashlights.Length, flashlightStats != null ? flashlightStats.AvailableFlashlightCount : 1);
            var available = new List<LightAssignment>();
            for (int i = 0; i < flashlights.Length; i++)
            {
                var root = flashlights[i];
                if (root == null) continue;
                if (root != gameObject) root.SetActive(i < count);
                var beam = root.GetComponent<CrystalLightBeam>();
                if (i < count && beam != null) available.Add(new LightAssignment { beam = beam });
            }
            lights = available.ToArray();
            if (autoRepeatToggle != null)
            {
                autoRepeatToggle.SetIsOnWithoutNotify(autoRepeat);
                autoRepeatToggle.onValueChanged.AddListener(SetAutoRepeat);
            }
            InitializeDragControls();
            if (PortalTowerProgression.Instance != null) PortalTowerProgression.Instance.Changed += RefreshAvailableLights;
        }
        private void RefreshAvailableLights()
        {
            if (escaped) return;
            int count = Mathf.Min(flashlights.Length, flashlightStats != null ? flashlightStats.AvailableFlashlightCount : 1);
            if (count <= lights.Length) return;
            var available = new List<LightAssignment>(lights);
            for (int i = lights.Length; i < count; i++)
            {
                var root = flashlights[i];
                if (root == null) continue;
                root.SetActive(true);
                var beam = root.GetComponent<CrystalLightBeam>();
                if (beam == null) continue;
                available.Add(new LightAssignment { beam = beam, position = beam.MarkerPosition,
                    restartPending = LightsActivated && autoRepeat, restartAfterFrame = Time.frameCount + 1 });
                if (LightsActivated) beam.ShowIdle(beam.MarkerPosition);
            }
            lights = available.ToArray();
        }
        public bool IsPointerOverBlockingUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, uiHits);
            foreach (var hit in uiHits)
            {
                if (!(hit.module is UnityEngine.UI.GraphicRaycaster)) continue;
                var canvas = hit.gameObject.GetComponentInParent<Canvas>();
                // World progress bars are decorative and must not eat clicks on their hex.
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace ||
                    hit.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
            }
            return false;
        }
        private void Update()
        {
            if (escaped) return;
            for (int i = 0; i < lights.Length; i++)
            {
                var assignment = lights[i];
                if (assignment.occupied)
                {
                    bool targetGone = assignment.hex != null ? !assignment.hex.isActiveAndEnabled || assignment.hex.IsUnlocked()
                        : assignment.requester == null || !assignment.requester.isActiveAndEnabled;
                    if (targetGone) Release(i);
                }
            }
            ProcessDragPointer(Input.mousePosition, Input.GetMouseButtonDown(0),
                Input.GetMouseButton(0), Input.GetMouseButtonUp(0));
            if (Input.GetMouseButtonDown(1))
            {
                rightPointerDown = Input.mousePosition;
                rightPointerStartedInWorld = !IsPointerOverBlockingUI(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(1) && rightPointerStartedInWorld &&
                Vector2.Distance(rightPointerDown, Input.mousePosition) < 8 && !IsPointerOverBlockingUI(Input.mousePosition) && worldCamera != null)
                RecallHumansAt(worldCamera.ScreenToWorldPoint(Input.mousePosition));
            UpdateAutomaticWork();
        }
        public void SetAutoRepeat(bool enabled)
        {
            if (escaped) return;
            bool changed = autoRepeat != enabled;
            autoRepeat = enabled;
            if (autoRepeatToggle != null) autoRepeatToggle.SetIsOnWithoutNotify(enabled);
            if (!changed) return;
            if (!enabled)
            {
                const string key = "world.lights.manual_mode_hint";
                string hint = GameFoundation.Localization.LocalizationService.Instance?.Get(key);
                if (string.IsNullOrEmpty(hint) || hint == key)
                    hint = "Автоповтор выключен. ЛКМ по гексу с лучом запускает работу.";
                GameFoundation.UI.GameNotifications.Post(hint);
            }
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].restartPending = enabled && !lights[i].occupied;
                lights[i].restartAfterFrame = Time.frameCount + 1;
                lights[i].retryAt = 0;
            }
        }
        private void UpdateAutomaticWork()
        {
            if (!autoRepeat || !LightsActivated || GameSpeedControls.SimulationSpeed <= 0) return;
            for (int i = 0; i < lights.Length; i++)
            {
                var assignment = lights[i];
                if (!assignment.restartPending || assignment.occupied || i == draggedLight || i == pendingDragLight ||
                    !assignment.beam.MarkerVisible || Time.frameCount < assignment.restartAfterFrame ||
                    Time.unscaledTime < assignment.retryAt) continue;
                // Content is sometimes spawned after the completion event; full storage can also clear later.
                assignment.retryAt = Time.unscaledTime + .2f;
                TryStartAtLight(i);
            }
        }
        private bool TryStartAtLight(int index)
        {
            if (escaped || !LightsActivated || GameSpeedControls.SimulationSpeed <= 0 ||
                index < 0 || index >= lights.Length || lights[index].occupied || !lights[index].beam.MarkerVisible) return false;
            var target = ResolveTarget(lights[index].beam.MarkerPosition, index);
            if (target.State != HoverState.Available || target.LightIndex != index) return false;
            lights[index].restartPending = false;
            return target.Hex != null ? OpenHex(index, target.Hex) : ReserveBuilding(index, target.Requester);
        }
        /// <summary>Manual clicks can restart only the light already standing on this hex.</summary>
        public bool StartAtIlluminatedHex(Vector2 point)
        {
            if (escaped || !LightsActivated || IsDragging || GameSpeedControls.SimulationSpeed <= 0) return false;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].occupied || !lights[i].beam.MarkerVisible) continue;
                var clicked = ResolveTarget(lights[i].beam.ContainsMarkerPoint(point) ? lights[i].beam.MarkerPosition : point, i);
                if (clicked.State != HoverState.Available || clicked.Anchor == null) continue;
                var underLight = ResolveTarget(lights[i].beam.MarkerPosition, i);
                if (underLight.Anchor != clicked.Anchor || underLight.State != HoverState.Available) continue;
                TargetClicked?.Invoke(clicked);
                return TryStartAtLight(i);
            }
            return false;
        }
        // Both input and the hover view resolve the same target and state.
        public HoverTarget GetHoverTarget(Vector2 point) => ResolveTarget(point, draggedLight);
        public bool CanStartManually(HoverTarget target)
        {
            if (escaped || !LightsActivated || IsDragging || target.Anchor == null || target.State != HoverState.Available)
                return false;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].occupied || !lights[i].beam.MarkerVisible) continue;
                var underLight = ResolveTarget(lights[i].beam.MarkerPosition, i);
                if (underLight.Anchor == target.Anchor && underLight.State == HoverState.Available) return true;
            }
            return false;
        }
        private HoverTarget ResolveTarget(Vector2 point, int preferredLight)
        {
            if (escaped || !LightsActivated) return default;
            int occupiedIndex = -1;
            float occupiedDistance = float.MaxValue;
            for (int i = 0; i < lights.Length; i++)
                if (lights[i].occupied && lights[i].beam.ContainsMarkerPoint(point))
                {
                    float distance = (point - lights[i].position).sqrMagnitude;
                    if (distance < occupiedDistance) { occupiedDistance = distance; occupiedIndex = i; }
                }
            if (occupiedIndex >= 0)
            {
                var c = lights[occupiedIndex];
                var target = c.hex != null ? c.hex.transform : c.requester != null ? c.requester.transform : null;
                if (target != null && target.gameObject.activeInHierarchy)
                    return new HoverTarget(HexAnchor(target), c.working ? HoverState.Working : HoverState.WaitingForResources,
                        c.hex, c.requester, occupiedIndex, c.beam.Flag);
            }
            HexLightUnlocker hex = null;
            float nearest = float.MaxValue;
            foreach (var candidate in HexLightUnlocker.ActiveInstances)
            {
                if (!candidate.isActiveAndEnabled || candidate.IsUnlocked() || !candidate.IsPointOverHex(point)) continue;
                float distance = Vector2.SqrMagnitude(point - (Vector2)candidate.transform.position);
                if (distance < nearest) { nearest = distance; hex = candidate; }
            }
            int free = preferredLight >= 0 ? (!lights[preferredLight].occupied ? preferredLight : -1)
                : Array.FindIndex(lights, c => !c.occupied);
            var availability = free >= 0 ? HoverState.Available : HoverState.LightsBusy;
            if (hex != null)
            {
                var blocker = hex.GetComponentInParent<HexBlocker>();
                if (blocker != null && blocker.IsBlocked) return default;
                return new HoverTarget(HexAnchor(hex.transform), hex.IsUnlocking() && !hex.IsPaused ? HoverState.Working :
                    availability, hex: hex, lightIndex: free);
            }
            ResourceRequester requester = null;
            nearest = buildingSelectionRadius * buildingSelectionRadius;
            foreach (var candidate in ResourceRequester.ActiveInstances)
            {
                if (candidate == null || candidate.GetComponentInParent<HexBlocker>() != null ||
                    (!candidate.CanRecallHumans && (!candidate.CanSelectCrystalCycle || !HasProductionTimer(candidate)))) continue;
                float distance = Vector2.SqrMagnitude(point - (Vector2)candidate.transform.position);
                if (distance <= nearest) { requester = candidate; nearest = distance; }
            }
            return requester != null ? new HoverTarget(HexAnchor(requester.transform),
                !requester.CanSelectCrystalCycle || !HasProductionTimer(requester) ? HoverState.RecallOnly :
                availability, requester: requester, lightIndex: free) : default;
        }
        private static Transform HexAnchor(Transform target)
        {
            // HexMagnet belongs to the stationary tile, even when the building art is offset or animated.
            Transform anchor = target;
            float nearest = .75f * .75f;
            foreach (var magnet in HexMagnet.ActiveInstances)
            {
                if (magnet == null) continue;
                float distance = ((Vector2)(magnet.transform.position - target.position)).sqrMagnitude;
                if (distance < nearest) { nearest = distance; anchor = magnet.transform; }
            }
            return anchor;
        }
        private bool HasProductionTimer(ResourceRequester requester) => ProductionTimers(requester).Length > 0;
        public float GetActionAlarm(HoverTarget target) => GetActionAlarm(target, out _);
        public float GetActionAlarm(HoverTarget target, out float reservedForAction)
        {
            reservedForAction = 0;
            if ((target.Hex != null && target.Hex.IsPaused) ||
                (target.Requester != null && target.Requester.IsCyclePaused)) return 0f;
            if (target.State == HoverState.Working || target.State == HoverState.WaitingForResources)
            {
                if (target.LightIndex < 0 || target.LightIndex >= lights.Length) return 0;
                var assignment = lights[target.LightIndex];
                if (assignment.alarm != null)
                    foreach (var source in assignment.alarmSources.Keys)
                        reservedForAction += assignment.alarm.GetReservedActionAlarm(source);
                return reservedForAction;
            }
            if (target.State != HoverState.Available && target.State != HoverState.LightsBusy) return 0f;
            if (target.Hex != null)
            {
                var blocker = target.Hex.GetComponentInParent<HexBlocker>();
                return blocker != null ? blocker.UnlockAlarmAmount : 0f;
            }
            if (target.Requester == null) return 0f;
            float amount = AlarmEmitter.Preview(target.Requester.OnAllResourcesReceived) +
                AlarmEmitter.Preview(target.Requester.OnActionExecuted);
            foreach (var timer in ProductionTimers(target.Requester))
                amount += AlarmEmitter.Preview(timer.OnTimerEnd);
            return amount;
        }
        private TimerController[] ProductionTimers(ResourceRequester requester)
        {
            if (productionTimers.TryGetValue(requester, out var cached)) return cached;
            var result = new List<TimerController>();
            foreach (var timer in requester.GetComponentsInChildren<TimerController>(true))
                for (int i = 0; i < timer.OnTimerEnd.GetPersistentEventCount(); i++)
                    if (timer.OnTimerEnd.GetPersistentTarget(i) == requester && timer.OnTimerEnd.GetPersistentMethodName(i) == "FinishProcessing")
                    { result.Add(timer); break; }
            return productionTimers[requester] = result.ToArray();
        }
        public bool SelectAt(Vector2 point)
        {
            if (escaped || !LightsActivated || GameSpeedControls.SimulationSpeed <= 0) return false;
            var target = GetHoverTarget(point);
            if (target.State == HoverState.None) return false;
            TargetClicked?.Invoke(target);
            if (target.State == HoverState.WaitingForResources) { Release(target.LightIndex); GameFoundation.UI.GameNotifications.Post("Действие отменено"); return true; }
            if (target.State != HoverState.Available) return false;
            return target.Hex != null ? OpenHex(target.LightIndex, target.Hex) : ReserveBuilding(target.LightIndex, target.Requester);
        }
        public bool RecallHumansAt(Vector2 point)
        {
            if (escaped || !LightsActivated || GameSpeedControls.SimulationSpeed <= 0) return false;
            if (draggedLight >= 0) { CancelDrag(); return true; }
            var target = GetHoverTarget(point);
            if (target.State == HoverState.WaitingForResources)
            {
                TargetClicked?.Invoke(target);
                Release(target.LightIndex);
                if (target.CanRecallHumans && Warehouse.Instance != null)
                {
                    target.Requester.SetCrystalFlag(null);
                    target.Requester.RecallIdleHumans();
                }
                OrderManager.Instance?.ForceUpdateOrders();
                GameFoundation.UI.GameNotifications.Post("Действие отменено");
                return true;
            }
            if (Warehouse.Instance == null) return false;
            if (!target.CanRecallHumans) return false;
            // A recalled resident must not be dispatched again by a pending repeat at this position.
            foreach (var light in lights)
                if (Vector2.Distance(light.position, target.Anchor.position) <= buildingSelectionRadius)
                    light.restartPending = false;
            TargetClicked?.Invoke(target);
            target.Requester.SetCrystalFlag(null);
            int sent = target.Requester.RecallIdleHumans();
            if (sent > 0) GameFoundation.UI.GameNotifications.Post("Люди возвращаются в портал");
            OrderManager.Instance?.ForceUpdateOrders();
            return sent > 0;
        }
        private bool OpenHex(int index, HexLightUnlocker hex)
        {
            bool resuming = hex.IsPaused;
            var assignment = lights[index];
            assignment.restartPending = false;
            assignment.occupied = assignment.working = true;
            assignment.hex = hex;
            assignment.position = hex.transform.position;
            assignment.alarm = AlarmSystem.Instance;
            var blocker = hex.GetComponentInParent<HexBlocker>();
            if (blocker != null) assignment.alarmSources[blocker] = blocker.UnlockAlarmAmount;
            if (!resuming) ReserveAlarm(assignment);
            SetAlarmPaused(assignment, false);
            foreach (var source in assignment.alarmSources.Keys) pausedAlarmSources.Remove(source);
            assignment.completed = () => Release(index, true);
            hex.OnUnlockCompleteEvent.AddListener(assignment.completed);
            assignment.beam.Show(assignment.position, null, true, openingHex: true);
            hex.StartUnlockProcess(1);
            GameFoundation.UI.GameNotifications.Post(resuming ? "Открытие гекса продолжено" : "Начато открытие гекса");
            return true;
        }
        private bool ReserveBuilding(int index, ResourceRequester requester)
        {
            // Mine construction also contains a separate timer used only for its visuals.
            var timers = ProductionTimers(requester);
            if (timers.Length == 0) return false;
            bool resuming = requester.IsCyclePaused;
            var assignment = lights[index];
            assignment.restartPending = false;
            assignment.occupied = true;
            assignment.requester = requester;
            assignment.position = requester.transform.position;
            assignment.timers = timers;
            assignment.started = () => StartProduction(index);
            assignment.alarm = AlarmSystem.Instance;
            CollectAlarmSources(assignment, requester.OnAllResourcesReceived);
            CollectAlarmSources(assignment, requester.OnActionExecuted);
            foreach (var timer in timers) CollectAlarmSources(assignment, timer.OnTimerEnd);
            if (!resuming) ReserveAlarm(assignment);
            foreach (var source in assignment.alarmSources.Keys) pausedAlarmSources.Remove(source);
            assignment.completed = () => Release(index, true);
            foreach (var timer in assignment.timers)
            { timer.BindCrystalOwner(requester); timer.CrystalCycleStarted += assignment.started; }
            requester.OnActionExecuted.AddListener(assignment.completed);
            assignment.beam.Show(assignment.position, requester, true);
            if (!requester.CrystalResourcesReady) GameFoundation.UI.GameNotifications.Post("Здание ожидает доставку ресурсов");
            requester.SetCrystalFlag(assignment.beam.Flag);
            Physics2D.SyncTransforms();
            requester.TryStartCrystalCycle();
            if (requester.IsProcessing && !resuming)
                foreach (var timer in assignment.timers) if (!timer.IsRunning) timer.ResetTimer();
            OrderManager.Instance?.ForceUpdateOrders();
            return true;
        }
        private void StartProduction(int index)
        {
            var assignment = lights[index];
            if (!assignment.occupied || assignment.working || escaped) return;
            assignment.working = true;
            SetAlarmPaused(assignment, false);
            GameFoundation.UI.GameNotifications.Post("Здание начало работу");
            assignment.beam.Show(assignment.position, assignment.requester, true);
        }
        private static void CollectAlarmSources(LightAssignment assignment, UnityEvent action)
        {
            if (action == null) return;
            for (int i = 0; i < action.GetPersistentEventCount(); i++)
            {
                var source = AlarmEmitter.ConfiguredEmitter(action, i);
                if (source == null) continue;
                assignment.alarmSources.TryGetValue(source, out float amount);
                assignment.alarmSources[source] = amount + source.ConfiguredAmount;
            }
        }
        private static void ReserveAlarm(LightAssignment assignment)
        {
            if (assignment.alarm == null) return;
            foreach (var source in assignment.alarmSources) assignment.alarm.ReserveActionAlarm(source.Key, source.Value);
        }
        private static void SetAlarmPaused(LightAssignment assignment, bool paused)
        {
            if (assignment.alarm == null) return;
            foreach (var source in assignment.alarmSources.Keys) assignment.alarm.SetActionAlarmPaused(source, paused);
        }
        private void Release(int index, bool completed = false)
        {
            var assignment = lights[index];
            bool repeat = completed && assignment.occupied && autoRepeat && !escaped;
            bool preserveProgress = !completed && !escaped &&
                ((assignment.hex != null && assignment.hex.IsUnlocking()) ||
                 (assignment.requester != null && assignment.requester.IsProcessing));
            if (completed && assignment.occupied) GameFoundation.UI.GameNotifications.Post(assignment.hex != null ? "Гекс открыт" : "Работа здания завершена", GameFoundation.UI.NotificationKind.Positive);
            // Completion callbacks can precede the persistent alarm callback in the same event.
            // Keep its reservation until the emitter atomically transfers it into flying orbs.
            if (preserveProgress)
            {
                foreach (var source in assignment.alarmSources.Keys) pausedAlarmSources.Add(source);
                SetAlarmPaused(assignment, true);
            }
            else if (!completed && assignment.alarm != null)
                foreach (var source in assignment.alarmSources) assignment.alarm.CancelActionAlarm(source.Key);
            assignment.alarmSources.Clear();
            assignment.alarm = null;
            if (assignment.hex != null && assignment.completed != null)
            {
                assignment.hex.OnUnlockCompleteEvent.RemoveListener(assignment.completed);
                if (preserveProgress) assignment.hex.PauseUnlockProcess();
                else if (!assignment.hex.IsUnlocked()) assignment.hex.CancelUnlockProcess();
            }
            if (assignment.requester != null)
            {
                assignment.requester.OnActionExecuted.RemoveListener(assignment.completed);
                assignment.requester.SetCrystalFlag(null);
            }
            if (assignment.timers != null)
                foreach (var timer in assignment.timers) if (timer != null) timer.CrystalCycleStarted -= assignment.started;
            if (assignment.beam != null)
            {
                if (LightsActivated && !escaped) assignment.beam.ShowIdle(assignment.position);
                else assignment.beam.Clear();
            }
            assignment.hex = null; assignment.requester = null; assignment.timers = null;
            assignment.started = null; assignment.completed = null;
            assignment.occupied = assignment.working = false;
            assignment.restartPending = repeat;
            // Let production, spawning and alarm callbacks finish before starting another cycle.
            assignment.restartAfterFrame = Time.frameCount + 1;
            assignment.retryAt = 0;
            if (!escaped) OrderManager.Instance?.ForceUpdateOrders();
        }
        public void DisableAllForEscape()
        {
            PortalTowerProgression.Instance?.EndExpedition();
            escaped = true;
            StopAllCoroutines();
            pendingDragLight = draggedLight = -1;
            if (activationButton != null) activationButton.gameObject.SetActive(false);
            for (int i = 0; i < lights.Length; i++) Release(i);
            if (AlarmSystem.Instance != null)
                foreach (var source in pausedAlarmSources) AlarmSystem.Instance.CancelActionAlarm(source);
            pausedAlarmSources.Clear();
            foreach (var hex in HexLightUnlocker.ActiveInstances)
                if (hex != null && hex.IsPaused) hex.CancelUnlockProcess();
        }
        private void OnDisable()
        {
            if (PortalTowerProgression.Instance != null) PortalTowerProgression.Instance.Changed -= RefreshAvailableLights;
            if (autoRepeatToggle != null) autoRepeatToggle.onValueChanged.RemoveListener(SetAutoRepeat);
            DisableAllForEscape();
        }
    }
}
