using System.Collections;
using System.Collections.Generic;
using GameFoundation.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameFoundation.Audio
{
    public sealed class GameAudioController : MonoBehaviour
    {
        public static GameAudioController Instance { get; private set; }
        public event System.Action<GameAudioCue> CuePlayed;

        [SerializeField] private GameAudioLibrary library;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float worldVolume = 0.65f;
        [SerializeField, Min(1)] private int worldVoiceCount = 14;

        private AudioSource musicSource;
        private AudioSource uiSource;
        private AudioSource resourceSource;
        private readonly Queue<double> resourceVoiceEnds = new();
        private readonly List<AudioSource> worldSources = new();
        private readonly Dictionary<ResourceType, int> resourceAmounts = new();
        private readonly Dictionary<GameAudioCue, float> lastPlayTime = new();
        private int nextWorldVoice;
        private GameSettingsService observedSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("Game Audio Controller");
            DontDestroyOnLoad(go);
            go.AddComponent<GameAudioController>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (library == null)
                library = ProjectReferences.Instance?.audioLibrary ?? Resources.Load<GameAudioLibrary>("GameAudioLibrary");
            musicSource = CreateSource("Music", 0f, true);
            uiSource = CreateSource("UI SFX", 0f, false);
            resourceSource = CreateSource("Resource SFX", 0f, false);
            for (int i = 0; i < worldVoiceCount; i++) worldSources.Add(CreateSource($"World SFX {i + 1}", 0.68f, false));
            SceneManager.sceneLoaded += OnSceneLoaded;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
        }

        private void Start()
        {
            ObserveSettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GlobalResourceManager.OnResourceChanged -= OnResourceChanged;
            if (observedSettings != null) observedSettings.Changed -= ApplyVolumes;
        }

        private AudioSource CreateSource(string sourceName, float spatialBlend, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = spatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 3f;
            source.maxDistance = 22f;
            return source;
        }

        private void Update()
        {
            ObserveSettings();

        }

        private void ObserveSettings()
        {
            if (observedSettings == GameSettingsService.Instance) return;
            if (observedSettings != null) observedSettings.Changed -= ApplyVolumes;
            observedSettings = GameSettingsService.Instance;
            if (observedSettings != null) observedSettings.Changed += ApplyVolumes;
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            float musicSetting = observedSettings != null ? observedSettings.Music : 1f;
            float effectsSetting = observedSettings != null ? observedSettings.Effects : 1f;
            if (musicSource != null) musicSource.volume = musicVolume * musicSetting;
            if (uiSource != null) uiSource.volume = uiVolume * effectsSetting;
            if (resourceSource != null) resourceSource.volume = uiVolume * effectsSetting;
            foreach (AudioSource source in worldSources) if (source != null) source.volume = worldVolume * effectsSetting;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            resourceAmounts.Clear();
            resourceVoiceEnds.Clear();
            resourceSource.Stop();
            PlaySceneMusic(scene.name);
            DiscoverRuntimeObjects();
        }

        private void PlaySceneMusic(string sceneName)
        {
            if (library == null || musicSource == null) return;
            AudioClip clip = sceneName switch
            {
                "MainMenu" => library.mainMenuMusic,
                "Base" => library.baseMusic,
                "World" => library.worldMusic,
                _ => null
            };
            if (musicSource.clip == clip && musicSource.isPlaying) return;
            musicSource.Stop();
            musicSource.clip = clip;
            if (clip != null) musicSource.Play();
        }

        // Scene bootstrap handles existing/inactive controls once. Runtime objects register from their lifecycle.
        private void DiscoverRuntimeObjects()
        {
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) RegisterHierarchy(root);
            InitializeResourceSnapshot();
        }

        public static void RegisterHierarchy(GameObject root)
        {
            if (root == null) return;
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true)) Register(component);
        }

        public static void Register(Component component)
        {
            if (component == null) return;
            switch (component)
            {
                case Button value:
                    if (!value.TryGetComponent<ButtonAudioHook>(out _)) value.gameObject.AddComponent<ButtonAudioHook>().Setup(value);
                    break;
                case Health value:
                    if (!value.TryGetComponent<HealthAudioHook>(out _)) value.gameObject.AddComponent<HealthAudioHook>().Setup(value);
                    break;
                case EnemyAI value:
                    if (!value.TryGetComponent<CombatAudioHook>(out _)) value.gameObject.AddComponent<CombatAudioHook>().Setup(value);
                    break;
                case EnemyAI_Ranged value:
                    if (!value.TryGetComponent<CombatAudioHook>(out _)) value.gameObject.AddComponent<CombatAudioHook>().Setup(value);
                    break;
                case ArcherTower value:
                    if (!value.TryGetComponent<CombatAudioHook>(out _)) value.gameObject.AddComponent<CombatAudioHook>().Setup(value);
                    break;
                case TimerController value:
                    if (!value.TryGetComponent<TimerAudioHook>(out _)) value.gameObject.AddComponent<TimerAudioHook>().Setup(value);
                    break;
                case HexLightUnlocker value:
                    if (!value.TryGetComponent<HexAudioHook>(out _)) value.gameObject.AddComponent<HexAudioHook>().Setup(value);
                    break;
                case AlarmSystem value:
                    if (!value.TryGetComponent<AlarmAudioHook>(out _)) value.gameObject.AddComponent<AlarmAudioHook>().Setup(value);
                    break;
                case HumanUnit _:
                case Porter _:
                case EnemyMovement _:
                    if (!component.TryGetComponent<FootstepAudioHook>(out _)) component.gameObject.AddComponent<FootstepAudioHook>();
                    break;
            }
        }

        private void InitializeResourceSnapshot()
        {
            if (GlobalResourceManager.Instance == null || resourceAmounts.Count > 0) return;
            foreach (ResourceType resource in GlobalResourceManager.Instance.AvailableResources)
                if (resource != null) resourceAmounts[resource] = GlobalResourceManager.Instance.GetResourceAmount(resource);
        }

        private void OnResourceChanged(ResourceType type, int amount)
        {
            if (type == null) return;
            if (!resourceAmounts.TryGetValue(type, out int previous)) { resourceAmounts[type] = amount; return; }
            resourceAmounts[type] = amount;
            if (amount > previous) PlayResourceGain(0.72f);
            else if (amount < previous) PlayUI(GameAudioCue.ResourceSpend, 0.62f, 0.96f, 1.04f, 0.07f);
        }

        public static void PlayResourceGain(float volume)
        {
            var controller = Instance;
            if (controller == null || controller.resourceSource == null) return;
            var clip = controller.library != null ? controller.library.resourceGain : null;
            if (clip == null) return;

            var activeVoices = controller.resourceVoiceEnds;
            double now = AudioSettings.dspTime;
            while (activeVoices.Count > 0 && activeVoices.Peek() <= now) activeVoices.Dequeue();
            // Track only voice lifetimes. Every drop starts a one-shot immediately.
            float gain = Mathf.Clamp01(volume) / (1f + activeVoices.Count * .65f);
            controller.resourceSource.PlayOneShot(clip, gain);
            activeVoices.Enqueue(now + clip.length);
            controller.CuePlayed?.Invoke(GameAudioCue.ResourceGain);
        }

        public static void PlayUI(GameAudioCue cue, float volume = 1f, float pitchMin = 0.98f, float pitchMax = 1.02f, float cooldown = 0.025f)
        {
            Instance?.Play(Instance.uiSource, cue, Vector3.zero, volume, pitchMin, pitchMax, cooldown, false);
        }

        public static void PlayAt(GameAudioCue cue, Vector3 position, float volume = 1f, float pitchMin = 0.94f, float pitchMax = 1.06f, float cooldown = 0.02f)
        {
            if (Instance == null || Instance.worldSources.Count == 0) return;
            AudioSource source = Instance.worldSources[Instance.nextWorldVoice++ % Instance.worldSources.Count];
            source.transform.position = position;
            Instance.Play(source, cue, position, volume, pitchMin, pitchMax, cooldown, true);
        }

        private void Play(AudioSource source, GameAudioCue cue, Vector3 position, float volume, float pitchMin, float pitchMax, float cooldown, bool world)
        {
            if (library == null || source == null) return;
            AudioClip clip = library.Get(cue);
            if (clip == null) return;
            if (lastPlayTime.TryGetValue(cue, out float last) && Time.unscaledTime - last < cooldown) return;
            lastPlayTime[cue] = Time.unscaledTime;
            source.pitch = Random.Range(pitchMin, pitchMax);
            if (world) source.transform.position = position;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
            CuePlayed?.Invoke(cue);
        }
    }
}
