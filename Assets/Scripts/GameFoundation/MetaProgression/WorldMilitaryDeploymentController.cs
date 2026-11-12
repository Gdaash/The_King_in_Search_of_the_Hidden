using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Audio;

namespace GameFoundation.MetaProgression
{
    public sealed partial class WorldMilitaryDeploymentController : MonoBehaviour
    {
        [Serializable]
        public sealed class UnitControl
        {
            public ResourceType resource;
            public GameObject prefab;
            public Button summonButton;
            public Button recallButton;
            public Text storedAmount;
            public Text deployedAmount;
            [NonSerialized] public readonly List<GameObject> deployed = new List<GameObject>();
        }

        [SerializeField] private Transform portal;
        [SerializeField, Min(0f)] private float departureMinRadius = 0.65f;
        [SerializeField, Min(0.1f)] private float departureMaxRadius = 1.25f;
        [Header("Автоматический выход и улучшения")]
        [SerializeField] private GlobalStats scientificStats;
        [SerializeField, Min(0.1f)] private float baseRecoveryRadius = 1.8f;
        [SerializeField] private UnitControl swordsmen = new UnitControl();
        [SerializeField] private UnitControl archers = new UnitControl();

        [SerializeField] private List<UnitControl> additionalUnitTypes = new();
        private IEnumerable<UnitControl> Controls => new[] { swordsmen, archers }.Concat(additionalUnitTypes).Where(c => c != null).Distinct();

        private void Awake()
        {
            RegisterEngineers();
            var slinger = ResourceCatalog.Find("Slinger");
            var slingerPrefab = Resources.Load<GameObject>("Units/Slinger");
            if (slinger != null && slingerPrefab != null && !additionalUnitTypes.Exists(unit => unit?.resource == slinger))
                additionalUnitTypes.Add(new UnitControl { resource = slinger, prefab = slingerPrefab });
            foreach (var control in Controls) Bind(control);
        }

