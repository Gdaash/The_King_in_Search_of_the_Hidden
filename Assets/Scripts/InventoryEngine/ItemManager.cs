using System.Collections.Generic;
using Assets.Develop.Scripts.InventoryEngine.Scripts.Helpers;
using Singletons;
using UnityEngine;

namespace Assets.Scripts.InventoryEngine
{
    /// <summary>
    /// Корректирует информацию элементов
    /// </summary>
    public class ItemManager : MMSingleton<ItemManager>
    {
        /// <summary>
        /// Список элементов с максимальным значением стека. Корректирует переменную MaximumStack в элементах
        /// </summary>
        [field: SerializeField, Tooltip("Список элементов с максимальным значением стека. Корректирует переменную MaximumStack в элементах")]
        public List<ItemCount> ListMaximumStackItems { get; private set; }

        /// <summary>
        /// Словарь элементов максимальных значений стека
        /// </summary>
        [Tooltip("Словарь элементов максимальных значений стека")]
        public Dictionary<string, ItemCount> DictionaryMaximumStackItems { get; private set; }

        public override void InitializeSingleton()
        {
            if (is_init) return;
            base.InitializeSingleton();

            if (DictionaryMaximumStackItems == null)
            {
                DictionaryMaximumStackItems = new Dictionary<string, ItemCount>();
                foreach (ItemCount itemCount in ListMaximumStackItems)
                {
                    DictionaryMaximumStackItems.Add(itemCount.InventoryItem.ItemID, itemCount);
                }
            }
        }

        /// <summary>
        /// Возвращает максимальное значение стека по id элемента
        /// </summary>
        /// <returns></returns>
        public int GetMaximumStackItemByItemID(string id)
        {
            try { return DictionaryMaximumStackItems[id].Count; }
            catch { return 0; }
        }

        /// <summary>
        /// Добавляет максимальный размер стека по id элемента
        /// </summary>
        /// <param name="id"></param>
        /// <param name="add"></param>
        public void AddMaximumStackItemByItemID(string id, int add)
        {
            try { DictionaryMaximumStackItems[id].Count += add; }
            catch { }
        }
    }
}
