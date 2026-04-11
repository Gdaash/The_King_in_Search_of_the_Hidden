using System;

namespace InventoryEngine
{
    /// <summary>
    /// События инвенторя
    /// </summary>
    public interface IInventoryEvent
    {
        /// <summary>
        /// Возвращает событие старта инвенторя
        /// </summary>
        /// <returns></returns>
        event Action OnStart;

        /// <summary>
        /// Возвращает событие обновления инвенторя
        /// </summary>
        /// <returns></returns>
        event Action OnChanged;

        /// <summary>
        /// Возвращает событие завершения инвенторя
        /// </summary>
        /// <returns></returns>
        event Action OnLoaded;
    }
}
