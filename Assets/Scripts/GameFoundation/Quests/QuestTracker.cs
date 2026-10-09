using UnityEngine;
using GameFoundation.UI;
using UnityEngine.SceneManagement;

namespace GameFoundation.Quests
{
    [RequireComponent(typeof(GlobalResourceManager))]
    public sealed class QuestTracker : MonoBehaviour
    {
        [SerializeField] private QuestCatalog catalog;
        private GlobalResourceManager resources;
        private GameFoundation.MetaProgression.DayCycleService observedDay;
        private bool evaluating;
        private void Awake() => resources = GetComponent<GlobalResourceManager>();
        private void OnEnable() { GlobalResourceManager.OnResourceChanged += OnResourceChanged; QuestProgress.Changed += Evaluate; GameFoundation.Base.BuildingUpgradeService.Changed += Evaluate; SceneManager.sceneLoaded += OnSceneLoaded; BindDay(); }
        private void OnDisable() { GlobalResourceManager.OnResourceChanged -= OnResourceChanged; QuestProgress.Changed -= Evaluate; GameFoundation.Base.BuildingUpgradeService.Changed -= Evaluate; SceneManager.sceneLoaded -= OnSceneLoaded; if (observedDay != null) observedDay.Changed -= Evaluate; observedDay = null; }
        private void OnSceneLoaded(Scene _, LoadSceneMode __) { BindDay(); Evaluate(); }
        private void BindDay()
        {
            var day = GameFoundation.MetaProgression.DayCycleService.Instance;
            if (observedDay == day) return;
            if (observedDay != null) observedDay.Changed -= Evaluate;
            observedDay = day;
            if (observedDay != null) observedDay.Changed += Evaluate;
        }
        private void OnDestroy() { if (observedDay != null) observedDay.Changed -= Evaluate; }
        private void ActivateDialogues()
        {
            foreach (var view in Object.FindObjectsByType<QuestDialogueView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (view.gameObject.scene.IsValid() && view.gameObject.scene.isLoaded) view.ActivateIfNeeded();
        }
        private void Start() { BindDay(); Evaluate(); }
        private void OnResourceChanged(ResourceType _, int __) => Evaluate();
        private void Evaluate()
        {
            if (evaluating || resources == null || GlobalResourceManager.Instance != resources) return;
            evaluating = true;
            try
            {
                if (catalog != null) foreach (var entry in catalog.quests) QuestProgress.TryFail(entry, resources);
                var quest = QuestProgress.Current(catalog);
                if (QuestProgress.TryComplete(quest, resources)) GameNotifications.Post("Задание выполнено: " + quest.title, NotificationKind.Positive);
                ActivateDialogues();
            }
            finally { evaluating = false; }
        }
    }
}
