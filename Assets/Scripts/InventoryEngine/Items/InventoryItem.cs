using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InventoryEngine.Items
{
    /// <summary>
    /// Базовый класс для предметов инвентаря, который планируется расширить.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "Item", menuName = "InventoryItem/Base", order = 1)]
    public class InventoryItem : ScriptableObject
    {
        [Header("Идентификатор и цель")]
        /// Идентификатор и цель
        [Tooltip("Идентификатор и цель")]
        public string ItemID;

        /// <summary>
        /// Массив поддерживаемых типов инвенторя
        /// Если тип инвенторя в элементе соответствует типу в инвенторе, его можно добавить в него
        /// </summary>
        [Tooltip("Массив поддерживаемых типов инвенторя. Если тип инвенторя в элементе соответствует типу в инвенторе, его можно добавить в него")]
        public InventoryType[] InventoryTypes;

        /// если это правда, предмет не будет добавлен туда, где есть место в инвентаре, а вместо этого будет добавлен в указанный слот TargetIndex.
        [Tooltip("если это правда, предмет не будет добавлен туда, где есть место в инвентаре, а вместо этого будет добавлен в указанный слот TargetIndex.")]
        public bool ForceSlotIndex = false;
        /// если ForceSlotIndex имеет значение true, это индекс, по которому элемент будет добавлен в целевой инвентарь.
        [Tooltip("если ForceSlotIndex имеет значение true, это индекс, по которому элемент будет добавлен в целевой инвентарь.")]
        public int TargetIndex = 0;

        [Header("Permissions")]
        /// можно ли «использовать» этот элемент (через метод Use) — важно, это только НАЧАЛЬНОЕ состояние этого объекта, IsUsable можно использовать в любое время после этого
        [Tooltip("можно ли «использовать» этот элемент (через метод Use) — важно, это только НАЧАЛЬНОЕ состояние этого объекта, IsUsable можно использовать в любое время после этого")]
        public bool Usable = false;
        /// если это правда, вызов Use для этого объекта будет потреблять одну его единицу
        [Tooltip("если это правда, вызов Use для этого объекта будет потреблять одну его единицу")]
        public bool Consumable = true;
        /// если этот предмет является расходным, определяет, сколько будет израсходовано за одно использование (обычно один)
        [Tooltip("если этот предмет является расходным, определяет, сколько будет израсходовано за одно использование (обычно один)")]
        public int ConsumeQuantity = 1;

        /// можно ли использовать этот объект
        public virtual bool IsUsable { get { return Usable; } }

        /// <summary>
        /// Возвращает True если является данным типоп
        /// </summary>
        /// <param name="inventoryType"></param>
        /// <returns></returns>
        public virtual bool IsTargetInventoryType(InventoryType inventoryType)
        {
            return InventoryTypes.Contains(inventoryType);
        }

        public virtual bool IsTargetInventoryType(string inventoryTypeId)
        {
            foreach (var type in InventoryTypes)
            {
                if (type.NameType == inventoryTypeId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Принимает и возвращает объект сохранения
        /// </summary>
        public virtual object DataSave
        {
            get => null;
            set 
            {
            }
        }

        /// базовое количество этого товара
        [Tooltip("базовое количество этого товара")]
        public int Quantity = 1;

        [Header("Basic info")]
        /// название товара - будет отображаться в панели подробностей
        [Tooltip("название товара - будет отображаться в панели подробностей")]
        public string ItemName;

        [Space(10)]
        [TextArea]
        /// краткое описание товара
        [Tooltip("краткое описание товара")]
        public string ShortDescription;
        [TextArea]
        /// подробное описание товара
        [Tooltip("подробное описание товара")]
        public string Description;

        [Header("Image")]
        [Tooltip("значок, который будет отображаться в слоте инвентаря")]
        /// значок, который будет отображаться в слоте инвентаря
        public Sprite Icon;

        [Header("Prefab Drop")]
        [Tooltip("префаб для создания экземпляра при удалении элемента")]
        /// префаб для создания экземпляра при удалении элемента
        public GameObject Prefab;

        [Header("Inventory Properties")]
        [Tooltip("максимальное количество предметов, которые вы можете сложить в один слот")]
        /// максимальное количество предметов, которые вы можете сложить в один слот
        public int MaximumStack = 1;

        [Tooltip("Звук использования предмета")]
        /// Звук использования предмета
        public AudioClip AudioClipUse;

        /// <summary>
        /// Определяет, является ли элемент нулевым или нет
        /// </summary>
        /// <returns><c>true</c> if is null the specified item; otherwise, <c>false</c>.</returns>
        /// <param name="item">Item.</param>
        public static bool IsNull(InventoryItem item)
        {
            if (item == null)
            {
                return true;
            }
            if (item.ItemID == null)
            {
                return true;
            }
            if (item.ItemID == "")
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Копирует элемент в новый
        /// </summary>
        public virtual InventoryItem Copy()
        {
            string name = this.name;
            InventoryItem clone = Instantiate(this) as InventoryItem;
            clone.name = name;
            return clone;
        }

        /// <summary>
        /// Что происходит при создании объекта — переопределите это, чтобы добавить собственное поведение.
        /// </summary>
        public virtual void SpawnPrefab(Vector2 position)
        {
            //GameObject droppedObject = (GameObject)Instantiate(Prefab);
            //if (droppedObject.GetComponent<ItemPicker>() != null)
            //{
            //    droppedObject.GetComponent<ItemPicker>().Quantity = Quantity;
            //    droppedObject.GetComponent<ItemPicker>().RemainingQuantity = Quantity;
            //}
            //
            //MMSpawnAround.ApplySpawnAroundProperties(droppedObject, DropProperties,
            //    TargetInventory(playerID).TargetTransform.position);
        }

        /// <summary>
        /// Что происходит при использовании объекта — переопределите это, чтобы добавить собственное поведение.
        /// </summary>
        public virtual bool Use(object target) { Debug.Log($"Item use {name}"); return true; }
    }
}