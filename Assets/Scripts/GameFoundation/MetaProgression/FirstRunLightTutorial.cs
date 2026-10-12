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
        public static bool BlocksInput { get; private set; }
        public static bool AllowsLightDrag { get; private set; }
        private static FirstRunLightTutorial active;
        enum Step { Inactive, Activate, Scout, Opening, Drag, Workers, Supplies, Complete }
        private Step step;
        private Vector2 firstHex;
        private float previousSpeed;
        private bool paused;
        private readonly List<Behaviour> suspended = new();
        private readonly List<(CanvasGroup group, bool created, bool previousBlocks)> blockers = new();
        private const string CompletedKey = "foundation.tutorial.light.completed";
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
            hold.Configure(()=>step==Step.Scout || step==Step.Workers || step==Step.Supplies, OnNext, .7f, new Color(.55f,.7f,.85f,1));
        }
        private void OnEnable()
        {
            if (!enabled) return;
            lights.LightsTurnedOn += OnActivated; lights.LightHexOpened += OnHexOpened; lights.LightDraggedToHex += OnLightMoved;
        }
        private void Start()
        {
            if (!DayCycleService.IsFirstPortalRun || SaveSlotPrefs.GetInt(CompletedKey,0)!=0) return;
            active=this; BlocksInput=true;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if(canvas!=canvas.rootCanvas)continue;
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
            step=Step.Activate;
            Show("Включи свет, чтобы начать разведку. Нажми кнопку включения света у башни портала.");
            if(lights.LightsActivated)OnActivated();
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
            Show("Луч света — наша разведка. С его помощью можно разведывать территорию вокруг башни портала.");ShowNext();
        }
        private void OnHexOpened(Vector2 position)
        {
            if(step!=Step.Opening)return;
            firstHex=position;step=Step.Drag;AllowsLightDrag=true;Pause();
            Show("Зажми левую кнопку мыши <size=64><sprite=0></size> на луче света и перетащи его на соседний гекс.");
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
            AllowsLightDrag=false;step=Step.Workers;Pause();
            Show("Люди могут работать только на освещённых гексах. Чтобы добыть ресурс, перенеси луч света на гекс с этим ресурсом.");
            ShowNext();
        }
        private void OnNext()
        {
            if(step==Step.Scout){step=Step.Opening;window.SetActive(false);ResumeTime();return;}
            if(step==Step.Supplies)
            {
                step=Step.Complete;window.SetActive(false);SaveSlotPrefs.SetInt(CompletedKey,1);SaveSlotPrefs.Save();Unlock();ResumeTime();return;
            }
            if(step!=Step.Workers)return;
            step=Step.Supplies;
            Show("Собери припасы для укрытия, чтобы никто не умер с голоду.");ShowNext();
        }
        private void Unlock()
        {
            if(active!=this)return;
            active=null;BlocksInput=false;AllowsLightDrag=false;
            foreach(var blocker in blockers)
            {
                if(blocker.group==null)continue;
                if(blocker.created)Destroy(blocker.group);
                else blocker.group.blocksRaycasts=blocker.previousBlocks;
            }
            blockers.Clear();
            foreach(var behaviour in suspended)if(behaviour!=null)behaviour.enabled=true;suspended.Clear();
        }
        private void OnDisable()
        {
            if(lights!=null){lights.LightsTurnedOn-=OnActivated;lights.LightHexOpened-=OnHexOpened;lights.LightDraggedToHex-=OnLightMoved;}
            Unlock();ResumeTime();
        }
    }
}
