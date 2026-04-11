namespace InventoryEngine
{
    /// <summary>
    /// Получатель InventoryItem
    /// </summary>
    public interface IRecipientInventoryItem<Item>
    {
        /// <summary>
        /// Получает InventoryItem в количетсве quantity
        /// </summary>
        /// <returns></returns>
        bool AddItem(Item itemToAdd, int quantity);
    }
}
