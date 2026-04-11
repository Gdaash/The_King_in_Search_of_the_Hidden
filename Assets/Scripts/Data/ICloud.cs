namespace Data
{
    /// <summary>
    /// Договор работы с облаком
    /// </summary>
    public interface ICloud
    {
        public bool SaveFile(string fileName, string saveData);
        public string LoadFile(string fileName);
    }
}