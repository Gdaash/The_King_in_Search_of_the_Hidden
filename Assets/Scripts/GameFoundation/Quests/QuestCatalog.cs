using UnityEngine;

namespace GameFoundation.Quests
{
    [CreateAssetMenu(menuName = "Game/Quests/Quest sequence")]
    public sealed class QuestCatalog : ScriptableObject
    {
        [Tooltip("Quests unlock in this order. Resources must be present together in storage.")]
        public QuestDefinition[] quests;
    }
}
