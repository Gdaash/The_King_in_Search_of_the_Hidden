using Tools;

namespace InventoryEngine
{
    /// <summary>
    /// Событие инвенторя
    /// </summary>
    public struct InventoryEvent
    {
        public Inventory Inventory;
        public TypeInventoryEvent TypeInventoryEvent;

        static InventoryEvent e;

        public static void Trigger(Inventory inventory, TypeInventoryEvent type_inventory_event)
        {
            e.Inventory = inventory;
            e.TypeInventoryEvent = type_inventory_event;

            MMEventManager.TriggerEvent(e);
        }
    }
    public enum TypeInventoryEvent
    {
        /// <summary>
        /// Инвентарь инициализирован
        /// </summary>
        InventoryInit,
        /// <summary>
        /// Изменение контента
        /// </summary>
        ContentChanged,
        /// <summary>
        /// Предмет использован
        /// </summary>
        ItemUsed,
        /// <summary>
        /// Инвентарь загружен
        /// </summary>
        InventoryLoaded,
        /// <summary>
        /// Передача инвентарей на старте игры
        /// </summary>
        InventoryStart
    }
}
