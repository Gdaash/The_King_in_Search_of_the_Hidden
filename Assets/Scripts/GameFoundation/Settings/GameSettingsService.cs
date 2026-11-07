using System;
using System.Linq;
using UnityEngine;

namespace GameFoundation.Settings
{
    public sealed class GameSettingsService : MonoBehaviour
    {
        public static GameSettingsService Instance { get; private set; }
        public event Action Changed;
        public float Master { get; private set; } = 1f;
        public float Music { get; private set; } = 1f;
        public float Effects { get; private set; } = 1f;
        public bool Fullscreen { get; private set; }
        public bool VSync { get; private set; } = true;
        private const string Prefix = "foundation.settings.";
        private void Awake() { if (Instance != null) { Destroy(this); return; } Instance = this; DontDestroyOnLoad(gameObject); Load(); }
        public void SetMaster(float value) { Master = Mathf.Clamp01(value); AudioListener.volume = Master; Store("master", Master); }
        public void SetMusic(float value) { Music = Mathf.Clamp01(value); Store("music", Music); }
        public void SetEffects(float value) { Effects = Mathf.Clamp01(value); Store("effects", Effects); }
        public void SetFullscreen(bool value)
        {
            Fullscreen = value;
            var resolution = SavedResolution;
            if (SupportsResolution(resolution)) ApplyResolution(resolution);
            else Screen.fullScreen = value;
            Store("fullscreen", value ? 1 : 0);
        }

        public void SetVSync(bool value) { VSync = value; QualitySettings.vSyncCount = value ? 1 : 0; Store("vsync", value ? 1 : 0); }

        public static Vector2Int[] AvailableResolutions() => Screen.resolutions
            .Select(mode => new Vector2Int(mode.width, mode.height))
            .Where(size => size.x > 0 && size.y > 0)
            .Distinct()
            .OrderByDescending(size => size.x).ThenByDescending(size => size.y)
            .ToArray();

        private static Vector2Int SavedResolution => new(
            PlayerPrefs.GetInt(Prefix + "resolution.width"), PlayerPrefs.GetInt(Prefix + "resolution.height"));

        private static bool SupportsResolution(Vector2Int size) => size.x > 0 && size.y > 0 &&
            Screen.resolutions.Any(mode => mode.width == size.x && mode.height == size.y);

        public bool SetResolution(Vector2Int size)
        {
            if (!SupportsResolution(size)) return false;
            ApplyResolution(size);
            PlayerPrefs.SetInt(Prefix + "resolution.width", size.x);
            Store("resolution.height", size.y);
            return true;
        }

        private void ApplyResolution(Vector2Int size) => Screen.SetResolution(size.x, size.y,
            Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

        private void Load() { SetMaster(PlayerPrefs.GetFloat(Prefix+"master",1)); SetMusic(PlayerPrefs.GetFloat(Prefix+"music",1)); SetEffects(PlayerPrefs.GetFloat(Prefix+"effects",1)); SetFullscreen(PlayerPrefs.GetInt(Prefix+"fullscreen",Screen.fullScreen?1:0)==1); SetVSync(PlayerPrefs.GetInt(Prefix+"vsync",1)==1); }
        private void Store(string key, float value) { PlayerPrefs.SetFloat(Prefix+key,value); PlayerPrefs.Save(); Changed?.Invoke(); }
        private void Store(string key, int value) { PlayerPrefs.SetInt(Prefix+key,value); PlayerPrefs.Save(); Changed?.Invoke(); }
    }
}
