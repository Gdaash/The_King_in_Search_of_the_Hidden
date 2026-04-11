using System;
using InventoryEngine.Items;
using UnityEngine;

namespace Assets.Develop.Scripts.InventoryEngine.Scripts.Helpers
{
    /// <summary>
    /// Описывает количество выбранного элемента
    /// </summary>
    [Serializable]
    public class ItemCount
    {
        /// <summary>
        /// Целевой элемент
        /// </summary>
        [Tooltip("Целевой элемент")]
        public InventoryItem InventoryItem;
        /// <summary>
        /// Количество целевого элемента
        /// </summary>
        [Tooltip("Количество целевого элемента")]
        public int Count;

        public ItemCount Copy()
        {
            ItemCount itemCount = new ItemCount();
            itemCount.Count = Count;
            itemCount.InventoryItem = InventoryItem.Copy();
            return itemCount;
        }
    }
}
