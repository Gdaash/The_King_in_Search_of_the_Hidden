namespace InventoryEngine.GUI
{
    /// <summary>
    /// Обновление значение GUI элемента инвенторя
    /// </summary>
    public interface IListResourcesGUI
    {
        public void Init(IInventoryItems items, IInventoryEvent events, IRequiredItem requiredItem);
    }
}
