using System;

namespace InventoryEngine
{
    [Serializable]
    /// <summary>
    /// Модель данных для сохранения и загрузки инвенторя
    /// </summary>
    public class SerializedInventory
    {
        public int NumberOfRows;
        public int NumberOfColumns;
        public string InventoryName;
        public bool DrawContentInInspector = false;

        public string[] ContentType;
        public int[] ContentQuantity;

        public string ContentData;

        public ModeItemDataSave[] ModeItemDataSaves;
    }

    [Serializable]
    public class ItemData
    {
        public InventoryType[] InventoryTypes;
        public ModeItemDataSave ModeItemDataSaveRarity;
    }

    [Serializable]
    public class ArrayItemData
    {
        public ItemData[] ItemDatas;
    }
}