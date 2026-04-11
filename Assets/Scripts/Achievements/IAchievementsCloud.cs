using System.Collections.Generic;

namespace Achievements
{
    /// <summary>
    /// Облако достижений
    /// </summary>
    public interface IAchievementsCloud
    {
        /// <summary>
        /// Устанавливает достижение по ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool UnlockAchievement(string id);

        /// <summary>
        /// Сбрасывает достижения
        /// </summary>
        /// <param name="idAchievements"></param>
        public void ClearAchievements(List<string> idAchievements);
    }
}