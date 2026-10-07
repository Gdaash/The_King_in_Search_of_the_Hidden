using UnityEngine;
namespace GameFoundation.Base
{
    [CreateAssetMenu(menuName = "Game Foundation/Royal Decree")]
    public sealed class RoyalDecreeDefinition : ScriptableObject
    {
        public string id;
        public GameFoundation.Quests.ContentUnlockDefinition requiredUnlock;
        public bool IsAvailable => GameFoundation.Quests.ContentUnlocks.IsUnlocked(requiredUnlock);
        public string title;
        [TextArea] public string description;
        public ResourceType influence;
        [Min(0)] public int activationCost = 5;
    }
}
