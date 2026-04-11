using System.Collections.Generic;
using System.Linq;
using Tools.LoadSave;
using UnityEngine;
using InventoryEngine.Items;

namespace InventoryEngine
{
    /// <summary>
    /// Контролирует сохранение и загрузку инвенторя, а так же предоставляет доступ к уникальным инвенторям
    /// </summary>
    public class InventoryService : MonoBehaviour
    {
        /// <summary>
        /// Лист зарегистрированных инвенторей
        /// </summary>
        [SerializeField] private List<Inventory> _inventorys = new List<Inventory>();

        private void Awake()
        {
            LoadData();
        }

        private void OnApplicationQuit()
        {
            SaveData();
        }

        /// <summary>
        /// Регистрирует инвентарь если у него есть имя.
        /// Если имя инвенторя повторится в массиве, сгенерируется исключение
        /// </summary>
        /// <param name="inventory"></param>
        /// <returns></returns>
        public bool RegisterInventory(Inventory inventory)
        {
            if (inventory|| 
                string.IsNullOrEmpty(inventory.InventoryName) || 
                _inventorys.Contains(inventory)) return false;

            if (_inventorys.SingleOrDefault(x => x.InventoryName.Equals(inventory.InventoryName)))
            {
                UnityEngine.Debug.LogError("Повторение уникального имени инвенторя!");
                return false;
            }

            _inventorys.Add(inventory);
            return true;
        }
        
        [ContextMenu("SaveData")]
        private void SaveData()
        {
            List<SerializedInventory> saveList = new List<SerializedInventory>();
            foreach (Inventory inventory in _inventorys)
            {
                if (inventory.IsSave)
                {
                    saveList.Add(FillSerializedInventory(inventory));
                }
            }
            MMSaveLoadManager.Save(saveList.ToArray(), DetermineSaveName());
        }
        
        [ContextMenu("LoadData")]
        private void LoadData()
        {
            SerializedInventory[] saveArray = (SerializedInventory[])MMSaveLoadManager.Load(typeof(SerializedInventory[]), DetermineSaveName());
            if (saveArray != null)
            {
                foreach (SerializedInventory serializedInventory in saveArray)
                {
                    Inventory inventory = _inventorys.SingleOrDefault(x => x.InventoryName.Equals(serializedInventory.InventoryName));
                    if (inventory)
                    {
                        inventory.LoadSavedInventory(serializedInventory);
                    }
                    else { Debug.LogWarning($"{serializedInventory.InventoryName} : null. Не найден инвентарь при загрузке."); }
                }
            }
        }

        private string DetermineSaveName()
        {
            return "DataBase.inventory";
        }

        /// <summary>
        /// Заполняет номерной запас для хранения
        /// </summary>
        /// <param name="serializedInventory">Serialized inventory.</param>
        private SerializedInventory FillSerializedInventory(Inventory inventory)
        {
            SerializedInventory serializedInventory = new SerializedInventory();
            serializedInventory.InventoryName = inventory.InventoryName;
            serializedInventory.DrawContentInInspector = inventory.DrawContentInInspector;
            serializedInventory.ContentType = new string[inventory.Content.Length];
            serializedInventory.ContentQuantity = new int[inventory.Content.Length];
            serializedInventory.ModeItemDataSaves = new ModeItemDataSave[inventory.Content.Length];

            //ArrayItemData arrayItemData = new ArrayItemData();
            //arrayItemData.ItemDatas = new ItemData[inventory.Content.Length];

            for (int i = 0; i < inventory.Content.Length; i++)
            {
                if (!InventoryItem.IsNull(inventory.Content[i]))
                {
                    serializedInventory.ContentType[i] = inventory.Content[i].ItemID;
                    serializedInventory.ContentQuantity[i] = inventory.Content[i].Quantity;

                    //arrayItemData.ItemDatas[i] = new ItemData();
                    //arrayItemData.ItemDatas[i].InventoryTypes = inventory.Content[i].InventoryTypes;

                    object data_save = inventory.Content[i].DataSave;
                    if (data_save != null)
                    { 
                        serializedInventory.ModeItemDataSaves[i] = new ModeItemDataSave() { Data = JsonUtility.ToJson(data_save), Type = data_save.GetType().ToString()};
                    }

                }
                else
                {
                    serializedInventory.ContentType[i] = null;
                    serializedInventory.ContentQuantity[i] = 0;
                }
            }

            //serializedInventory.ContentData = JsonUtility.ToJson(arrayItemData);

            return serializedInventory;
        }
    }
}