        private void Start()
        {
            // A battle begins with the whole garrison stepping through the portal.
            foreach (var control in Controls) DeployAll(control);
            Refresh();
            InitializeMilitaryLights();
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() { GlobalResourceManager.OnResourceChanged -= OnResourceChanged; if (militaryLights != null) militaryLights.MilitaryPositionsChanged -= RebuildFormation; }

        private void OnDestroy()
        {
            escapeRecall = true;
            ClearBallistas();
            FinalizeSurvivors();
        }
        public void FinalizeSurvivors()
        {
            foreach (var control in Controls) ReturnSurvivors(control);
        }

        private void Bind(UnitControl control)
        {
            // The controls are kept as optional legacy references so old prefabs remain valid.
            // Deployment is now automatic and returning happens only through escape or a retreat upgrade.
            if (control.summonButton != null) control.summonButton.gameObject.SetActive(false);
            if (control.recallButton != null) control.recallButton.gameObject.SetActive(false);
        }

        public IReadOnlyList<GameObject> ActiveUnits
        {
            get
            {
                foreach (var control in Controls) RemoveDestroyed(control);
                return Controls.SelectMany(c => c.deployed)
                    .Concat(ballistas.Where(ballista => ballista != null && ballista.IsAlive).Select(ballista => ballista.gameObject)).ToArray();
            }
        }

        public bool IsMilitaryResource(ResourceType resource) => resource != null &&
            Controls.Any(c => c.resource == resource);

        // Deployment only moves a warrior out of storage; it is not a resource loss.
        // Include living deployed warriors in run/day snapshots, including before
        // scene teardown returns survivors to storage.
        public void AddDeployedToSnapshot(Dictionary<ResourceType, int> snapshot)
        {
            foreach (var control in Controls) AddDeployedToSnapshot(control, snapshot);
        }

        private static void AddDeployedToSnapshot(UnitControl control, Dictionary<ResourceType, int> snapshot)
        {
            if (control.resource == null) return;
            RemoveDestroyed(control);
            snapshot.TryGetValue(control.resource, out int stored);
            snapshot[control.resource] = stored + control.deployed.Count;
        }

        /// <summary>Recount tracked troops and deploy all newly stored warriors, also after battle start.</summary>
        public void RefreshAndDeployStoredWarriors()
        {
            foreach (var control in Controls) RemoveDestroyed(control);
            if (!escapeRecall && portal != null && !PortalCloudTransition.IsLoading)
                foreach (var control in Controls) DeployAll(control);
            RebuildFormation();
            Refresh();
        }

        private void DeployAll(UnitControl control)
        {
            if (control.resource == null || GlobalResourceManager.Instance == null) return;
            int count = GlobalResourceManager.Instance.GetResourceAmount(control.resource);
            IReadOnlyList<MilitaryProfile> profiles = MilitaryExperienceService.GetStored(control.resource, count);
            for (int i = 0; i < profiles.Count; i++) Summon(control, profiles[i]);
        }

        private void Summon(UnitControl control, MilitaryProfile profile)
        {
            if (portal == null || control.resource == null || control.prefab == null ||
                GlobalResourceManager.Instance == null ||
                !GlobalResourceManager.Instance.TrySpendResource(control.resource, 1)) return;

            var unit = Instantiate(control.prefab, portal.position, Quaternion.identity);
            GameAudioController.PlayAt(GameAudioCue.Portal, portal.position, 0.72f, 0.96f, 1.04f, 0.08f);
            unit.name = control.prefab.name;
            control.deployed.Add(unit);
            ConfigureWarrior(unit, control, profile);
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
            float maxRadius = Mathf.Max(departureMinRadius, departureMaxRadius);
            float radius = UnityEngine.Random.Range(Mathf.Min(departureMinRadius, maxRadius), maxRadius);
            WorldMilitaryArrivalMover mover = unit.AddComponent<WorldMilitaryArrivalMover>();
            mover.Begin((Vector2)portal.position + direction * radius);
            Refresh();
        }

        private void ConfigureWarrior(GameObject unit, UnitControl control, MilitaryProfile profile)
        {
            if (unit == null) return;
            if (unit.TryGetComponent<WorldEngineer>(out var engineer)) engineer.Initialize(this);
            var loss=unit.GetComponent<WorldLossIdentity>()??unit.AddComponent<WorldLossIdentity>();loss.resource=control.resource;
            MilitaryExperience experience = unit.GetComponent<MilitaryExperience>() ?? unit.AddComponent<MilitaryExperience>();
            experience.Initialize(profile);
            WorldMilitaryLowHealthRetreat retreat = unit.GetComponent<WorldMilitaryLowHealthRetreat>() ?? unit.AddComponent<WorldMilitaryLowHealthRetreat>();
            retreat.Initialize(this, scientificStats);
            WorldMilitaryBaseRegen regeneration = unit.GetComponent<WorldMilitaryBaseRegen>() ?? unit.AddComponent<WorldMilitaryBaseRegen>();
            regeneration.Initialize(portal, scientificStats, baseRecoveryRadius);
            Health health = unit.GetComponent<Health>();
            if (health != null) health.OnDeath.AddListener(() => HandleWarriorDeath(control, unit));
        }

        private void Recall(UnitControl control)
        {
            RemoveDestroyed(control);
            if (control.deployed.Count == 0 || GlobalResourceManager.Instance == null || control.resource == null) return;
            GameObject unit = null;
            for (int i = control.deployed.Count - 1; i >= 0; i--)
            {
                GameObject candidate = control.deployed[i];
                if (candidate != null && candidate.GetComponent<WorldMilitaryReturner>() == null)
                {
                    unit = candidate;
                    break;
                }
            }
            if (unit == null || portal == null) return;

            unit.GetComponent<WorldEngineer>()?.LeaveBallista();
            WorldMilitaryReturner returner = unit.AddComponent<WorldMilitaryReturner>();
            GameAudioController.PlayAt(GameAudioCue.Portal, portal.position, 0.55f, 0.94f, 1.02f, 0.08f);
            returner.Begin(portal, returnedUnit => CompleteRecall(control, returnedUnit));
            Refresh();
        }

        public void BeginEscapeRecall()
        {
            escapeRecall = true;
            ClearBallistas();
            foreach (var control in Controls) BeginEscapeRecall(control);
            Refresh();
        }
        public void SettleEscapeLosses()
        {
            foreach (var control in Controls) SettleEscapeLosses(control);
        }
        private static void SettleEscapeLosses(UnitControl control)
        {
            foreach(var unit in control.deployed)
            {
                if(unit==null)continue;
                if(unit.TryGetComponent<Health>(out var health)&&health.IsDead)WorldCasualties.Death(control.resource,unit);
                else WorldCasualties.DidNotReturn(control.resource,1);
                unit.GetComponent<MilitaryExperience>()?.Die();Destroy(unit);
            }
            control.deployed.Clear();
        }

        /// <summary>Called by a deployed warrior when an active decree requests a low-health retreat.</summary>
        public bool RequestLowHealthRetreat(GameObject unit)
        {
            if (unit == null || portal == null || unit.GetComponent<WorldMilitaryReturner>() != null) return false;
            UnitControl control = Controls.FirstOrDefault(c => c.deployed.Contains(unit));
            if (control == null) return false;
            unit.GetComponent<WorldEngineer>()?.LeaveBallista();
            WorldMilitaryReturner returner = unit.AddComponent<WorldMilitaryReturner>();
            returner.Begin(portal, returnedUnit => CompleteRecall(control, returnedUnit));
            Refresh();
            return true;
        }

        public bool HasReturningUnits
        {
            get
            {
                foreach (var control in Controls) RemoveDestroyed(control);
                return Controls.Any(c => c.deployed.Count > 0);
            }
        }

        private void BeginEscapeRecall(UnitControl control)
        {
            RemoveDestroyed(control);
            foreach (GameObject unit in control.deployed)
            {
                if (unit == null || unit.GetComponent<WorldMilitaryReturner>() != null) continue;
                unit.GetComponent<WorldEngineer>()?.LeaveBallista();
            WorldMilitaryReturner returner = unit.AddComponent<WorldMilitaryReturner>();
                returner.Begin(portal, returnedUnit => CompleteRecall(control, returnedUnit));
            }
        }

        private void CompleteRecall(UnitControl control, GameObject unit)
        {
            if (!control.deployed.Remove(unit)) return;
            MilitaryExperience experience = unit != null ? unit.GetComponent<MilitaryExperience>() : null;
            experience?.CaptureHealth();
            experience?.ReturnToBase();
            if (GlobalResourceManager.Instance != null && control.resource != null)
                GlobalResourceManager.Instance.AddResource(control.resource, 1);
            if (unit != null) Destroy(unit);
            Refresh();
        }

        private void HandleWarriorDeath(UnitControl control, GameObject unit)
        {
            if (!control.deployed.Remove(unit)) return;
            unit?.GetComponent<MilitaryExperience>()?.Die();
            Refresh();
        }

        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            foreach (var control in Controls) Refresh(control);
        }

