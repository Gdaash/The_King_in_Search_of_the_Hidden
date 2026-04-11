using System;
using System.Collections.Generic;
using InventoryEngine;
using UnityEngine;

namespace Achievements
{
    /// <summary>
    /// Группа состояний, если все ее элементы удовлетворяют условию
    /// достижение считается достигнутым
    /// </summary>
    [Serializable]
    public class Achievement
    {
        /// <summary>
        /// Id достижения
        /// должен совпадать с серверным названием
        /// </summary>
        [field: SerializeField, Tooltip("Id достижения. Должен совпадать с серверным названием")]
        public string Id { get; set; }
        
        [field: SerializeField] public List<AchievementCondition> AchievementConditions { get; private set; }
        
        /// <summary>
        /// Возвращает true если условие достижения выполнено 
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <returns></returns>
        public bool IsChecked(IInventoryItems  inventoryItem)
        {   
            foreach (var item in AchievementConditions)
            {
                if(inventoryItem.GetQuantity(item.Item) < item.Quantity) return false;
            }
            
            return true;
        }
    }
}