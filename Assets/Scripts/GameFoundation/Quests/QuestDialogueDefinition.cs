using UnityEngine;

namespace GameFoundation.Quests
{
    [AddComponentMenu("Game/Quests/Dialogue definition")]
    public sealed class QuestDialogueDefinition : MonoBehaviour
    {
        [Tooltip("Постоянный идентификатор разговора для сохранений")]
        public string id;
        public string speakerName = "Старик Пью";
        public Sprite portrait;
        [TextArea(3, 8)] public string[] lines;
        [Tooltip("Задание выдаётся только по кнопке в последней реплике. Можно оставить пустым.")]
        public QuestDefinition quest;
        [Tooltip("Показывать разговор после получения награды за это задание. Пусто — доступен сразу.")]
        public QuestDefinition prerequisite;
        [Tooltip("Начать после принятия prerequisite, не дожидаясь его награды")]
        public bool prerequisiteAccepted;
    }
}
