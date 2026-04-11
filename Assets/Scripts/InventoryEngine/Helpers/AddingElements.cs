using System.Collections.Generic;
using Assets.Develop.Scripts.InventoryEngine.Scripts.Helpers;
using UnityEngine;

namespace InventoryEngine.Scripts.Helpers
{
    /// <summary>
    /// Добавляет указанные элементы в указанный инвентарь
    /// </summary>
    [Tooltip("Добавляет указанные элементы в указанный инвентарь")]
    public class AddingElements : MonoBehaviour
    {
        /// <summary>
        /// Целевой инвеньтарь, в который добавляются элементы
        /// </summary>
        [Tooltip("Целевой инвеньтарь, в который добавляются элементы")]
        [field: SerializeField] public Inventory InventoryTarget { get; private set; }
        /// <summary>
        /// Список добвыляемых елементов
        /// </summary>
        [Tooltip("Список добвыляемых елементов")]
        [field : SerializeField] public List<ItemCount> Items { get; private set; }

        /// <summary>
        /// Добавляет элементы в инвентарь
        /// </summary>
        [ContextMenu("AddItems")]
        public void AddItems()
        {
            if (InventoryTarget == null) return;
            if (Items == null) return;

            Items.ForEach(item => { InventoryTarget.AddItem(item.InventoryItem, item.Count); });
        }
    }
}
