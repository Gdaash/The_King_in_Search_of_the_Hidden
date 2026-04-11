using System;
using UnityEngine;
using InventoryEngine.Items;

namespace Achievements
{
    /// <summary>
    /// Условие достижения
    /// Работает в связке с инвенторем
    /// Сравнивается количество элементов в инвенторе и в условии
    /// </summary>
    [Serializable]
    public class AchievementCondition
    {
        /// <summary>
        /// Элемент инвенторя
        /// </summary>
        [field: SerializeField, Tooltip("Элемент инвенторя")] 
        public InventoryItem Item { get; set; }
        
        /// <summary>
        /// Количество по достижению которого фиксируется достижение
        /// </summary>
        [field: SerializeField, Tooltip("Количество по достижению которого фиксируется достижение")] 
        public int Quantity { get; set; }
    }
}