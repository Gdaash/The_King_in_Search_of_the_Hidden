using UnityEngine;
using System.IO;
using System.Text;
using Data;

namespace Steamworks
{
    /// <summary>
    /// Предоставляет работу с облаком Steam
    /// </summary>
    public class SteamCloud : MonoBehaviour, ICloud
    {
        /// <summary>
        /// Сохранить игру в облако Steam
        /// </summary>
        public bool SaveFile(string fileName, string saveData)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            byte[] data = Encoding.UTF8.GetBytes(saveData);
            
            if (!SteamRemoteStorage.FileWrite(path, data, data.Length))
            {
                Debug.LogError("Ошибка записи в Steam Cloud!");
                return false;
            }

            Debug.Log($"Данные сохранены в облако: {fileName}");
            return true;
        }

        /// <summary>
        /// Загрузить игру из облака Steam
        /// </summary>
        public string LoadFile(string fileName)
        {
            if (!SteamRemoteStorage.FileExists(fileName))
            {
                Debug.LogWarning("Файл сохранения не найден в облаке");
                return null;
            }

            int fileSize = SteamRemoteStorage.GetFileSize(fileName);
            byte[] data = new byte[fileSize];

            int bytesRead = SteamRemoteStorage.FileRead(fileName, data, fileSize);

            if (bytesRead > 0)
            {
                string saveData = Encoding.UTF8.GetString(data, 0, bytesRead);
                Debug.Log($"Данные загружены из облака: {bytesRead} байт");
                return saveData;
            }

            return null;
        }

        /// <summary>
        /// Проверить статус синхронизации
        /// </summary>
        [ContextMenu("CheckCloudSyncStatus")]
        public void CheckCloudSyncStatus()
        {
            if (SteamRemoteStorage.IsCloudEnabledForAccount() &&
                SteamRemoteStorage.IsCloudEnabledForApp())
            {
                Debug.Log("Steam Cloud активен ✅");
            }
            else
            {
                Debug.LogWarning("Steam Cloud отключен ⚠️");
            }
        }

        /// <summary>
        /// Получить количество файлов в облаке
        /// </summary>
        [ContextMenu("GetCloudFileCount")]
        public int GetCloudFileCount()
        {
            int count = SteamRemoteStorage.GetFileCount();
            Debug.LogWarning($"Количество файлов сохранения: {count}️");
            return count;
        }
    }
}