namespace Data
{
    /// <summary>
    /// Договор сохранения/загрузки данных
    /// </summary>
    public interface ISaveData
    {
        public bool SaveFile<T>(string fileName, T objectToSave);
        public T LoadFile<T>(string fileName);
    }
}