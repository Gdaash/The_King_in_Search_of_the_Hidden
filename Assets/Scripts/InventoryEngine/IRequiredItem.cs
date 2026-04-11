using InventoryEngine.Items;

namespace InventoryEngine
{
    /// <summary>
    /// Предоставляет информацию от требуемых элементах
    /// </summary>
    public interface IRequiredItem
    {
        public int GetRequiredQuantity(InventoryItem inventoryItem);
    }
}
