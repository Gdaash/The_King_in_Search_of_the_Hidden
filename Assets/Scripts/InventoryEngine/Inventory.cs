using System;
using System.Collections.Generic;
using InventoryEngine.Items;
using UnityEngine;

namespace InventoryEngine
{
    /// <summary>
    /// Будет обрабатывать хранение предметов, сохранять и загружать их содержимое, добавлять в них предметы, удалять предметы, экипировать их и т. д.
    /// </summary>
    [Serializable]
    public class Inventory :
        MonoBehaviour , 
        IRecipientInventoryItem<InventoryItem>,
        IInventoryEvent,
        IInventoryItems
    {
        /// <summary>
        /// Уникальное имя инвенторя, используется для сохранения
        /// </summary>
        [Tooltip("Уникальное имя инвенторя, используется для сохранения")]
        [field: SerializeField] public string InventoryName { get; private set; }

        /// <summary>
        /// Массив поддерживаемых типов инвенторя
        /// Если тип инвенторя в элементе соответствует типу в инвенторе, его можно добавить в него
        /// </summary>
        [Tooltip("Массив поддерживаемых типов инвенторя. Если тип инвенторя в элементе соответствует типу в инвенторе, его можно добавить в него")]
        public InventoryType[] InventoryTypes;

        /// <summary>
        /// Полный список предметов инвентаря в этом инвентаре
        /// </summary>
        [Tooltip("Это просмотр содержимого вашего инвентаря в реальном времени. Не изменяйте этот список через инспектор, он виден только в целях контроля.")]
        public InventoryItem[] Content;

        /// <summary>
        /// Триггер сохранения инвенторя, если активен инвентарь сохраняется и загружается
        /// </summary>
        [Tooltip("Триггер сохранения инвенторя, если активен инвентарь сохраняется и загружается")]
        public bool IsSave = false;

        /// <summary>
        /// Количество свободных слотов в этом инвентаре
        /// </summary>
        [Tooltip("Количество свободных слотов в этом инвентаре")]
        public int NumberOfFreeSlots { get { return Content.Length - NumberOfFilledSlots; } }

        public event Action OnStart;
        public event Action OnChanged;
        public event Action OnLoaded;

        /// <summary>
        /// Количество заполненных слотов
        /// </summary>
        public int NumberOfFilledSlots
        {
            get
            {
                int numberOfFilledSlots = 0;
                for (int i = 0; i < Content.Length; i++)
                {
                    if (!InventoryItem.IsNull(Content[i]))
                    {
                        numberOfFilledSlots++;
                    }
                }
                return numberOfFilledSlots;
            }
        }

        public int Count => Content is null ? 0 : Content.Length;

        public InventoryItem this[int index] => Content[index];

        public const string _resourceItemPath = "Inventory/InventoryItems/";

        protected virtual void Start()
        {
            //TODO копируем стартовые элементы
            if (Content != null && Content.Length > 0)
            {
                for (int q = 0; q < Content.Length; q++)
                {
                    if(Content[q] != null)
                        Content[q] = Content[q].Copy();
                }
            }
            OnStart?.Invoke();
        }

        /// <summary>
        /// Количество штабелируемых слотов
        /// </summary>
        /// <param name="searchedItemID"></param>
        /// <param name="maxStackSize"></param>
        /// <returns></returns>
        public int NumberOfStackableSlots(string searchedItemID, int maxStackSize)
        {
            int numberOfStackableSlots = 0;
            int i = 0;

            while (i < Content.Length)
            {
                if (InventoryItem.IsNull(Content[i]))
                {
                    numberOfStackableSlots += maxStackSize;
                }
                else
                {
                    if (Content[i].ItemID == searchedItemID)
                    {
                        numberOfStackableSlots += maxStackSize - Content[i].Quantity;
                    }
                }
                i++;
            }

            return numberOfStackableSlots;
        }

        [Header("Debug")]
        /// Если true, содержимое инвентаря будет отображено в инспекторе.
        [Tooltip("Если true, содержимое инвентаря будет отображено в инспекторе.")]
        public bool DrawContentInInspector = false;

        /// <summary>
        /// Пытается добавить элемент указанного типа.
        /// </summary>
        /// <returns><c>true</c>, if item was added, <c>false</c> if it couldn't be added (item null, inventory full).</returns>
        /// <param name="itemToAdd">Item to add.</param>
        public virtual bool AddItem(InventoryItem itemToAdd, int quantity)
        {
            if (!CanAddResource(itemToAdd, quantity)) return false;
            
            //int maximum_stack = ItemManager.Instance.GetMaximumStackItemByItemID(itemToAdd.ItemID);
            int maximum_stack = 0;
            if (maximum_stack == 0) maximum_stack = itemToAdd.MaximumStack;

            if (maximum_stack < 0) 
            {
                Debug.LogError($"Maximum_stack не может быть меньше 0. itemToAdd.ItemID: {itemToAdd.ItemID}");
                return false;
            }

            List<int> list = InventoryContains(itemToAdd.ItemID);
            // if there's at least one item like this already in the inventory and it's stackable
            if (list.Count > 0 && maximum_stack > 1)
            {
                // we store items that match the one we want to add
                for (int i = 0; i < list.Count; i++)
                {
                    // if there's still room in one of these items of this kind in the inventory, we add to it
                    if (Content[list[i]].Quantity < maximum_stack)
                    {
                        // we increase the quantity of our item
                        Content[list[i]].Quantity += quantity;
                        // if this exceeds the maximum stack
                        if (Content[list[i]].Quantity > maximum_stack)
                        {
                            InventoryItem restToAdd = itemToAdd;
                            int restToAddQuantity = Content[list[i]].Quantity - maximum_stack;
                            // we clamp the quantity and add the rest as a new item
                            Content[list[i]].Quantity = maximum_stack; 
                            //AddItem(restToAdd, restToAddQuantity); TODO для этого проекта отключаем чтобы не было переполнения
                        }
                        OnChanged?.Invoke();
                        return true;
                    }
                }
            }
            // if we've reached the max size of our inventory, we don't add the item
            if (NumberOfFilledSlots >= Content.Length)
            {
                return false;
            }
            while (quantity > 0)
            {
                if (quantity > maximum_stack)
                {
                    AddItem(itemToAdd, maximum_stack);
                    quantity -= maximum_stack;
                }
                else
                {
                    if (list.Count == 0)
                    {
                        AddItemToArray(itemToAdd, quantity);
                    }
                    quantity = 0;
                }
            }
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Проверяет можно ли добавить ресурсы
        /// Если ресурса нет в инвенторе, значит мы его не можем вести
        /// Например у нас не тот багажник
        /// </summary>
        /// <returns></returns>
        public virtual bool CanAddResource(InventoryItem itemToAdd, int quantity)
        {
            // if the item to add is null, we do nothing and exit
            if (itemToAdd == null)
            {
                Debug.LogWarning(this.name + " : The item you want to add to the inventory is null");
                return false;
            }

            if (!SupportsType(itemToAdd)) return false;

            //TODO надо спросить у Саши как будут добавляться ресурсы
            //if (InventoryContains(itemToAdd.ItemID).Count == 0) return false;

            return true;
        }

        /// <summary>
        /// Удаляет указанный предмет из инвентаря по номеру.
        /// </summary>
        /// <returns><c>true</c>, if item was removed, <c>false</c> otherwise.</returns>
        /// <param name="itemToRemove">Item to remove.</param>
        public virtual bool RemoveItem(int i, int quantity)
        {
            if (i < 0 || i >= Content.Length)
            {
                Debug.LogWarning("InventoryEngine : you're trying to remove an item from an invalid index.");
                return false;
            }
            if (InventoryItem.IsNull(Content[i]))
            {
                Debug.LogWarning("InventoryEngine : you're trying to remove from an empty slot.");
                return false;
            }

            quantity = Mathf.Max(0, quantity);

            Content[i].Quantity -= quantity;

            if (Content[i].Quantity <= 0)
            {
                bool suppressionSuccessful = RemoveItemFromArray(i);
                OnChanged?.Invoke();
                return suppressionSuccessful;
            }
            else
            {
                OnChanged?.Invoke();
                return true;
            }
        }

        /// <summary>
        /// Удаляет указанный предмет из инвентаря по ItemId.
        /// </summary>
        public virtual bool RemoveItem(string id, int quantity)
        {
            int index = Array.FindIndex(Content, x => x?.ItemID == id);

            if (index == -1)
            {
                Debug.LogWarning("InventoryEngine : you're trying to remove from an empty slot.");
                return false;
            }

            quantity = Mathf.Max(0, quantity);
            Content[index].Quantity -= quantity;

            if (Content[index].Quantity <= 0)
            {
                bool suppressionSuccessful = RemoveItemFromArray(index);
                OnChanged?.Invoke();
                return suppressionSuccessful;
            }
            else
            {
                OnChanged?.Invoke();
                return true;
            }
        }
        /// <summary>
        /// Удаляет указанный предмет из инвентаря.
        /// Работает в случае если совпадают объекты
        /// </summary>
        /// <returns>Возырвщвет True если слот еще заполнен, иначе False</returns>
        public virtual bool RemoveItem(InventoryItem inventoryItem, int quantity)
        {
            if (!SupportsType(inventoryItem)) return false;

            int index = Array.FindIndex(Content, x => x?.ItemID == inventoryItem.ItemID);

            if (index == -1)
            {
                Debug.LogWarning("InventoryEngine : you're trying to remove from an empty slot.");
                return false;
            }

            quantity = Mathf.Max(0, quantity);
            Content[index].Quantity -= quantity;

            if (Content[index].Quantity <= 0)
            {
                RemoveItemFromArray(index);
                OnChanged?.Invoke();
                return false;
            }
            else
            {
                OnChanged?.Invoke();
                return true;
            }
        }

        /// <summary>
        /// Уничтожает элемент, хранящийся по индексу i.
        /// </summary>
        /// <returns><c>true</c>, if item was destroyed, <c>false</c> otherwise.</returns>
        /// <param name="i">The index.</param>
        public virtual bool DestroyItem(int i)
        {
            Content[i] = null;

            OnChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Очищает текущее состояние инвентаря.
        /// </summary>
        [ContextMenu("EmptyInventory")]
        public virtual void EmptyInventory()
        {
            Content = new InventoryItem[Content.Length];
            OnChanged?.Invoke();
        }

        /// <summary>
        /// Добавляет элемент в массив содержимого.
        /// </summary>
        /// <returns><c>true</c>, if item to array was added, <c>false</c> otherwise.</returns>
        /// <param name="itemToAdd">Item to add.</param>
        /// <param name="quantity">Quantity.</param>
        protected virtual bool AddItemToArray(InventoryItem itemToAdd, int quantity)
        {
            if (!SupportsType(itemToAdd)) return false;

            if (NumberOfFreeSlots == 0)
            {
                return false;
            }
            int i = 0;
            while (i < Content.Length)
            {
                if (InventoryItem.IsNull(Content[i]))
                {
                    Content[i] = itemToAdd.Copy();
                    Content[i].Quantity = quantity;

                    OnChanged?.Invoke();

                    return true;
                }
                i++;
            }
            return false;
        }

        /// <summary>
        /// Удаляет элемент с индексом i из массива.
        /// </summary>
        /// <returns><c>true</c>, if item from array was removed, <c>false</c> otherwise.</returns>
        /// <param name="i">The index.</param>
        protected virtual bool RemoveItemFromArray(int i)
        {
            if (i < Content.Length)
            {
                //Content[i].ItemID = null;
                Content[i] = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Изменяет размер массива до указанного нового размера
        /// </summary>
        /// <param name="newSize">New size.</param>
        public virtual void ResizeArray(int newSize)
        {
            InventoryItem[] temp = new InventoryItem[newSize];
            for (int i = 0; i < Mathf.Min(newSize, Content.Length); i++)
            {
                temp[i] = Content[i];
            }
            Content = temp;
        }

        /// <summary>
        /// Возвращает общее количество товаров, соответствующих указанному имени.
        /// </summary>
        /// <returns>The quantity.</returns>
        /// <param name="searchedItem">Searched item.</param>
        public virtual int GetQuantity(string searchedItemID)
        {
            List<int> list = InventoryContains(searchedItemID);
            int total = 0;
            foreach (int i in list)
            {
                total += Content[i].Quantity;
            }
            return total;
        }

        /// <summary>
        /// Возвращает список всех предметов в инвентаре, соответствующих указанному имени.
        /// </summary>
        /// <returns>A list of item matching the search criteria.</returns>
        /// <param name="searchedType">The searched type.</param>
        public virtual List<int> InventoryContains(string searchedItemID)
        {
            List<int> list = new List<int>();

            for (int i = 0; i < Content.Length; i++)
            {
                if (!InventoryItem.IsNull(Content[i]))
                {
                    if (Content[i].ItemID == searchedItemID)
                    {
                        list.Add(i);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Загружает инвентарь из сохраненного файла
        /// </summary>
        public virtual void LoadSavedInventory(SerializedInventory serialized_inventory)
        {
            ExtractSerializedInventory(serialized_inventory);
            OnLoaded?.Invoke();
        }

        /// <summary>
        /// Extracts the serialized inventory from a file content
        /// </summary>
        /// <param name="serializedInventory">Serialized inventory.</param>
        protected void ExtractSerializedInventory(SerializedInventory serializedInventory)
        {
            if (serializedInventory == null)
            {
                return;
            }

            DrawContentInInspector = serializedInventory.DrawContentInInspector;
            Content = new InventoryItem[serializedInventory.ContentType.Length];

            ArrayItemData arrayItemData = JsonUtility.FromJson<ArrayItemData>(serializedInventory.ContentData);
            
            for (int i = 0; i < serializedInventory.ContentType.Length; i++)
            {
                if ((serializedInventory.ContentType[i] != null) && (serializedInventory.ContentType[i] != ""))
                {
                    InventoryItem loadedInventoryItem = Resources.Load<InventoryItem>(_resourceItemPath + serializedInventory.ContentType[i]);
                    if (!loadedInventoryItem)
                    {
                        Debug.LogError("InventoryEngine : Couldn't find any inventory item to load at " + _resourceItemPath
                            + " named " + serializedInventory.ContentType[i] + ". Make sure all your items definitions names (the name of the InventoryItem scriptable " +
                            "objects) are exactly the same as their ItemID string in their inspector. " +
                            "Once that's done, also make sure you reset all saved inventories as the mismatched names and IDs may have " +
                            "corrupted them.");
                    }
                    else
                    {
                        Content[i] = loadedInventoryItem.Copy();
                        Content[i].Quantity = serializedInventory.ContentQuantity[i];

                        //Content[i].InventoryTypes = arrayItemData.ItemDatas[i].InventoryTypes;

                        // добавлена проверка модели на null
                        if (serializedInventory.ModeItemDataSaves[i] != null)
                        {
                            if (!string.IsNullOrEmpty(serializedInventory.ModeItemDataSaves[i].Data) &&
                                !string.IsNullOrEmpty(serializedInventory.ModeItemDataSaves[i].Type))
                            {
                                object data_save = JsonUtility.FromJson(serializedInventory.ModeItemDataSaves[i].Data, Type.GetType(serializedInventory.ModeItemDataSaves[i].Type));

                                if (data_save != null)
                                {
                                    Content[i].DataSave = data_save;
                                }
                            }
                        }
                        
                    }
                }
                else
                {
                    Content[i] = null;
                }
            }
        }

        /// <summary>
        /// Возвращет true если инвентарь поддерживает тип элемента, иначе false
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <returns></returns>
        public bool SupportsType(InventoryItem inventoryItem)
        {
            foreach (InventoryType types in InventoryTypes)
            {
                foreach (InventoryType types_item in inventoryItem.InventoryTypes)
                {
                    if (types.NameType.Equals(types_item.NameType)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Возвращает количество указанного элемента
        /// </summary>
        /// <param name="inventoryItem"></param>
        /// <returns></returns>
        public int GetQuantity(InventoryItem inventoryItem)
        {
            int n = 0;

            for (int q = 0; q < Content.Length; q++)
            {
                if (Content[q] != null && Content[q].ItemID == inventoryItem.ItemID)
                {
                    n += Content[q].Quantity;
                }
            }

            return n;
        }

        public List<int> IndexOf(InventoryItem inventoryItem)
        {
            List<int> indexs = null;
            if (inventoryItem is null) return indexs;
            for (int q = 0; q < Content.Length; q++)
            {
                if (Content[q] is null) continue;
                if (Content[q].ItemID == inventoryItem.ItemID)
                {
                    indexs ??= new List<int>();
                    indexs.Add(q);
                }
            }
            return indexs;
        }

        /// <summary>
        /// Возвращает количество элемента по индексу
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetQuantity(int index)
        {
            if (Content[index] != null) return Content[index].Quantity;
            return 0;
        }

        protected void SetOnChanged() => OnChanged?.Invoke();
    }

    public enum TypeMoveItem
    {
        None,
        /// <summary>
        /// Перемещение элемента отменено
        /// </summary>
        Cancel,
        /// <summary>
        /// Элемент добавлен
        /// </summary>
        Add,
        /// <summary>
        /// Элемент обменен
        /// </summary>
        Swap,
        /// <summary>
        /// Элемент удален
        /// </summary>
        Delete
    }
}