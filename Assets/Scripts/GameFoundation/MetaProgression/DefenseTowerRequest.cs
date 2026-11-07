using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    internal enum DefenseTowerKind { Arrow, Stone, Magic }

    internal sealed class DefenseTowerRequest : ResourceRequester
    {
        private WorldMilitaryDeploymentController controller;
        private GameObject prefab, tower;
        private ArcherTower weapon;
        private Health health;
        private ResourceType human;
        private DefenseTowerKind kind;
        private bool removed;
        private readonly List<SpriteRenderer> icons = new();

        internal bool Built => tower != null;
        internal Sprite Preview => prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>()?.sprite : null;

        internal static GameObject LoadPrefab(DefenseTowerKind kind) => Resources.Load<GameObject>("Engineering/" + kind + " Tower");

        internal void Initialize(WorldMilitaryDeploymentController owner, DefenseTowerKind type, GameObject towerPrefab,
            ResourceType material, ResourceType ore, ResourceType worker)
        {
            controller = owner;
            kind = type;
            prefab = towerPrefab;
            human = worker;
            requirements.Add(new ResourceRequirement { resourceType = material, requiredAmount = 2 });
            if (ore != null) requirements.Add(new ResourceRequirement { resourceType = ore, requiredAmount = 1 });
            for (int i = 0; i < (ore == null ? 2 : 3); i++)
            {
                var icon = new GameObject("Required resource").AddComponent<SpriteRenderer>();
                icon.transform.SetParent(transform, false);
                icon.sortingLayerName = "OverLight";
                icon.sortingOrder = 119;
                icons.Add(icon);
            }
        }

        public override bool HasLogisticFlag() => !removed && controller != null &&
            (health == null || !health.IsDead) && controller.IsBallistaSiteLit(transform.position);

        protected override void CheckCompletion()
        {
            if (Built || !HasLogisticFlag() || !CrystalResourcesReady) return;
            tower = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            weapon = tower.GetComponent<ArcherTower>();
            health = tower.GetComponent<Health>();
            health.OnDeath.AddListener(OnTowerDeath);
            weapon.enabled = false;
            if (kind == DefenseTowerKind.Stone) tower.GetComponent<TowerVisuals>().UseLobbedStones();
            requirements.Clear();
            requirements.Add(new ResourceRequirement { resourceType = human, requiredAmount = 1 });
            OrderManager.Instance?.ForceUpdateOrders();
        }

        protected override void Update()
        {
            base.Update();
            if (!Built)
            {
                CheckCompletion();
                return;
            }

            bool lit = HasLogisticFlag();
            if (!lit && ResidentHumanCount > 0 && Warehouse.Instance != null)
                SendHumansHome(human, Warehouse.Instance);
            weapon.enabled = lit && ResidentHumanCount > 0;
        }

        public override void UpdateIndicator()
        {
            int index = 0;
            if (HasLogisticFlag())
                foreach (var requirement in requirements)
                    for (int i = requirement.currentAmount; i < requirement.requiredAmount; i++)
                        icons[index++].sprite = requirement.resourceType.resourceIcon;
            for (int i = 0; i < icons.Count; i++)
            {
                icons[i].enabled = i < index;
                icons[i].transform.localPosition = new Vector3((i - (index - 1) * .5f) * .45f, .69f);
            }
        }

        private void OnTowerDeath()
        {
            controller.ForgetDefenseTower(transform.position);
            Remove();
        }

        internal void Remove()
        {
            if (removed) return;
            removed = true;
            OrderManager.Instance?.CancelDeliveries(this);
            if (Warehouse.Instance != null) SendHumansHome(human, Warehouse.Instance);
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
