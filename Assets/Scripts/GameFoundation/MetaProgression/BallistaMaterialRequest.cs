using UnityEngine;

namespace GameFoundation.MetaProgression
{
    internal sealed class BallistaMaterialRequest : ResourceRequester
    {
        internal const int WoodCost = 2;
        private WorldMilitaryDeploymentController controller;
        private readonly SpriteRenderer[] woodIcons = new SpriteRenderer[WoodCost];

        internal void Initialize(WorldMilitaryDeploymentController owner, ResourceType wood)
        {
            controller = owner;
            requirements.Add(new ResourceRequirement { resourceType = wood, requiredAmount = WoodCost });
            for (int i = 0; i < WoodCost; i++)
            {
                var icon = new GameObject("Required wood").AddComponent<SpriteRenderer>();
                icon.transform.SetParent(transform, false);
                icon.transform.localPosition = new Vector3((i - .5f) * .45f, .69f, 0);
                icon.sprite = wood.resourceIcon;
                icon.sortingLayerName = "OverLight";
                icon.sortingOrder = 119;
                woodIcons[i] = icon;
            }
        }

        public override bool HasLogisticFlag() => controller != null && controller.IsBallistaSiteLit(transform.position);

        public override void UpdateIndicator()
        {
            if (requirements.Count == 0) return;
            int remaining = WoodCost - requirements[0].currentAmount;
            bool visible = HasLogisticFlag();
            for (int i = 0; i < WoodCost; i++)
            {
                if (woodIcons[i] == null) continue;
                woodIcons[i].enabled = visible && i < remaining;
                woodIcons[i].transform.localPosition = new Vector3((i - (remaining - 1) * .5f) * .45f, .69f, 0);
            }
        }
    }
}
