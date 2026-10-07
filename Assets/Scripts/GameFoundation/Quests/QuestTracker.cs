using UnityEngine;
using GameFoundation.UI;

namespace GameFoundation.Quests
{
    [RequireComponent(typeof(GlobalResourceManager))]
    public sealed class QuestTracker : MonoBehaviour
    {
        [SerializeField] private QuestCatalog catalog;
        private GlobalResourceManager resources;
        private void Awake() => resources = GetComponent<GlobalResourceManager>();
        private void OnEnable() => GlobalResourceManager.OnResourceChanged += OnResourceChanged;
        private void OnDisable() => GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
        private void Start() => Evaluate();
        private void OnResourceChanged(ResourceType _, int __) => Evaluate();
        private void Evaluate()
        {
            if (resources == null || GlobalResourceManager.Instance != resources) return;
            var quest = QuestProgress.Current(catalog);
            if (QuestProgress.TryComplete(quest, resources))
                GameNotifications.Post("Задание выполнено: " + quest.title, NotificationKind.Positive);
        }
    }
}
