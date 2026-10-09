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
            [Tooltip("Для цели строительства укажите id здания, например blacksmith. Resource оставьте пустым.")]
            public string buildingId;
            public Sprite buildingIcon;
            public int CurrentAmount(Func<ResourceType, int> stock) => !string.IsNullOrWhiteSpace(buildingId) ?
                GameFoundation.Saves.SaveSlotPrefs.GetInt("foundation.building." + buildingId + ".built", 0) : resource != null && stock != null ? stock(resource) : 0;
        }
        [Tooltip("Stable save identifier. Do not change after publishing the quest.")]
        public string id;
        public string title;
        [TextArea] public string description;
        public string inProgressStatus = "Доставьте ресурсы в портал";
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
        [Tooltip("Открыть наградной контент сразу после выполнения условий. Кнопка награды всё ещё завершает задание.")]
        public bool unlockRewardsOnCompletion;
        [Header("Срок, сдача и штраф")]
        public bool parallelQuest;
        [Min(0)] public int deadlineDays;
        public bool consumeRequirementsOnClaim;
        public ResourceType penaltyResource;
        [Min(0)] public int penaltyAmount;
        public bool confiscateResourcesOnFailure;
        public List<ResourceType> protectedResources = new();
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N");
        }

        public bool HasRequiredStock(Func<ResourceType, int> stock)
        {
            if (stock == null || requirements.Count == 0) return false;
            foreach (var goal in requirements)
                if (goal == null || goal.amount < 1 || goal.CurrentAmount(stock) < goal.amount) return false;
            return true;
        }
    }
}
