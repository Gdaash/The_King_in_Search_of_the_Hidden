using UnityEngine;

namespace GameFoundation.Audio
{
    public enum GameAudioCue
    {
        UiClick, UiOpen, UiDenied, UiConfirm,
        ResourceGain, ResourceSpend, ProductionComplete, BuildingComplete,
        Portal, BowAttack, SwordAttack, MagicAttack, Hit, Death,
        Danger, MonsterSpawn, FootstepA, FootstepB, WoodWork, StoneWork,
        DialogueWord, DialogueOpen, DialogueNext, DialogueClose, QuestAccept, QuestComplete, QuestReward, ContentUnlock, QuestFly, RefugeesArrival, RefugeeAdmit, RecruitWarrior, DisarmWarrior
    }

    [CreateAssetMenu(menuName = "Game Foundation/Audio/Game Audio Library")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [Header("Музыка сцен")]
        public AudioClip mainMenuMusic;
        public AudioClip baseMusic;
        public AudioClip worldMusic;

        [Header("Интерфейс")]
        public AudioClip uiClick;
        public AudioClip uiOpen;
        public AudioClip uiDenied;
        public AudioClip uiConfirm;
        [Header("Диалоги, задания и жители")]
        public AudioClip dialogueWord, dialogueOpen, dialogueNext, dialogueClose;
        public AudioClip questAccept, questComplete, questReward, contentUnlock, questFly;
        public AudioClip refugeesArrival, refugeeAdmit, recruitWarrior, disarmWarrior;

        [Header("Экономика и строительство")]
        public AudioClip resourceGain;
        public AudioClip resourceSpend;
        public AudioClip productionComplete;
        public AudioClip buildingComplete;

        [Header("Мир и бой")]
        public AudioClip portal;
        public AudioClip bowAttack;
        public AudioClip swordAttack;
        public AudioClip magicAttack;
        public AudioClip hit;
        public AudioClip death;
        public AudioClip danger;
        public AudioClip monsterSpawn;
        public AudioClip footstepA;
        public AudioClip footstepB;
        public AudioClip woodWork;
        public AudioClip stoneWork;

        public AudioClip Get(GameAudioCue cue) => cue switch
        {
            GameAudioCue.UiClick => uiClick,
            GameAudioCue.UiOpen => uiOpen,
            GameAudioCue.UiDenied => uiDenied,
            GameAudioCue.UiConfirm => uiConfirm,
            GameAudioCue.DialogueWord => dialogueWord,
            GameAudioCue.DialogueOpen => dialogueOpen,
            GameAudioCue.DialogueNext => dialogueNext,
            GameAudioCue.DialogueClose => dialogueClose,
            GameAudioCue.QuestAccept => questAccept,
            GameAudioCue.QuestComplete => questComplete,
            GameAudioCue.QuestReward => questReward,
            GameAudioCue.ContentUnlock => contentUnlock,
            GameAudioCue.QuestFly => questFly,
            GameAudioCue.RefugeesArrival => refugeesArrival,
            GameAudioCue.RefugeeAdmit => refugeeAdmit,
            GameAudioCue.RecruitWarrior => recruitWarrior,
            GameAudioCue.DisarmWarrior => disarmWarrior,
            GameAudioCue.ResourceGain => resourceGain,
            GameAudioCue.ResourceSpend => resourceSpend,
            GameAudioCue.ProductionComplete => productionComplete,
            GameAudioCue.BuildingComplete => buildingComplete,
            GameAudioCue.Portal => portal,
            GameAudioCue.BowAttack => bowAttack,
            GameAudioCue.SwordAttack => swordAttack,
            GameAudioCue.MagicAttack => magicAttack,
            GameAudioCue.Hit => hit,
            GameAudioCue.Death => death,
            GameAudioCue.Danger => danger,
            GameAudioCue.MonsterSpawn => monsterSpawn,
            GameAudioCue.FootstepA => footstepA,
            GameAudioCue.FootstepB => footstepB,
            GameAudioCue.WoodWork => woodWork,
            GameAudioCue.StoneWork => stoneWork,
            _ => null
        };
    }
}
