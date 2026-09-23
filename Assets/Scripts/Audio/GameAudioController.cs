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

        [SerializeField] private GameAudioLibrary library;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float worldVolume = 0.65f;
        [SerializeField, Min(1)] private int worldVoiceCount = 14;

        private AudioSource musicSource;
        private AudioSource uiSource;
        private readonly List<AudioSource> worldSources = new();
        private readonly Dictionary<ResourceType, int> resourceAmounts = new();
        private readonly Dictionary<GameAudioCue, float> lastPlayTime = new();
        private float nextDiscoveryTime;
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
            if (library == null) library = Resources.Load<GameAudioLibrary>("GameAudioLibrary");
            musicSource = CreateSource("Music", 0f, true);
            uiSource = CreateSource("UI SFX", 0f, false);
            for (int i = 0; i < worldVoiceCount; i++) worldSources.Add(CreateSource($"World SFX {i + 1}", 0.68f, false));
            SceneManager.sceneLoaded += OnSceneLoaded;
            GlobalResourceManager.OnResourceChanged += OnResourceChanged;
        }

        private void Start()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
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
            if (Time.unscaledTime < nextDiscoveryTime) return;
            nextDiscoveryTime = Time.unscaledTime + 0.75f;
            DiscoverRuntimeObjects();
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
            foreach (AudioSource source in worldSources) if (source != null) source.volume = worldVolume * effectsSetting;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            resourceAmounts.Clear();
            nextDiscoveryTime = 0f;
            PlaySceneMusic(scene.name);
            StartCoroutine(DisableLegacyMusicAtEndOfFrame());
        }

        private IEnumerator DisableLegacyMusicAtEndOfFrame()
        {
            yield return null;
            foreach (AudioSource source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (source == musicSource || source.transform.IsChildOf(transform)) continue;
                if (!source.name.Equals("BackgroundMusic", System.StringComparison.OrdinalIgnoreCase)) continue;
                source.Stop();
                source.enabled = false;
            }
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

        private void DiscoverRuntimeObjects()
        {
            foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (button.GetComponent<ButtonAudioHook>() == null) button.gameObject.AddComponent<ButtonAudioHook>().Setup(button);
            foreach (Health health in FindObjectsByType<Health>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (health.GetComponent<HealthAudioHook>() == null) health.gameObject.AddComponent<HealthAudioHook>().Setup(health);
            foreach (EnemyAI ai in FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ai.GetComponent<CombatAudioHook>() == null) ai.gameObject.AddComponent<CombatAudioHook>().Setup(ai);
            foreach (EnemyAI_Ranged ai in FindObjectsByType<EnemyAI_Ranged>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (ai.GetComponent<CombatAudioHook>() == null) ai.gameObject.AddComponent<CombatAudioHook>().Setup(ai);
            foreach (ArcherTower tower in FindObjectsByType<ArcherTower>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tower.GetComponent<CombatAudioHook>() == null) tower.gameObject.AddComponent<CombatAudioHook>().Setup(tower);
            foreach (TimerController timer in FindObjectsByType<TimerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (timer.GetComponent<TimerAudioHook>() == null) timer.gameObject.AddComponent<TimerAudioHook>().Setup(timer);
            foreach (HexLightUnlocker hex in FindObjectsByType<HexLightUnlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (hex.GetComponent<HexAudioHook>() == null) hex.gameObject.AddComponent<HexAudioHook>().Setup(hex);
            foreach (AlarmSystem alarm in FindObjectsByType<AlarmSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (alarm.GetComponent<AlarmAudioHook>() == null) alarm.gameObject.AddComponent<AlarmAudioHook>().Setup(alarm);
            foreach (HumanUnit unit in FindObjectsByType<HumanUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None)) AddFootsteps(unit.gameObject);
            foreach (Porter unit in FindObjectsByType<Porter>(FindObjectsInactive.Include, FindObjectsSortMode.None)) AddFootsteps(unit.gameObject);
            foreach (EnemyMovement unit in FindObjectsByType<EnemyMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None)) AddFootsteps(unit.gameObject);
            InitializeResourceSnapshot();
        }

        private static void AddFootsteps(GameObject target)
        {
            if (target.GetComponent<FootstepAudioHook>() == null) target.AddComponent<FootstepAudioHook>();
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
            if (amount > previous) PlayUI(GameAudioCue.ResourceGain, 0.72f, 0.96f, 1.04f, 0.07f);
            else if (amount < previous) PlayUI(GameAudioCue.ResourceSpend, 0.62f, 0.96f, 1.04f, 0.07f);
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
        }
    }
}
