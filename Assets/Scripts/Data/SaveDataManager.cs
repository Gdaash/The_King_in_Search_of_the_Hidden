using DeskCat.FindIt.Scripts.Core.Main.System;
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
            string data = JsonHelper.SaveToJson(objectToSave, fileName);    //Сохраняем локально в файл
            bool isSave = _cloud.SaveFile(fileName, data);                  //Сохраняем в облако
            return isSave;
        }

        public T LoadFile<T>(string fileName)
        {
            throw new System.NotImplementedException();
        }
    }
}