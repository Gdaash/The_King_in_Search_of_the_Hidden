using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFoundation.Quests
{
    [CreateAssetMenu(menuName = "Game/Quests/Resource quest")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class Requirement
        {
            public ResourceType resource;
            [Min(1)] public int amount = 1;
            public string purpose;
        }
        [Tooltip("Stable save identifier. Do not change after publishing the quest.")]
        public string id;
        public string title;
        [TextArea] public string description;
        public List<Requirement> requirements = new();
        [Serializable]
        public sealed class ResourceReward
        {
            public ResourceType resource;
            [Min(1)] public int amount = 1;
        }
        [Header("Награда — выдаётся только по кнопке")]
        public List<ResourceReward> resourceRewards = new();
        public List<ContentUnlockDefinition> unlockRewards = new();
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N");
        }

        public bool HasRequiredStock(Func<ResourceType, int> stock)
        {
            if (stock == null || requirements.Count == 0) return false;
            foreach (var goal in requirements)
                if (goal == null || goal.resource == null || goal.amount < 1 || stock(goal.resource) < goal.amount) return false;
            return true;
        }
    }
}
