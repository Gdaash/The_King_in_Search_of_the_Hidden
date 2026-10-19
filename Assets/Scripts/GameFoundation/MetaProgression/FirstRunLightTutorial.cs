using System.Collections.Generic;
using GameFoundation.Localization;
using GameFoundation.Quests;
using GameFoundation.Saves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameFoundation.MetaProgression
{
    [DefaultExecutionOrder(500)]
    public sealed class FirstRunLightTutorial : MonoBehaviour
    {
        [SerializeField] private WorldFlashlightAvailability lights;
        [SerializeField] private GameObject window, continueHint;
        [SerializeField] private TMP_Text instruction;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject firstHexForest;
        public static bool BlocksInput { get; private set; }
        public static bool AllowsLightDrag { get; private set; }
        private static FirstRunLightTutorial active;
        enum Step { Inactive, Activate, Scout, Opening, Drag, Workers, Supplies, EmergencyReturn, Complete, Mining, Carriers, Delivery, CrystalIntro, FoodWarning }
        private Step step;
        private Vector2 firstHex;
        private HexBlocker firstTutorialHex;
        private bool firstHexPrepared;
        private float previousSpeed;
        private bool paused;
        private bool previousAutoRepeat, autoRepeatOverridden, firstResourceDelivered;
        private readonly HashSet<ResourceType> tutorialResources = new();
        private readonly List<Behaviour> suspended = new();
        private readonly List<(CanvasGroup group, bool created, bool previousBlocks)> blockers = new();
        private const string CompletedKey = "foundation.tutorial.light.completed";
        private const string CrystalIntroKey = "foundation.tutorial.crystals.seen";
        private System.Action crystalIntroCompleted;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { active = null; BlocksInput = false; AllowsLightDrag = false; }
        private void Awake()
        {
            if (window == null || instruction == null || lights == null || nextButton == null || continueHint == null)
            {
                Debug.LogError("FirstRunLightTutorial requires its configured prefab and a light controller.", this);
                enabled = false;
                return;
            }
            window.SetActive(false); continueHint.SetActive(false); nextButton.gameObject.SetActive(false);
            var hold = nextButton.GetComponent<DialogueHoldButton>();
            if (hold == null) hold = nextButton.gameObject.AddComponent<DialogueHoldButton>();
            hold.Configure(()=>step==Step.Scout || step==Step.Workers || step==Step.Carriers || step==Step.Supplies || step==Step.CrystalIntro || step==Step.FoodWarning, OnNext);
        }
        private void OnEnable()
        {
            if (!enabled) return;
            lights.LightsTurnedOn += OnActivated; lights.LightHexOpened += OnHexOpened; lights.LightDraggedToHex += OnLightMoved;
            ResourceRequester.ResourceProduced += OnResourceProduced;
            Warehouse.ResourceDeposited += OnResourceDeposited;
        }
        private System.Collections.IEnumerator Start()
        {
            if(step==Step.CrystalIntro)yield break;
            while (PortalTowerProgression.IsChoosingUpgrade) yield return null;
            if (PlaytestSettings.TutorialsAndQuestsDisabled) yield break;
            if (!DayCycleService.IsFirstPortalRun || SaveSlotPrefs.GetInt(CompletedKey,0)!=0) yield break;
            LockInput();
            previousAutoRepeat=lights.AutoRepeat;autoRepeatOverridden=true;lights.SetAutoRepeat(true);
            step=Step.Activate;
            Show("Включи свет у башни портала. Посмотрим, что Тёмные земли приберегли для нас сегодня.");
            if(lights.LightsActivated)OnActivated();
        }
        public bool TryExplainCrystals(System.Action onComplete)
        {
            if(!isActiveAndEnabled||PlaytestSettings.TutorialsAndQuestsDisabled||
                DayCycleService.Instance==null||DayCycleService.Instance.Day<2||SaveSlotPrefs.GetInt(CrystalIntroKey,0)!=0)return false;
            crystalIntroCompleted=onComplete;
            LockInput();AllowsLightDrag=false;step=Step.CrystalIntro;Pause();
            Show("А теперь маленький секрет: башню можно вооружить. Я называю это Кристальной эгидой. Кристаллы встречаются на открытых участках Тёмных земель; носильщики привезут их к порталу, и башня станет сильнее. Для начала выбери ей оружие. Соседям с клыками такой поворот вряд ли понравится!");
            ShowNext();return true;
        }
        private void LockInput()
        {
            if (active == this) return;
            active=this; BlocksInput=true;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(canvas!=canvas.rootCanvas||canvas.GetComponent<PortalCloudTransition>()!=null)continue;
                var blocker=canvas.GetComponent<CanvasGroup>(); bool created=blocker==null;
                if(created)blocker=canvas.gameObject.AddComponent<CanvasGroup>();
                blockers.Add((blocker,created,blocker.blocksRaycasts)); blocker.blocksRaycasts=false;
            }
            // The tutorial's hold button is the only Canvas control allowed through the lock.
            var group=nextButton.GetComponent<CanvasGroup>();
            if(group==null)group=nextButton.gameObject.AddComponent<CanvasGroup>();
            group.ignoreParentGroups=true; group.blocksRaycasts=true;
            foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                string name=behaviour.GetType().Name;
                if(behaviour.enabled&&(name=="ContentZoom"||name=="BuildingPlacer"||name=="CheatResourcePopup"||name=="FlashlightController"))
                {suspended.Add(behaviour);behaviour.enabled=false;}
            }
        }
        public bool EmergencyReturnActive => step == Step.EmergencyReturn;
        public bool TryWarnAboutFood(bool lastWarning)
        {
            if(!isActiveAndEnabled||BlocksInput)return false;
            LockInput();AllowsLightDrag=false;step=Step.FoodWarning;Pause();
            Show(lastWarning ? "Я тебя предупредил, рыцарь. Решение за тобой, но людям от этого сытнее не станет. Больше удерживать не буду."
                : "Погоди, рыцарь, еды на всех ещё не хватит! Вернёмся с пустыми телегами – кто-нибудь не переживёт голодную ночь. Добудь ещё припасов, а потом отправимся домой.");
            ShowNext();return true;
        }
        private CanvasGroup emergencyButtonGroup;
        private bool emergencyIgnoreParentGroups;
        public void BeginEmergencyReturn(Button returnButton)
        {
            LockInput();
            AllowsLightDrag = false;
            step = Step.EmergencyReturn;
            Pause();
            Show("Рыцарь, портал едва держится! Возвращайся немедленно, пока эти твари не разрушили башню. Соберём людей у портала и уйдём вместе – живыми мы ещё всё восстановим.");
            emergencyButtonGroup = returnButton.GetComponent<CanvasGroup>();
            if (emergencyButtonGroup == null) emergencyButtonGroup = returnButton.gameObject.AddComponent<CanvasGroup>();
            emergencyIgnoreParentGroups = emergencyButtonGroup.ignoreParentGroups;
            emergencyButtonGroup.ignoreParentGroups = true;
            emergencyButtonGroup.interactable = true;
            emergencyButtonGroup.blocksRaycasts = true;
        }
        public void CompleteEmergencyReturn()
        {
            if (!EmergencyReturnActive) return;
            step = Step.Complete;
            window.SetActive(false);
            if (emergencyButtonGroup != null) emergencyButtonGroup.ignoreParentGroups = emergencyIgnoreParentGroups;
            SaveSlotPrefs.SetInt(CompletedKey, 1);
            SaveSlotPrefs.Save();
            Unlock();
            if (previousSpeed <= 0f) previousSpeed = 1f;
            ResumeTime();
        }
        private void Pause()
        {
            if(!paused){previousSpeed=GameSpeedControls.SimulationSpeed;paused=true;}
            GameSpeedControls.SetSimulationSpeed(0);
        }
        private void ResumeTime(){if(!paused)return;paused=false;GameSpeedControls.SetSimulationSpeed(previousSpeed);}
        private void Show(string text)
        {
            continueHint.SetActive(false);nextButton.gameObject.SetActive(false);
            instruction.text=GameText.Get(text);window.SetActive(true);
        }
        private void ShowNext()
        {
            continueHint.SetActive(false);
            nextButton.GetComponentInChildren<TMP_Text>(true).text=GameText.Get("Далее");
            nextButton.gameObject.SetActive(true);
        }
        private void OnActivated()
        {
            if(step!=Step.Activate)return;
            step=Step.Scout;Pause();
            Show("Этот луч – наши глаза в Тёмных землях. Куда он доберётся, там и узнаем, что нас ждёт. Хорошая разведка сбережёт немало жизней.");ShowNext();
        }
        private void OnHexOpened(Vector2 position)
        {
            if(step!=Step.Opening)return;
            if(firstHexPrepared&&(position-firstHex).sqrMagnitude>.01f)return;
            firstHex=position;
            StartCoroutine(WaitForFirstHex(firstTutorialHex));
        }
        public static void PrepareFirstHex(HexBlocker hex)
        {
            if(active==null||active.step!=Step.Opening||active.firstHexPrepared||hex==null||active.firstHexForest==null)return;
            active.firstHexPrepared=true;
            active.firstTutorialHex=hex;
            active.firstHex=hex.transform.position;
            // Replace the random assignment before opening, using the normal single spawn path.
            hex.AssignContent(active.firstHexForest,0,false);
        }
        private System.Collections.IEnumerator WaitForFirstHex(HexBlocker blocker)
        {
            // Let the fog's existing fade and disappearance finish before pausing it.
            while(blocker!=null&&blocker.gameObject.activeInHierarchy)yield return null;
            yield return new WaitForSeconds(.2f);
            if(step!=Step.Opening)yield break;
            step=Step.Workers;Pause();
            Show("Люди трудятся только там, где светло. Хочешь добыть дерево, камень или еду – направь луч на нужный участок. В темноту я бы и сам без нужды не полез!");ShowNext();
        }
        private void OnResourceProduced(ResourceRequester source, ResourceItem item)
        {
            if((step!=Step.Opening&&step!=Step.Workers&&step!=Step.Mining)||source==null||item==null||item.type==null||
                ((Vector2)source.transform.position-firstHex).sqrMagnitude>1f)return;
            tutorialResources.Add(item.type);
            if(step==Step.Mining)ShowCarriers();
        }
        private void ShowCarriers()
        {
            step=Step.Carriers;Pause();
            Show("Вот и первая добыча! Теперь носильщики подкатят телеги и отвезут её в портал. Дождёмся первой доставки – припасы ещё должны добраться до наших складов.");ShowNext();
        }
        private void OnResourceDeposited(ResourceType type)
        {
            if(!tutorialResources.Contains(type))return;
            firstResourceDelivered=true;
            if(step==Step.Delivery)ShowDrag();
        }
        private void ShowDrag()
        {
            step=Step.Drag;AllowsLightDrag=true;Pause();
            Show("Переведи луч на соседний гекс. Подхвати его <size=64><sprite=0></size> и укажи, куда двигаться дальше – пора разведать ещё немного земли.");
        }
        public static bool IsAllowedDragTarget(Vector2 position)
        {
            if(!AllowsLightDrag||active==null)return true;
            float closest=float.MaxValue;
            foreach(var magnet in HexMagnet.ActiveInstances)
            {
                if(magnet==null)continue;float distance=((Vector2)magnet.transform.position-active.firstHex).sqrMagnitude;
                if(distance>.05f)closest=Mathf.Min(closest,distance);
            }
            float target=(position-active.firstHex).sqrMagnitude;
            return target>.05f&&target<=closest*1.25f;
        }
        private void OnLightMoved(Vector2 position)
        {
            if(step!=Step.Drag||!IsAllowedDragTarget(position))return;
            AllowsLightDrag=false;step=Step.Supplies;Pause();
            Show("Отлично, теперь дело пойдёт! Добудь еду для наших семерых и дерево для домов. На сытый желудок даже Тёмные земли кажутся чуть приветливее.");ShowNext();
        }
        private void OnNext()
        {
            if(step==Step.FoodWarning)
            {
                step=Step.Complete;window.SetActive(false);Unlock();ResumeTime();return;
            }
            if(step==Step.CrystalIntro)
            {
                var completed=crystalIntroCompleted;crystalIntroCompleted=null;
                SaveSlotPrefs.SetInt(CrystalIntroKey,1);SaveSlotPrefs.Save();
                step=Step.Complete;window.SetActive(false);
                // The weapon picker takes ownership of the same CanvasGroup immediately.
                Unlock(true);ResumeTime();completed?.Invoke();return;
            }
            if(step==Step.Scout){step=Step.Opening;window.SetActive(false);ResumeTime();return;}
            if(step==Step.Workers)
            {
                step=Step.Mining;window.SetActive(false);ResumeTime();
                if(tutorialResources.Count>0)ShowCarriers();
                return;
            }
            if(step==Step.Carriers)
            {
                step=Step.Delivery;window.SetActive(false);ResumeTime();
                if(firstResourceDelivered)ShowDrag();
                return;
            }
            if(step==Step.Supplies)
            {
                step=Step.Complete;window.SetActive(false);SaveSlotPrefs.SetInt(CompletedKey,1);SaveSlotPrefs.Save();Unlock();ResumeTime();return;
            }
        }
        private void Unlock(bool preserveGroups=false)
        {
            if(active!=this)return;
            active=null;BlocksInput=false;AllowsLightDrag=false;
            if(autoRepeatOverridden&&lights!=null){autoRepeatOverridden=false;lights.SetAutoRepeat(previousAutoRepeat);}
            foreach(var blocker in blockers)
            {
                if(blocker.group==null)continue;
                if(blocker.created&&!preserveGroups)Destroy(blocker.group);
                else blocker.group.blocksRaycasts=blocker.previousBlocks;
            }
            blockers.Clear();
            foreach(var behaviour in suspended)if(behaviour!=null)behaviour.enabled=true;suspended.Clear();
        }
        private void OnDisable()
        {
            if(lights!=null){lights.LightsTurnedOn-=OnActivated;lights.LightHexOpened-=OnHexOpened;lights.LightDraggedToHex-=OnLightMoved;}
            ResourceRequester.ResourceProduced-=OnResourceProduced;
            Warehouse.ResourceDeposited-=OnResourceDeposited;
            StopAllCoroutines();
            crystalIntroCompleted=null;
            Unlock();ResumeTime();
        }
    }
}
