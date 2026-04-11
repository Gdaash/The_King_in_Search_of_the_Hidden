using System;
using UnityEngine;

namespace InventoryEngine
{
    /// <summary>
    /// Тип инвенторя
    /// Используется при сравнении типа инвенторя и элемента, если типы совпадают (имена) инвентарь может использовать элемент
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "InventoryType", menuName = "InventoryTypes/InventoryType", order = 1)]
    public class InventoryType : ScriptableObject
    {
        /// <summary>
        /// Название типа
        /// </summary>
        [Tooltip("Название типа")]
        public string NameType;
    }
}
