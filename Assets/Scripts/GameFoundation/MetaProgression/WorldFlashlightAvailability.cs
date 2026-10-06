using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GameFoundation.MetaProgression
{
    /// <summary>One independent crystal cell owns each light and its single delivery/production cycle.</summary>
    public sealed class WorldFlashlightAvailability : MonoBehaviour
    {
        public const string LegacyUpgradeId = "world.flashlights";
        public const int MaximumCount = 6;
        [SerializeField] private GlobalStats flashlightStats;
        [SerializeField] private GameObject[] flashlights;
        [Header("Crystal charge")]
        [Tooltip("Standard time to recharge one empty cell at base power 1, when no other cell is charging.")]
        [SerializeField, Min(.1f)] private float rechargeSeconds = 30f;
        [Tooltip("Shared charging power. 1 = one cell at its standard recharge speed. Laboratory bonuses add to this base.")]
        [SerializeField, Min(0)] private float baseChargingPower = 1f;
        [SerializeField, Range(0, 1)] private float initialCharge = 1f;
        [SerializeField] private ResourceType rechargeResource;
        [SerializeField, Min(0)] private int rechargeCost = 1;
        [SerializeField, Min(.1f)] private float buildingSelectionRadius = 1.45f;
        public enum CellState { Charging, Ready, WaitingForResources, Working }
        public enum HoverState { None, Available, WaitingForResources, Working, NoEnergy, RecallOnly, CrystalBusy }
        public readonly struct HoverTarget
        {
            public readonly Transform Anchor;
            public readonly HoverState State;
            public readonly LogisticFlag Flag;
            internal readonly HexLightUnlocker Hex;
            internal readonly ResourceRequester Requester;
            internal readonly int CellIndex;
            public bool CanRecallHumans => Requester != null && Requester.CanRecallHumans;
            internal HoverTarget(Transform anchor, HoverState state, HexLightUnlocker hex = null,
                ResourceRequester requester = null, int cellIndex = -1, LogisticFlag flag = null)
            { Anchor = anchor; State = state; Hex = hex; Requester = requester; CellIndex = cellIndex; Flag = flag; }
        }
        public event Action<HoverTarget> TargetClicked;
        private sealed class Cell
        {
            public CrystalLightBeam beam;
            public float charge;
            public bool occupied, working;
            public HexLightUnlocker hex;
            public ResourceRequester requester;
            public TimerController[] timers;
            public UnityAction completed;
            public Action started;
            public Vector2 position;
            public AlarmSystem alarm;
            public readonly Dictionary<UnityEngine.Object, float> alarmSources = new();
        }
        private Cell[] cells = Array.Empty<Cell>();
        private bool escaped;
        private Camera worldCamera;
        private Vector2 pointerDown;
        private bool pointerStartedInWorld;
        private Vector2 rightPointerDown;
        private bool rightPointerStartedInWorld;
        private readonly List<RaycastResult> uiHits = new();
        private readonly Dictionary<ResourceRequester, TimerController[]> productionTimers = new();
        public int CellCount => cells.Length;
        public ResourceType RechargeResource => rechargeResource;
        public int RechargeCost => rechargeCost;
        public bool Escaped => escaped;
        public float ChargingPower => Mathf.Max(0, baseChargingPower) * (flashlightStats != null ? flashlightStats.CrystalChargingPowerMultiplier : 1f);
        public int ChargingCellCount
        {
            get { int count = 0; foreach (var cell in cells) if (!cell.occupied && cell.charge < 1f) count++; return count; }
        }
        public float Charge(int index) => cells[index].charge;
        public CellState State(int index) => cells[index].occupied
            ? (cells[index].working ? CellState.Working : CellState.WaitingForResources)
            : cells[index].charge >= 1 ? CellState.Ready : CellState.Charging;
        public bool CanRecharge
        {
            get
            {
                if (escaped || rechargeResource == null || GlobalResourceManager.Instance == null ||
                    GlobalResourceManager.Instance.GetResourceAmount(rechargeResource) < rechargeCost) return false;
                foreach (var cell in cells) if (cell.charge < 1) return true;
                return false;
            }
        }
        private void Awake()
        {
            worldCamera = Camera.main;
            // Disable the legacy drag/activation path before the first frame, including locked beams.
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
            var available = new List<Cell>();
            for (int i = 0; i < flashlights.Length; i++)
            {
                var root = flashlights[i];
                if (root == null) continue;
                if (root != gameObject) root.SetActive(i < count);
                var beam = root.GetComponent<CrystalLightBeam>();
                if (i < count && beam != null) available.Add(new Cell { beam = beam, charge = initialCharge });
            }
            cells = available.ToArray();
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
            float dt = Time.unscaledDeltaTime * GameSpeedControls.SimulationSpeed;
            for (int i = 0; i < cells.Length; i++)
            {
                var cell = cells[i];
                if (cell.occupied)
                {
                    bool targetGone = cell.hex != null ? !cell.hex.isActiveAndEnabled || cell.hex.IsUnlocked()
                        : cell.requester == null || !cell.requester.isActiveAndEnabled;
                    if (targetGone) Release(i);
                }
            }
            TickRecharge(dt);
            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = Input.mousePosition;
                pointerStartedInWorld = !IsPointerOverBlockingUI(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(0) && pointerStartedInWorld &&
                Vector2.Distance(pointerDown, Input.mousePosition) < 8 && !IsPointerOverBlockingUI(Input.mousePosition) && worldCamera != null)
                SelectAt(worldCamera.ScreenToWorldPoint(Input.mousePosition));
            if (Input.GetMouseButtonDown(1))
            {
                rightPointerDown = Input.mousePosition;
                rightPointerStartedInWorld = !IsPointerOverBlockingUI(Input.mousePosition);
            }
            if (Input.GetMouseButtonUp(1) && rightPointerStartedInWorld &&
                Vector2.Distance(rightPointerDown, Input.mousePosition) < 8 && !IsPointerOverBlockingUI(Input.mousePosition) && worldCamera != null)
                RecallHumansAt(worldCamera.ScreenToWorldPoint(Input.mousePosition));
        }
        /// <summary>Distribute this simulation step's finite power budget equally among charging cells.</summary>
        public void TickRecharge(float simulationSeconds)
        {
            if (escaped || simulationSeconds <= 0) return;
            float budget = simulationSeconds * ChargingPower / Mathf.Max(.1f, rechargeSeconds);
            // At most one pass per cell: every non-final pass finishes at least one cell.
            // Redistribute the remainder immediately, so completion does not waste power at low frame rates.
            for (int pass = 0; pass < cells.Length && budget > 0; pass++)
            {
                int count = 0;
                float nearestFull = 1f;
                foreach (var cell in cells)
                    if (!cell.occupied && cell.charge < 1f)
                    { count++; nearestFull = Mathf.Min(nearestFull, 1f - cell.charge); }
                if (count == 0) break;
                float share = Mathf.Min(budget / count, nearestFull);
                foreach (var cell in cells)
                    if (!cell.occupied && cell.charge < 1f)
                        cell.charge = 1f - cell.charge <= share ? 1f : cell.charge + share;
                budget = Mathf.Max(0, budget - share * count);
            }
        }
        // Both input and the hover view resolve the same target and state.
        public HoverTarget GetHoverTarget(Vector2 point)
        {
            if (escaped) return default;
            for (int i = 0; i < cells.Length; i++)
                if (cells[i].occupied && Vector2.Distance(point, cells[i].position) <= buildingSelectionRadius)
                {
                    var c = cells[i];
                    var target = c.hex != null ? c.hex.transform : c.requester != null ? c.requester.transform : null;
                    if (target != null && target.gameObject.activeInHierarchy)
                        return new HoverTarget(HexAnchor(target), c.working ? HoverState.Working : HoverState.WaitingForResources,
                            c.hex, c.requester, i, c.beam != null ? c.beam.Flag : null);
                }
            HexLightUnlocker hex = null;
            float nearest = float.MaxValue;
            foreach (var candidate in HexLightUnlocker.ActiveInstances)
            {
                if (!candidate.isActiveAndEnabled || candidate.IsUnlocked() || !candidate.IsPointOverHex(point)) continue;
                float distance = Vector2.SqrMagnitude(point - (Vector2)candidate.transform.position);
                if (distance < nearest) { nearest = distance; hex = candidate; }
            }
            int free = Array.FindIndex(cells, c => !c.occupied && c.charge >= 1f);
            // Paid recharge can fill an occupied cell without releasing its beam.
            var availability = free >= 0 ? HoverState.Available :
                Array.Exists(cells, c => c.occupied && c.charge >= 1f) ? HoverState.CrystalBusy : HoverState.NoEnergy;
            if (hex != null)
            {
                var blocker = hex.GetComponentInParent<HexBlocker>();
                if (blocker != null && blocker.IsBlocked) return default;
                return new HoverTarget(HexAnchor(hex.transform), hex.IsUnlocking() ? HoverState.Working :
                    availability, hex: hex, cellIndex: free);
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
                availability, requester: requester, cellIndex: free) : default;
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
        public float GetActionAlarm(HoverTarget target)
        {
            if (target.State != HoverState.Available && target.State != HoverState.NoEnergy &&
                target.State != HoverState.CrystalBusy) return 0f;
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
            if (escaped || GameSpeedControls.SimulationSpeed <= 0) return false;
            var target = GetHoverTarget(point);
            if (target.State == HoverState.None) return false;
            TargetClicked?.Invoke(target);
            if (target.State == HoverState.WaitingForResources) { Release(target.CellIndex); return true; }
            if (target.State != HoverState.Available) return false;
            return target.Hex != null ? OpenHex(target.CellIndex, target.Hex) : ReserveBuilding(target.CellIndex, target.Requester);
        }
        public bool RecallHumansAt(Vector2 point)
        {
            if (escaped || GameSpeedControls.SimulationSpeed <= 0 || Warehouse.Instance == null) return false;
            var target = GetHoverTarget(point);
            if (!target.CanRecallHumans) return false;
            TargetClicked?.Invoke(target);
            // Cancel the delivery order first, so no replacement worker is dispatched during recall.
            if (target.State == HoverState.WaitingForResources) Release(target.CellIndex);
            target.Requester.SetCrystalFlag(null);
            int sent = target.Requester.RecallIdleHumans();
            OrderManager.Instance?.ForceUpdateOrders();
            return sent > 0;
        }
        private bool OpenHex(int index, HexLightUnlocker hex)
        {
            var cell = cells[index];
            cell.occupied = cell.working = true;
            cell.charge = 0;
            cell.hex = hex;
            cell.position = hex.transform.position;
            cell.alarm = AlarmSystem.Instance;
            var blocker = hex.GetComponentInParent<HexBlocker>();
            if (blocker != null) cell.alarmSources[blocker] = blocker.UnlockAlarmAmount;
            ReserveAlarm(cell);
            cell.completed = () => Release(index, true);
            hex.OnUnlockCompleteEvent.AddListener(cell.completed);
            cell.beam.Show(cell.position, null, true);
            hex.StartUnlockProcess(1);
            return true;
        }
        private bool ReserveBuilding(int index, ResourceRequester requester)
        {
            // Mine construction also contains a separate timer used only for its visuals.
            var timers = ProductionTimers(requester);
            if (timers.Length == 0) return false;
            var cell = cells[index];
            cell.occupied = true;
            cell.requester = requester;
            cell.position = requester.transform.position;
            cell.timers = timers;
            cell.started = () => StartProduction(index);
            cell.alarm = AlarmSystem.Instance;
            CollectAlarmSources(cell, requester.OnAllResourcesReceived);
            CollectAlarmSources(cell, requester.OnActionExecuted);
            foreach (var timer in timers) CollectAlarmSources(cell, timer.OnTimerEnd);
            ReserveAlarm(cell);
            cell.completed = () => Release(index, true);
            foreach (var timer in cell.timers)
            { timer.BindCrystalOwner(requester); timer.CrystalCycleStarted += cell.started; }
            requester.OnActionExecuted.AddListener(cell.completed);
            cell.beam.Show(cell.position, requester, false);
            requester.SetCrystalFlag(cell.beam.Flag);
            Physics2D.SyncTransforms();
            requester.TryStartCrystalCycle();
            if (requester.IsProcessing)
                foreach (var timer in cell.timers) if (!timer.IsRunning) timer.ResetTimer();
            OrderManager.Instance?.ForceUpdateOrders();
            return true;
        }
        private void StartProduction(int index)
        {
            var cell = cells[index];
            if (!cell.occupied || cell.working || escaped) return;
            cell.charge = 0;
            cell.working = true;
            cell.beam.Show(cell.position, cell.requester, true);
        }
        private static void CollectAlarmSources(Cell cell, UnityEvent action)
        {
            if (action == null) return;
            for (int i = 0; i < action.GetPersistentEventCount(); i++)
            {
                var source = AlarmEmitter.ConfiguredEmitter(action, i);
                if (source == null) continue;
                cell.alarmSources.TryGetValue(source, out float amount);
                cell.alarmSources[source] = amount + source.ConfiguredAmount;
            }
        }
        private static void ReserveAlarm(Cell cell)
        {
            if (cell.alarm == null) return;
            foreach (var source in cell.alarmSources) cell.alarm.ReserveActionAlarm(source.Key, source.Value);
        }
        private void Release(int index, bool completed = false)
        {
            var cell = cells[index];
            // Completion callbacks can precede the persistent alarm callback in the same event.
            // Keep its reservation until the emitter atomically transfers it into flying orbs.
            if (!completed && cell.alarm != null)
                foreach (var source in cell.alarmSources) cell.alarm.CancelActionAlarm(source.Key);
            cell.alarmSources.Clear();
            cell.alarm = null;
            if (cell.hex != null && cell.completed != null)
            {
                cell.hex.OnUnlockCompleteEvent.RemoveListener(cell.completed);
                if (!cell.hex.IsUnlocked()) cell.hex.CancelUnlockProcess();
            }
            if (cell.requester != null)
            {
                cell.requester.OnActionExecuted.RemoveListener(cell.completed);
                cell.requester.SetCrystalFlag(null);
            }
            if (cell.timers != null)
                foreach (var timer in cell.timers) if (timer != null) timer.CrystalCycleStarted -= cell.started;
            if (cell.beam != null) cell.beam.Clear();
            cell.hex = null; cell.requester = null; cell.timers = null;
            cell.started = null; cell.completed = null;
            cell.occupied = cell.working = false;
            if (!escaped) OrderManager.Instance?.ForceUpdateOrders();
        }
        public bool RechargeAll()
        {
            if (!CanRecharge || !GlobalResourceManager.Instance.TrySpendResource(rechargeResource, rechargeCost)) return false;
            foreach (var cell in cells) cell.charge = 1;
            return true;
        }
        // Retain the old UnityEvent API; the obsolete button is removed from the prefab.
        public void ActivateAvailableLights() { }
        public void DisableAllForEscape()
        {
            escaped = true;
            for (int i = 0; i < cells.Length; i++) Release(i);
        }
        private void OnDisable() => DisableAllForEscape();
    }
}
