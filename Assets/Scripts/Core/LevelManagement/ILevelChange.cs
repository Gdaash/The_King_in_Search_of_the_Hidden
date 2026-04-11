namespace Core.LevelManagement
{
    /// <summary>
    /// Изменение уровня
    /// </summary>
    public interface ILevelChange
    {
        /// <summary>
        /// Загружает указанный уровень
        /// </summary>
        /// <param name="levelName"></param>
        public void LoadLevel(string levelName);
    }
}