namespace Core.LevelManagement
{
    /// <summary>
    /// Модель сигнала загрузки уровня
    /// </summary>
    public class SignalChangeLevel
    {
        public StatusChangeLevel  StatusChangeLevel { get; set; }
    }

    public enum StatusChangeLevel
    {
       /// <summary>
       /// Начата загрузка уровня
       /// </summary>
       LoadingStarted,
    
       /// <summary>
       /// Уровень загружен и готов к активации
       /// </summary>
       LevelLoaded
    } 
}