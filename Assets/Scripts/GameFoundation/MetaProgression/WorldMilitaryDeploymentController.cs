using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameFoundation.Audio;

namespace GameFoundation.MetaProgression
{
    public sealed class WorldMilitaryDeploymentController : MonoBehaviour
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
        [SerializeField] private UnitControl swordsmen = new UnitControl();
        [SerializeField] private UnitControl archers = new UnitControl();

        private void Awake()
        {
            if (portal == null)
            {
                var tower = GameObject.Find("PortalTower");
                if (tower != null) portal = tower.transform;
            }
            Bind(swordsmen);
            Bind(archers);
        }

        private void OnEnable()
        {
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
            Refresh();
        }

        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;

        private void OnDestroy()
        {
            ReturnSurvivors(swordsmen);
            ReturnSurvivors(archers);
        }

        private void Bind(UnitControl control)
        {
            if (control.summonButton != null) control.summonButton.onClick.AddListener(() => Summon(control));
            if (control.recallButton != null) control.recallButton.onClick.AddListener(() => Recall(control));
        }

        private void Summon(UnitControl control)
        {
            if (portal == null || control.resource == null || control.prefab == null ||
                GlobalResourceManager.Instance == null ||
                !GlobalResourceManager.Instance.TrySpendResource(control.resource, 1)) return;

            var unit = Instantiate(control.prefab, portal.position, Quaternion.identity);
            GameAudioController.PlayAt(GameAudioCue.Portal, portal.position, 0.72f, 0.96f, 1.04f, 0.08f);
            unit.name = control.prefab.name;
            control.deployed.Add(unit);
            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
            float maxRadius = Mathf.Max(departureMinRadius, departureMaxRadius);
            float radius = UnityEngine.Random.Range(Mathf.Min(departureMinRadius, maxRadius), maxRadius);
            WorldMilitaryArrivalMover mover = unit.AddComponent<WorldMilitaryArrivalMover>();
            mover.Begin((Vector2)portal.position + direction * radius);
            Refresh();
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

            WorldMilitaryReturner returner = unit.AddComponent<WorldMilitaryReturner>();
            GameAudioController.PlayAt(GameAudioCue.Portal, portal.position, 0.55f, 0.94f, 1.02f, 0.08f);
            returner.Begin(portal, returnedUnit => CompleteRecall(control, returnedUnit));
            Refresh();
        }

        public void BeginEscapeRecall()
        {
            BeginEscapeRecall(swordsmen);
            BeginEscapeRecall(archers);
            Refresh();
        }

        public bool HasReturningUnits
        {
            get
            {
                RemoveDestroyed(swordsmen);
                RemoveDestroyed(archers);
                return swordsmen.deployed.Count > 0 || archers.deployed.Count > 0;
            }
        }

        private void BeginEscapeRecall(UnitControl control)
        {
            RemoveDestroyed(control);
            foreach (GameObject unit in control.deployed)
            {
                if (unit == null || unit.GetComponent<WorldMilitaryReturner>() != null) continue;
                WorldMilitaryReturner returner = unit.AddComponent<WorldMilitaryReturner>();
                returner.Begin(portal, returnedUnit => CompleteRecall(control, returnedUnit));
            }
        }

        private void CompleteRecall(UnitControl control, GameObject unit)
        {
            if (!control.deployed.Remove(unit)) return;
            if (GlobalResourceManager.Instance != null && control.resource != null)
                GlobalResourceManager.Instance.AddResource(control.resource, 1);
            if (unit != null) Destroy(unit);
            Refresh();
        }

        private void OnResourceChanged(ResourceType _, int __) => Refresh();

        private void Refresh()
        {
            Refresh(swordsmen);
            Refresh(archers);
        }

        private void Refresh(UnitControl control)
        {
            RemoveDestroyed(control);
            int stored = GlobalResourceManager.Instance != null && control.resource != null
                ? GlobalResourceManager.Instance.GetResourceAmount(control.resource) : 0;
            if (control.storedAmount != null) control.storedAmount.text = stored.ToString();
            if (control.deployedAmount != null) control.deployedAmount.text = control.deployed.Count.ToString();
            if (control.summonButton != null) control.summonButton.interactable = portal != null && control.prefab != null && stored > 0;
            if (control.recallButton != null)
                control.recallButton.interactable = control.deployed.Exists(unit =>
                    unit != null && unit.GetComponent<WorldMilitaryReturner>() == null);
        }

        private static void RemoveDestroyed(UnitControl control) => control.deployed.RemoveAll(unit => unit == null);

        private static void ReturnSurvivors(UnitControl control)
        {
            if (!Application.isPlaying || GlobalResourceManager.Instance == null || control.resource == null) return;
            RemoveDestroyed(control);
            if (control.deployed.Count > 0)
                GlobalResourceManager.Instance.AddResource(control.resource, control.deployed.Count);
            control.deployed.Clear();
        }
    }
}
