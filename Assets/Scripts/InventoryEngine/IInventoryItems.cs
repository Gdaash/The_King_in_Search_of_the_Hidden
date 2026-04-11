using System.Collections.Generic;
using InventoryEngine.Items;

namespace InventoryEngine
{
    /// <summary>
    /// Интерфейс работы с элементами инвенторя
    /// Получать элемент по индексу, количество элементов в инвенторе и в стеке
    /// </summary>
    public interface IInventoryItems
    {
        /// <summary>
        /// Возвращает элемент по индексу
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        InventoryItem this[int index] { get; }

        /// <summary>
        /// Количество элементов
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Возвращает общее количество элемента
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int GetQuantity(InventoryItem inventoryItem);

        /// <summary>
        /// Возвращает количество элемента по индексу
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int GetQuantity(int index);

        /// <summary>
        /// Определяет индекс элемента
        /// -1 если не содержит элемент
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <returns></returns>
        List<int> IndexOf(InventoryItem inventoryItem);

        /// <summary>
        /// Убирает элементы
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <param name="quantity"></param>
        bool RemoveItem(InventoryItem inventoryItem, int quantity);
    }
}
