using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using DeskCat.FindIt.Scripts.Core.Main.System;

namespace DeskCat.FindIt.Scripts.Core.Model
{
    public static partial class GlobalSetting
    {
        public static float MusicVolume = 1;
        public static float SoundVolume = 1;
        public static string CurrentLanguage = "English";

        public static Action<float> MusicChangeAction;
        public static Action<float> SoundChangeAction;
        public static Action<string> LanguageChangeAction;

        private static int currentLanguageIndex;
        private static List<string> languageKeyList;
        public static FindItSetting DefaultFindItSetting;

        public static string ConfigFileName { get; set; } = "GlobalSettingConfig.sav";

        public static Dictionary<string, bool> LevelActiveDic = new Dictionary<string, bool>();

        public static void InitializeSetting()
        {
            if (languageKeyList != null) return;

            LoadConfig();

            if (languageKeyList == null)
            {
                MusicVolume = DefaultFindItSetting.DefaultBackgroundMusic;
                SoundVolume = DefaultFindItSetting.DefaultSoundFxMusic;
                currentLanguageIndex = DefaultFindItSetting.DefaultLanguagesIndex;
                languageKeyList = DefaultFindItSetting.LanguagesKey;
                CurrentLanguage = DefaultFindItSetting.LanguagesKey[currentLanguageIndex];

                SaveConfig();
            }
            
            MusicChangeAction?.Invoke(MusicVolume);
            SoundChangeAction?.Invoke(SoundVolume);
            LanguageChangeAction?.Invoke(CurrentLanguage);
        }

        public static void SaveConfig()
        {
            var config = new GlobalSettingConfig
            {
                MusicVolume = MusicVolume,
                SoundVolume = SoundVolume,
                CurrentLanguage = CurrentLanguage,
                CurrentLanguageIndex = currentLanguageIndex,
                LanguageKeyList = languageKeyList,
                LevelActiveList = ConvertDictionaryToList(LevelActiveDic)
            };
            JsonHelper.SaveToJson(config, ConfigFileName);
        }

        public static void LoadConfig()
        {
            var config = JsonHelper.LoadFromJson<GlobalSettingConfig>(ConfigFileName);
            MusicVolume = config.MusicVolume;
            SoundVolume = config.SoundVolume;
            CurrentLanguage = config.CurrentLanguage;
            currentLanguageIndex = config.CurrentLanguageIndex;
            languageKeyList = config.LanguageKeyList;
            LevelActiveDic = ConvertListToDictionary(config.LevelActiveList);
        }

        public static void ClearConfig()
        {
            string filePath = Path.Combine(Application.persistentDataPath, ConfigFileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (DefaultFindItSetting != null)
            {
                MusicVolume = DefaultFindItSetting.DefaultBackgroundMusic;
                SoundVolume = DefaultFindItSetting.DefaultSoundFxMusic;
                currentLanguageIndex = DefaultFindItSetting.DefaultLanguagesIndex;
                languageKeyList = DefaultFindItSetting.LanguagesKey;
                CurrentLanguage = DefaultFindItSetting.LanguagesKey[currentLanguageIndex];
            }
            LevelActiveDic.Clear();
        }

        public static void ChangeMusicVolume(float volume)
        {
            if (languageKeyList == null)
                InitializeSetting();

            MusicVolume = volume;
            MusicChangeAction?.Invoke(volume);
            SaveConfig();
        }

        public static void ChangeSoundVolume(float volume)
        {
            if (languageKeyList == null)
                InitializeSetting();

            SoundVolume = volume;
            SoundChangeAction?.Invoke(volume);
            SaveConfig();
        }

        public static void NextLanguage()
        {
            if (languageKeyList == null)
                InitializeSetting();

            currentLanguageIndex++;
            if (currentLanguageIndex >= languageKeyList.Count)
            {
                currentLanguageIndex = 0;
            }

            ChangeCurrentLanguage(languageKeyList[currentLanguageIndex]);
        }

        public static void PrevLanguage()
        {
            if (languageKeyList == null)
                InitializeSetting();

            currentLanguageIndex--;
            if (currentLanguageIndex < 0)
            {
                currentLanguageIndex = languageKeyList.Count - 1;
            }

            ChangeCurrentLanguage(languageKeyList[currentLanguageIndex]);
        }

        public static void ChangeCurrentLanguage(string language)
        {
            if (languageKeyList == null)
                InitializeSetting();

            CurrentLanguage = language;
            LanguageChangeAction?.Invoke(language);
            SaveConfig();
        }

        public static List<TModel> GetDefaultLanguageKey<TModel>() where TModel : MultiLanguageModel, new()
        {
            var multiLanguageTextModels = new List<TModel>();

            if (languageKeyList == null)
                InitializeSetting();

            foreach (var language in languageKeyList)
            {
                multiLanguageTextModels.Add(new TModel() { LanguageKey = language });
            }

            return multiLanguageTextModels;
        }

        private static List<LevelActiveEntry> ConvertDictionaryToList(Dictionary<string, bool> dictionary)
        {
            var list = new List<LevelActiveEntry>();
            foreach (var kvp in dictionary)
            {
                list.Add(new LevelActiveEntry { Key = kvp.Key, Value = kvp.Value });
            }

            return list;
        }

        private static Dictionary<string, bool> ConvertListToDictionary(List<LevelActiveEntry> list)
        {
            var dictionary = new Dictionary<string, bool>();
            if (list != null)
            {
                foreach (var entry in list)
                {
                    dictionary[entry.Key] = entry.Value;
                }
            }

            return dictionary;
        }
    }

}