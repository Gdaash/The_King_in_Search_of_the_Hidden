using System;
using System.Collections.Generic;

namespace DeskCat.FindIt.Scripts.Core.Model
{
    [Serializable]
    public class GlobalSettingConfig
    {
        public float MusicVolume;
        public float SoundVolume;
        public string CurrentLanguage;
        public int CurrentLanguageIndex;
        public List<string> LanguageKeyList;
        public List<LevelActiveEntry> LevelActiveList;
    }
}