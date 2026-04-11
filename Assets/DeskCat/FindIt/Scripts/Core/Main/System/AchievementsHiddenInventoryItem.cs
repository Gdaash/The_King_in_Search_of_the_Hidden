using InventoryEngine;
using InventoryEngine.Items;
using UnityEngine;
using Zenject;

namespace DeskCat.FindIt.Scripts.Core.Main.System
{
    /// <summary>
    /// Добавляет предмет в инвентарь достижений
    /// </summary>
    [RequireComponent(typeof(HiddenObj))]
    public class AchievementsHiddenInventoryItem : MonoBehaviour
    {
        [SerializeField] private InventoryItem _item;
        [SerializeField] private int _quantity;
        
        private IRecipientInventoryItem<InventoryItem> _achievementInventoryItems;
        private HiddenObj _hiddenObj;
        
        [Inject]
        private void Construct([Inject(Id = "AchievementsInventory")] IRecipientInventoryItem<InventoryItem> achievementInventoryItems)
        {
            _achievementInventoryItems = achievementInventoryItems;
            TryGetComponent(out _hiddenObj);
        }

        private void OnEnable()
        {
            _hiddenObj.TargetClickAction += OnClick;
        }

        private void OnDisable()
        {
            _hiddenObj.TargetClickAction -= OnClick;
        }

        private void OnClick()
        {
            _achievementInventoryItems.AddItem(_item, _quantity);
        }
    }
}