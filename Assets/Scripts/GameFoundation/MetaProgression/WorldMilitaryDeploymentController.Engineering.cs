using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed partial class WorldMilitaryDeploymentController
    {
        private const string EngineerId = "Engineer";
        private readonly List<WorldBallista> ballistas = new();
        private readonly List<Vector2> buildSites = new();
        private readonly Dictionary<Vector2, ResourceRequester> ballistaOrders = new();

        internal bool HasLivingEngineer
        {
            get
            {
                foreach (var control in additionalUnitTypes)
                {
                    if (control?.resource?.Id != EngineerId) continue;
                    foreach (var unit in control.deployed)
                        if (unit != null && unit.activeInHierarchy && unit.GetComponent<WorldMilitaryReturner>() == null &&
                            unit.TryGetComponent<Health>(out var health) && !health.IsDead) return true;
                }
                return false;
            }
        }

        internal bool CanOrderBallista(Vector2 site) => HasLivingEngineer && CanOrderDefense(site);

        internal bool CanOrderDefense(Vector2 site) => !ballistaOrders.ContainsKey(site) &&
            !defenseTowers.ContainsKey(site) && BallistaAt(site) == null && IsBallistaSiteLit(site) &&
            CombatTargetRegistry.Closest("Enemy1", site, home:site, homeRange:1.45f) == null;

        internal bool OrderBallista(Vector2 site)
        {
            if (!CanOrderBallista(site)) return false;
            var prefab = Resources.Load<GameObject>("Engineering/Ballista Foundation")?.GetComponent<ResourceRequester>();
            if (prefab == null) return false;
            var request = Instantiate(prefab, site, Quaternion.identity, transform);
            var order = request.gameObject;
            request.SetDedicatedDeliveryGate(() => IsBallistaSiteLit(site));
            ballistaOrders.Add(site, request);
            order.SetActive(true);
            OrderManager.Instance?.ForceUpdateOrders();
            return true;
        }

        private void RemoveBallistaRequest(ResourceRequester request)
        {
            if (request == null) return;
            OrderManager.Instance?.CancelDeliveries(request);
            request.gameObject.SetActive(false);
            Destroy(request.gameObject);
        }

        internal IEnumerable<Vector2> BallistaOrders => ballistaOrders.Keys;
        internal IEnumerable<WorldBallista> BuiltBallistas => ballistas;
        internal bool BallistaMaterialsReady(Vector2 site) => ballistaOrders.TryGetValue(site, out var request) && request.CrystalResourcesReady;
        internal bool HasBallistaOrder(Vector2 site) => ballistaOrders.ContainsKey(site);

        internal void DemolishBallista(Vector2 site)
        {
            if (ballistaOrders.Remove(site, out var request))
            {
                RemoveBallistaRequest(request);
                foreach (var control in additionalUnitTypes)
                {
                    if (control?.resource?.Id != EngineerId) continue;
                    foreach (var unit in control.deployed)
                        if (unit != null && unit.TryGetComponent<WorldEngineer>(out var engineer))
                            engineer.CancelOrderAt(site);
                }
            }
            if (defenseTowers.Remove(site, out var defense))
                defense.Remove();
            var ballista = BallistaAt(site);
            if (ballista == null) return;
            ballistas.Remove(ballista);
            ballista.gameObject.SetActive(false);
            Destroy(ballista.gameObject);
        }

        private void RegisterEngineers()
        {
            var resource = ResourceCatalog.Find(EngineerId);
            var prefab = Resources.Load<GameObject>("Engineering/Engineer");
            if (resource != null && prefab != null &&
                !additionalUnitTypes.Exists(control => control?.resource == resource))
                additionalUnitTypes.Add(new UnitControl { resource = resource, prefab = prefab });
        }

        internal Vector2 PortalPosition => portal != null ? (Vector2)portal.position : Vector2.zero;

        internal bool CanBuildBallista => !escapeRecall && portal != null && !PortalCloudTransition.IsLoading;

        internal bool FindBallistaSite(WorldEngineer engineer, out Vector2 site)
        {
            site = default;
            if (!CanBuildBallista || militaryLights == null) return false;
            militaryLights.CollectBallistaSites(buildSites, portal.position);
            float nearest = float.PositiveInfinity;
            foreach (var candidate in buildSites)
            {
                var station = BallistaAt(candidate);
                if (station != null && station.Operator != null) continue;
                if (station == null && (defenseTowers.ContainsKey(candidate) ||
                    (!HasBallistaOrder(candidate) && CombatTargetRegistry.Closest("Enemy1", candidate,
                        home: candidate, homeRange: 1.45f) != null))) continue;
                bool reserved = false;
                foreach (var unit in additionalUnitTypes)
                {
                    if (unit?.resource?.Id != EngineerId) continue;
                    foreach (var deployed in unit.deployed)
                        if (deployed != null && deployed.TryGetComponent<WorldEngineer>(out var other) &&
                            other != engineer && other.Reserves(candidate))
                            reserved = true;
                }
                if (reserved) continue;
                float distance = (candidate - (Vector2)engineer.transform.position).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                site = candidate;
            }
            return nearest < float.PositiveInfinity;
        }

        internal bool IsBallistaSiteLit(Vector2 site)
        {
            if (!CanBuildBallista || militaryLights == null) return false;
            militaryLights.CollectBallistaSites(buildSites, portal.position);
            return buildSites.Exists(candidate => (candidate - site).sqrMagnitude < .1f);
        }

        internal void MoveEngineer(WorldEngineer engineer, Vector2? site)
        {
            if (!CanBuildBallista || engineer.GetComponent<WorldMilitaryReturner>() != null) return;
            GameObject unit = engineer.gameObject;
            if (site.HasValue)
            {
                var mover = unit.GetComponent<WorldMilitaryArrivalMover>() ?? unit.AddComponent<WorldMilitaryArrivalMover>();
                mover.Begin(site.Value);
            }
            else
            {
                formation.Remove(unit);
                AssignFormation(unit, PortalWaitingPoint(unit, portal.position), true);
            }
        }

        internal WorldBallista BallistaAt(Vector2 site)
        {
            return ballistas.Find(ballista => ballista != null && ballista.IsAlive &&
                ((Vector2)ballista.transform.position - site).sqrMagnitude < .1f);
        }

        internal WorldBallista CompleteBallista(Vector2 site)
        {
            var existing = BallistaAt(site);
            if (existing != null) return existing;
            var prefab = Resources.Load<GameObject>("Engineering/Ballista")?.GetComponent<WorldBallista>();
            if (prefab == null || !IsBallistaSiteLit(site) || !ballistaOrders.TryGetValue(site, out var request) ||
                !request.CrystalResourcesReady) return null;
            ballistaOrders.Remove(site);
            RemoveBallistaRequest(request);
            var ballista = Instantiate(prefab, site, Quaternion.identity);
            ballistas.Add(ballista);
            GameFoundation.Audio.GameAudioController.PlayAt(GameFoundation.Audio.GameAudioCue.BuildingComplete, site, .8f, .98f, 1.02f, .1f);
            return ballista;
        }

        private void ClearBallistas()
        {
            foreach (var ballista in ballistas)
            {
                if (ballista == null) continue;
                ballista.Operator?.LeaveBallista();
                ballista.gameObject.SetActive(false);
                Destroy(ballista.gameObject);
            }
            ballistas.Clear();
            foreach (var request in ballistaOrders.Values) RemoveBallistaRequest(request);
            ballistaOrders.Clear();
            foreach (var defense in defenseTowers.Values) if (defense != null) defense.Remove();
            defenseTowers.Clear();
        }
    }

    public sealed partial class WorldFlashlightAvailability
    {
        internal void CollectBallistaSites(List<Vector2> result, Vector2 portalCenter)
        {
            result.Clear();
            if (escaped) return;
            for (int index = 0; index < lights.Length; index++)
            {
                var light = lights[index];
                if (!light.military || light.beam == null || !light.beam.MarkerVisible || light.hex != null ||
                    light.requester != null || (draggedLight == index && dragDetached)) continue;
                var anchor = OpenMilitaryHex(light.position);
                if (anchor == null) continue;
                Vector2 center = anchor.position;
                if ((center - portalCenter).sqrMagnitude < 1f || WorldHexFog.BlocksWork(center) ||
                    (center - light.position).sqrMagnitude > .5f * .5f || result.Contains(center)) continue;

                bool occupied = false;
                foreach (var requester in ResourceRequester.ActiveInstances)
                    if (requester != null && !requester.HasDedicatedDelivery && requester is not DefenseTowerRequest && requester.isActiveAndEnabled &&
                        ((Vector2)requester.transform.position - center).sqrMagnitude < 1.45f * 1.45f)
                    {
                        occupied = true;
                        break;
                    }
                if (occupied) continue;
                result.Add(center);
            }
        }
    }
}
