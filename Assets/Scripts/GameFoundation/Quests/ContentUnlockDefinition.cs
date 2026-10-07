using UnityEngine;
namespace GameFoundation.Quests
{
    [CreateAssetMenu(menuName = "Game/Quests/Content unlock")]
    public sealed class ContentUnlockDefinition : ScriptableObject
    {
        [Tooltip("Постоянный ключ сохранения. После выпуска не менять.")]
        public string id;
        public string title;
        public Sprite icon;
        private void OnValidate() { if (string.IsNullOrWhiteSpace(id)) id = System.Guid.NewGuid().ToString("N"); }
    }
}
