using UnityEngine;

namespace InventoryEngine.GUI
{
    /// <summary>
    /// Договор обновления GUI ячейки
    /// </summary>
    public interface IResourcesGUI<T>
    {
        /// <summary>
        /// Обновляет количество ресурса
        /// </summary>
        /// <param name="count"></param>
        public void UpdateCount(T item, string min, string max, Color? color = null);
    }
}
