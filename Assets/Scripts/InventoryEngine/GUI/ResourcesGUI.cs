using InventoryEngine.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryEngine.GUI
{
    /// <summary>
    /// Отображает ресурс и количество
    /// </summary>
    public class ResourcesGUI : 
        MonoBehaviour,
        IResourcesGUI<InventoryItem>
    {
        /// <summary>
        /// Картинка ресурсов
        /// </summary>
        [SerializeField, Tooltip("Картинка ресурсов")]
        private Image image;

        /// <summary>
        /// Текст количества ресурса
        /// </summary>
        [SerializeField, Tooltip("Текст количества ресурса")]
        private TextMeshProUGUI text_count;

        /// <summary>
        /// Разделитель ресурсов
        /// </summary>
        [SerializeField, Tooltip("Разделитель ресурсов")]
        private string separator = "";

        /// <summary>
        /// Суффикс, последний элемент строки
        /// </summary>
        [SerializeField, Tooltip("Суффикс, последний элемент строки")]
        private string suffix = "";

        /// <summary>
        /// Обновляет количество ресурса
        /// </summary>
        /// <param name="count"></param>
        public virtual void UpdateCount(InventoryItem inventoryItem, string min, string max, Color ?color = null)
        {
            image.sprite = inventoryItem is null ? null : inventoryItem.Icon;
            text_count.text = $"{min}{separator}{max}{suffix}";
            if(color != null) text_count.color = (Color)color;
        }
    }
}