        private void Refresh(UnitControl control)
        {
            RemoveDestroyed(control);
            int stored = GlobalResourceManager.Instance != null && control.resource != null
                ? GlobalResourceManager.Instance.GetResourceAmount(control.resource) : 0;
            if (control.storedAmount != null) control.storedAmount.text = stored.ToString();
            if (control.deployedAmount != null) control.deployedAmount.text = control.deployed.Count.ToString();
            if (control.summonButton != null) control.summonButton.gameObject.SetActive(false);
            if (control.recallButton != null) control.recallButton.gameObject.SetActive(false);
        }

        private static void RemoveDestroyed(UnitControl control) => control.deployed.RemoveAll(unit =>
            unit == null || (unit.TryGetComponent<Health>(out var health) && health.IsDead));

        public void AbandonDeployedUnits()
        {
            ClearBallistas();
            foreach (var control in Controls) Abandon(control);
        }

        private static void Abandon(UnitControl control)
        {
            foreach (var unit in control.deployed)
                if (unit != null) { unit.GetComponent<MilitaryExperience>()?.Die(); Destroy(unit); }
            control.deployed.Clear();
        }

        private static void ReturnSurvivors(UnitControl control)
        {
            if (!Application.isPlaying || GlobalResourceManager.Instance == null || control.resource == null) return;
            RemoveDestroyed(control);
            if (control.deployed.Count > 0)
            {
                foreach (GameObject unit in control.deployed)
                {
                    MilitaryExperience experience = unit != null ? unit.GetComponent<MilitaryExperience>() : null;
                    experience?.CaptureHealth();
                    experience?.ReturnToBase();
                }
                GlobalResourceManager.Instance.AddResource(control.resource, control.deployed.Count);
            }
            control.deployed.Clear();
        }
    }
}
