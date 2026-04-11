using System.Collections.Generic;
using Achievements;
using UnityEngine;

namespace Steamworks
{
    /// <summary>
    /// Основной класс для работы с Steam API
    /// </summary>
    public class SteamManager : MonoBehaviour, IAchievementsCloud
    {
        private bool _isInitialized = false;
        
        void Awake()
        {
            // Инициализация Steam API
            if (SteamAPI.Init())
            {
                Debug.Log("Steam API успешно инициализировано!");
                _isInitialized = true;
            }
            else
            {
                Debug.LogWarning("Steam API не инициализировано. Запущен ли клиент Steam?");
            }
        }

        void Update()
        {
            if (_isInitialized)
            {
                //TODO при скалировании времени равны 0
                // Update вызываться не будет
                SteamAPI.RunCallbacks();
            }
        }

        void OnApplicationQuit()
        {
            if (_isInitialized)
            {
                SteamAPI.Shutdown();
            }
        }

        // Пример функции для разблокировки достижения
        public bool UnlockAchievement(string achievementId)
        {
            if (!_isInitialized) return false;

            bool setAchievement = SteamUserStats.SetAchievement(achievementId);
            bool storeStats     = SteamUserStats.StoreStats(); // Отправляет данные на сервер Steam
        
            Debug.Log($"Достижение {achievementId} разблокировано! SetAchievement {setAchievement} StoreStats {storeStats}");

            return setAchievement && storeStats;
        }

        public void ClearAchievements(List<string> idAchievements)
        {
            foreach (var id in idAchievements)
            {
                bool state = SteamUserStats.ClearAchievement(id);
                Debug.Log($"Сброшено: {id} {state}");
            }
        
            // Сохраняем изменения
            bool success = SteamUserStats.StoreStats();
        }

        [ContextMenu(("Is steam running"))]
        private void IsSteamRunning()
        {
            print($"Steam test {SteamAPI.IsSteamRunning()}");
        }
    }
}