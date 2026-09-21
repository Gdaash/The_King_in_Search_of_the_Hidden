using System.Collections.Generic;
using System.Linq;
using Zenject;
using InventoryEngine;
using UnityEngine;

namespace Achievements
{
    /// <summary>
    /// Мэнеджер достижений
    /// </summary>
    public class AchievementsManager : MonoBehaviour
    {
        private IInventoryEvent _achievementInventoryEvent;
        private IInventoryItems _achievementInventoryItems;
        private IAchievementsCloud _achievementsCloud;
        private bool _subscribed;

        [SerializeField] private List<Achievement> _achievements;

        [Inject]
        private void Construct(
            [Inject(Id = "AchievementsInventory")] IInventoryEvent achievementInventoryEvent,
            [Inject(Id = "AchievementsInventory")] IInventoryItems achievementInventoryItems,
            [Inject] IAchievementsCloud achievementsCloud)
        {
            _achievementInventoryEvent = achievementInventoryEvent;
            _achievementInventoryItems = achievementInventoryItems;
            _achievementsCloud = achievementsCloud;
            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (_subscribed && _achievementInventoryEvent != null)
                _achievementInventoryEvent.OnChanged -= OnChanged;
            _subscribed = false;
        }

        private void Subscribe()
        {
            if (_subscribed || !isActiveAndEnabled || _achievementInventoryEvent == null) return;
            _achievementInventoryEvent.OnChanged += OnChanged;
            _subscribed = true;
        }

        private void OnChanged()
        {
            foreach (var item in _achievements)
            {
                if (item.IsChecked(_achievementInventoryItems))
                {
                    print(
                        $"Разблокировано достижение {item.Id} состояние {_achievementsCloud.UnlockAchievement(_achievementsCloud.UnlockAchievement(item.Id).ToString())}");
                }
            }
        }
        
        [ContextMenu("ClereAchievements")]
        private void ClereAchievements()
        {
            _achievementsCloud.ClearAchievements(_achievements.Select(x => x.Id).ToList());
        }
    }
}
