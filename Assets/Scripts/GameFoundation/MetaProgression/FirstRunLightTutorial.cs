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
        [SerializeField, Min(0)] private float lightIntroDelay = .6f;
        public static bool BlocksInput { get; private set; }
        public static bool AllowsLightDrag { get; private set; }
        public static bool AllowsLightActivation => active != null && active.step == Step.Activate;
        public static bool AllowsMilitaryToggle { get; private set; }
        private static FirstRunLightTutorial active;
        enum Step { Inactive, Activate, Scout, Opening, Drag, Workers, Supplies, EmergencyReturn, Complete, Mining, Carriers, Delivery, CrystalIntro, FoodWarning, Activating, Military, MilitaryIntro, SecondLight }
        private Step step;
        public static bool WaitsForLightExplanation => active != null &&
            (active.step == Step.Activate || active.step == Step.Activating || active.step == Step.Scout);
        private Vector2 firstHex;
        private HexBlocker firstTutorialHex;
        private bool firstHexPrepared;
        private float previousSpeed;
        private bool paused;
        private bool previousAutoRepeat, autoRepeatOverridden;
        private int deliveredResources;
        public const string CarrierInstruction = "Вот и первая добыча! Носильщики подкатят телеги и отвезут её в портал. Они собирают припасы только на освещённой земле – в темноте ничего не найдут. Дождёмся, пока в портал привезут хотя бы две единицы добычи.";
        private readonly HashSet<ResourceType> tutorialResources = new();
        private readonly List<Behaviour> suspended = new();
        private readonly List<(CanvasGroup group, bool created, bool previousBlocks)> blockers = new();
        private const string CompletedKey = "foundation.tutorial.light.completed";
        private const string CrystalIntroKey = "foundation.tutorial.crystals.seen";
        private System.Action crystalIntroCompleted;
        private System.Action warningClosed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState() { active = null; BlocksInput = false; AllowsLightDrag = false; AllowsMilitaryToggle = false; }
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
            hold.Configure(()=>step==Step.Scout || step==Step.Workers || step==Step.Carriers || step==Step.Supplies || step==Step.SecondLight || step==Step.CrystalIntro || step==Step.FoodWarning || step==Step.MilitaryIntro, OnNext);
        }
        private void OnEnable()
        {
            if (!enabled) return;
            instruction.OnPreRenderText += MirrorMilitaryMouseIcon;
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
            if (PortalTowerProgression.Instance?.UsesEnergyLoadout == true)
            {
                Show(PortalArsenalPopup.T("energy.tutorial", "Арсенал находится в магической башне на базе. Назначь оружие в слоты перед вылазкой, а в бою переключай его клавишами 2–5. Клавиша 1 выключает оружие. Кристаллы дают энергию для выстрелов и дополнительных рабочих лучей. Если энергии станет меньше отметки, дополнительный луч погаснет."));
                ShowNext(); return true;
            }
            if (PortalTowerProgression.Instance?.UsesArsenal == true)
            {
                Show(PortalArsenalPopup.T("tutorial", "Башня уже вооружена магическими снарядами. Нажми на неё, чтобы открыть арсенал: там можно изучить оружие и условия его открытия, а затем свободно переключаться между открытыми видами. Кристаллы по-прежнему повышают уровень башни и дают улучшения и рабочие лучи."));
                ShowNext(); return true;
            }
            Show("А теперь маленький секрет: башню можно вооружить. Я называю это Кристальной эгидой. Кристаллы встречаются на открытых участках Тёмных земель; носильщики привезут их к порталу, и башня станет сильнее. Для начала выбери ей оружие. Соседям с клыками такой поворот вряд ли понравится!");
            ShowNext();return true;
        }
        public bool TryExplainMilitary()
        {
            if (!isActiveAndEnabled || BlocksInput || (PlaytestSettings.TutorialsAndQuestsDisabled && !PlaytestSettings.ForceMilitaryLightTutorial)) return false;
            LockInput(); Pause(); step = Step.MilitaryIntro; AllowsMilitaryToggle = false;
            const string key = "world.military.intro";
            string text = LocalizationService.Instance?.Get(key);
            Show(string.IsNullOrEmpty(text) || text == key ? "Открытый гекс в луче света можно перевести в военный режим. Вместо работников и носильщиков его займут воины: лучники встанут в центре, а мечники прикроют их снаружи. Если военных гексов несколько, войска разделятся между ними. Повторное переключение вернёт гекс к добыче и сбору." : text);
            ShowNext();
            return true;
        }
        public void CompleteMilitaryExplanation()
        {
            if (step != Step.Military) return;
            SaveSlotPrefs.SetInt(WorldFlashlightAvailability.MilitaryTutorialKey, 1); SaveSlotPrefs.Save();
            step = Step.Complete; window.SetActive(false); Unlock(); ResumeTime();
        }
        private void MirrorMilitaryMouseIcon(TMP_TextInfo textInfo)
        {
            if (step != Step.Military) return;
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                var character = textInfo.characterInfo[i];
                if (character.elementType != TMP_TextElementType.Sprite) continue;
                var uv = textInfo.meshInfo[character.materialReferenceIndex].uvs0;
                int v = character.vertexIndex;
                var lower = uv[v]; uv[v] = uv[v + 3]; uv[v + 3] = lower;
                var upper = uv[v + 1]; uv[v + 1] = uv[v + 2]; uv[v + 2] = upper;
            }
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
        public bool TryShowCartRecovery(string text, System.Action closed)
        {
            if (!isActiveAndEnabled || BlocksInput || PortalTowerProgression.IsChoosingUpgrade) return false;
            warningClosed = closed; LockInput(); AllowsLightDrag = false; step = Step.FoodWarning; Pause();
            Show(text); ShowNext(); return true;
        }
        public bool TryExplainReturnOptions()
        {
            if (PlaytestSettings.TutorialsAndQuestsDisabled) return false;
            const string key = "world.tutorial.return_options";
            string text = LocalizationService.Instance?.Get(key);
            return TryShowCartRecovery(string.IsNullOrEmpty(text) || text == key
                ? "Монстры услышали наших рабочих! Теперь ты можешь вернуться домой или попробовать добыть ещё немного ресурсов. Только следи за людьми и башней – не стоит рисковать всем ради лишней охапки дров." : text, null);
        }
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
            bool closes = step == Step.SecondLight || step == Step.FoodWarning || step == Step.CrystalIntro;
            var label = nextButton.GetComponentInChildren<TMP_Text>(true);
            label.text=GameText.Get(closes ? "Закрыть" : "Далее");
            var theme = Resources.Load<GameFoundation.UI.ButtonVisualTheme>("UI/ButtonVisualTheme");
            if (theme != null) label.color = theme.neutralLabelColor;
            nextButton.gameObject.SetActive(true);
        }
        private void OnActivated()
        {
            if(step!=Step.Activate)return;
            step=Step.Activating;
            window.SetActive(false);
            StartCoroutine(ExplainActivatedLight());
        }
        private System.Collections.IEnumerator ExplainActivatedLight()
        {
            while (step == Step.Activating && lights != null && !lights.HasVisibleLight) yield return null;
            yield return new WaitForSecondsRealtime(lightIntroDelay);
            if (step != Step.Activating || lights == null || lights.Escaped) yield break;
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
            if((step!=Step.Opening&&step!=Step.Workers&&step!=Step.Mining&&step!=Step.Carriers&&step!=Step.Delivery)||source==null||item==null||item.type==null||
                ((Vector2)source.transform.position-firstHex).sqrMagnitude>1f)return;
            tutorialResources.Add(item.type);
            if(step==Step.Mining)ShowCarriers();
        }
        private void ShowCarriers()
        {
            deliveredResources=0;step=Step.Carriers;Pause();
            Show(CarrierInstruction);ShowNext();
        }
        private void OnResourceDeposited(ResourceType type)
        {
            if(step!=Step.Delivery||!tutorialResources.Contains(type))return;
            deliveredResources++;
            if(deliveredResources>=2)ShowDrag();
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
            if (step == Step.MilitaryIntro)
            {
                step = Step.Military; AllowsMilitaryToggle = true;
                const string key = "world.military.practice";
                string text = LocalizationService.Instance?.Get(key);
                Show(string.IsNullOrEmpty(text) || text == key ? "А теперь нажми <size=64><sprite=0></size> на отпущенный луч света на открытом гексе. Рамка станет синей, а воины займут позицию!" : text);
                return;
            }
            if(step==Step.FoodWarning)
            {
                var closed=warningClosed;warningClosed=null;
                step=Step.Complete;window.SetActive(false);Unlock();ResumeTime();closed?.Invoke();return;
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
                return;
            }
            if(step==Step.Supplies)
            {
                step=Step.SecondLight;
                Show(LocalizationService.Instance.Get("world.tutorial.second_light")); ShowNext(); return;
            }
            if (step == Step.SecondLight)
            {
                step=Step.Complete;window.SetActive(false);SaveSlotPrefs.SetInt(CompletedKey,1);SaveSlotPrefs.Save();Unlock();ResumeTime();
                if (PortalTowerProgression.Instance == null || !PortalTowerProgression.Instance.ShowTutorialLightReward(ShowCameraHint)) ShowCameraHint(); return;
            }
        }
        private void ShowCameraHint()
        {
            StartCoroutine(CameraHint());
        }
        private System.Collections.IEnumerator CameraHint()
        {
            var canvas = window.GetComponentInParent<Canvas>().rootCanvas;
            var root = new GameObject("Camera movement hint", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0); rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, 96);
            var background = root.GetComponent<Image>();
            var authored = continueHint.GetComponent<Image>();
            background.sprite = authored != null ? authored.sprite : null;
            background.type = Image.Type.Sliced; background.raycastTarget = false;
            background.color = background.sprite != null ? Color.white : new Color(.12f,.09f,.15f,.97f);
            var labelRoot = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));labelRoot.transform.SetParent(rect, false);
            var label = labelRoot.GetComponent<TextMeshProUGUI>();label.font = instruction.font;label.fontSize = 24;label.alignment = TextAlignmentOptions.Center;label.color = new Color(.95f,.91f,.76f);label.raycastTarget = false;
            var lr = label.rectTransform;lr.anchorMin = Vector2.zero;lr.anchorMax = Vector2.one;lr.offsetMin = new Vector2(20,8);lr.offsetMax = new Vector2(-20,-8);
            var group = root.GetComponent<CanvasGroup>();group.blocksRaycasts = false;group.interactable = false;
            float started = Time.unscaledTime;float dismissed = -1;string previousLanguage = null;
            while (root != null && lights != null && !lights.Escaped)
            {
                string language = LocalizationService.Instance?.Language;
                if (previousLanguage != language || string.IsNullOrEmpty(label.text))
                {
                    previousLanguage = language;
                    const string key = "world.tutorial.camera_hint";string format = LocalizationService.Instance?.Get(key);
                    label.text = string.Format(string.IsNullOrEmpty(format) || format == key ? "Перемещать камеру: {0}" : format, "<b>WASD</b>");
                    rect.sizeDelta = new Vector2(Mathf.Ceil(label.GetPreferredValues(label.text).x) + 48, 56);
                }
                bool blocked = BlocksInput || PortalTowerProgression.IsChoosingUpgrade;
                if (!blocked && Time.unscaledTime - started > 3 && dismissed < 0 &&
                    (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))) dismissed = Time.unscaledTime;
                float alpha = dismissed < 0 ? 1 : 1 - (Time.unscaledTime - dismissed) / .4f;
                group.alpha = blocked ? 0 : Mathf.Clamp01(alpha);
                if (alpha <= 0) break;
                yield return null;
            }
            if (root != null) Destroy(root);
        }
        private void Unlock(bool preserveGroups=false)
        {
            if(active!=this)return;
            active=null;BlocksInput=false;AllowsLightDrag=false;AllowsMilitaryToggle=false;
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
            if (instruction != null) instruction.OnPreRenderText -= MirrorMilitaryMouseIcon;
            if(lights!=null){lights.LightsTurnedOn-=OnActivated;lights.LightHexOpened-=OnHexOpened;lights.LightDraggedToHex-=OnLightMoved;}
            ResourceRequester.ResourceProduced-=OnResourceProduced;
            Warehouse.ResourceDeposited-=OnResourceDeposited;
            StopAllCoroutines();
            crystalIntroCompleted=null;
            warningClosed=null;
            Unlock();ResumeTime();
        }
    }
}
