using UnityEngine;

namespace GameFoundation.Quests
{
    [AddComponentMenu("Game/Quests/Dialogue definition")]
    public sealed class QuestDialogueDefinition : MonoBehaviour
    {
        [Tooltip("Постоянный идентификатор разговора для сохранений")]
        public string id;
        [Tooltip("Явное исключение: разрешить этот диалог поверх открытого попапа.")]
        public bool allowOverPopups;
        public string speakerName = "Стайк Пью";
        public Sprite portrait;
        [TextArea(3, 8)] public string[] lines;
        [Tooltip("Задание выдаётся только по кнопке в последней реплике. Можно оставить пустым.")]
        public QuestDefinition quest;
        [Tooltip("Показывать разговор после получения награды за это задание. Пусто – доступен сразу.")]
        public QuestDefinition prerequisite;
        public string requiredEnemyKillsId;
        [Min(0)] public int requiredEnemyKills;
        public GameFoundation.MetaProgression.PortalTowerBalance.Weapon onlyBeforeEnhancing;
        public bool ChallengeAvailable => (string.IsNullOrEmpty(requiredEnemyKillsId) || GameFoundation.MetaProgression.PortalWeaponEnhancements.Kills(requiredEnemyKillsId) >= requiredEnemyKills) &&
            (onlyBeforeEnhancing == GameFoundation.MetaProgression.PortalTowerBalance.Weapon.None || GameFoundation.MetaProgression.PortalWeaponEnhancements.Rank(onlyBeforeEnhancing) == 0);
        [Tooltip("Начать после принятия prerequisite, не дожидаясь его награды")]
        public bool prerequisiteAccepted;
        [Tooltip("Дождаться следующего дня после получения награды за предыдущее задание.")]
        public bool prerequisiteNextDay;
        public bool PrerequisiteNightPassed => !prerequisiteNextDay || QuestProgress.NightAfterClaimPassed(prerequisite);
        [Tooltip("Дождаться следующего дня после строительства этого здания.")]
        public string requiredBuildingAfterNight;
        public bool BuildingNightPassed => string.IsNullOrEmpty(requiredBuildingAfterNight) ||
            (GameFoundation.Base.BuildingUpgradeService.IsBuilt(requiredBuildingAfterNight) &&
             GameFoundation.MetaProgression.DayCycleService.Instance != null &&
             GameFoundation.MetaProgression.DayCycleService.Instance.Day >
             GameFoundation.Saves.SaveSlotPrefs.GetInt("foundation.building." + requiredBuildingAfterNight + ".builtDay", 1));
    }
}
