using DeskCat.FindIt.Scripts.Core.Model;
using UnityEngine;

namespace DeskCat.FindIt.Scripts.Core.Main.System
{
    public class DefaultSettingView : MonoBehaviour
    {
        public FindItSetting DefaultFindItSetting;
        public string ConfigFileName = "GlobalSettingConfig.sav";

        private void Start()
        {
            GlobalSetting.DefaultFindItSetting = DefaultFindItSetting;
            GlobalSetting.ConfigFileName = ConfigFileName;
            GlobalSetting.InitializeSetting();
        }
    }
}