using System.IO;
using UnityEngine;

namespace Data
{
    /// <summary>
    /// Мэнаджер сохранения и загрузки данных
    /// TODO
    /// </summary>
    public class SaveDataManager : MonoBehaviour, ISaveData
    {
        private ICloud _cloud;
        
        public bool SaveFile<T>(string fileName, T objectToSave)
        {
            string data = JsonUtility.ToJson(objectToSave, true);
            string directory = Path.Combine(Application.dataPath, "Data");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, fileName), data);
            bool isSave = _cloud.SaveFile(fileName, data);                  //Сохраняем в облако
            return isSave;
        }

        public T LoadFile<T>(string fileName)
        {
            throw new System.NotImplementedException();
        }
    }
}