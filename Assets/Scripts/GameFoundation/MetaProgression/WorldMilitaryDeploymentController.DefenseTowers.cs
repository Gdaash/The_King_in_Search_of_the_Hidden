using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.MetaProgression
{
    public sealed partial class WorldMilitaryDeploymentController
    {
        private readonly Dictionary<Vector2, DefenseTowerRequest> defenseTowers = new();
        internal IEnumerable<KeyValuePair<Vector2, DefenseTowerRequest>> DefenseTowers => defenseTowers;

        internal bool OrderDefenseTower(Vector2 site, DefenseTowerKind kind)
        {
            if (!CanOrderDefense(site)) return false;
            var prefab = DefenseTowerRequest.LoadPrefab(kind);
            var material = ResourceCatalog.Find(kind == DefenseTowerKind.Arrow ? "Wood" : "Stone");
            var ore = kind == DefenseTowerKind.Magic ? ResourceCatalog.Find("MagicOre") : null;
            var human = ResourceCatalog.Find("Human");
            if (prefab == null || material == null || human == null || (kind == DefenseTowerKind.Magic && ore == null))
                return false;

            var order = new GameObject("Defense tower order");
            order.SetActive(false);
            order.transform.SetParent(transform);
            order.transform.position = site;
            var request = order.AddComponent<DefenseTowerRequest>();
            request.Initialize(this, kind, prefab, material, ore, human);
            defenseTowers.Add(site, request);
            order.SetActive(true);
            OrderManager.Instance?.ForceUpdateOrders();
            return true;
        }

        internal void ForgetDefenseTower(Vector2 site) => defenseTowers.Remove(site);
    }
}
