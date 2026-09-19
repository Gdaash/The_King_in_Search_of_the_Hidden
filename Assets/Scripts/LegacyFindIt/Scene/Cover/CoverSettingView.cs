using Core.LevelManagement;
using DeskCat.FindIt.Scripts.Core.Main.System;
using DeskCat.FindIt.Scripts.Core.Main.Utility.Region;
using DeskCat.FindIt.Scripts.Core.Model;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using LevelManager = DeskCat.FindIt.Scripts.Core.Main.System.LevelManager;

namespace DeskCat.FindIt.Scripts.Scene.Cover
{
    public class CoverSettingView : MonoBehaviour
    {
        public GameObject MainPanel;
        public Button CloseBtn;

        public Slider MusicSlider;
        public Slider SoundFxSlider;

        [Header("MultiLanguages")] public Text LanguageTextValue;
        public Button PrevBtn;
        public Button NextBtn;

        public Button BackToTitleBtn;
        public Button ResumeBtn;
        public Button ClearBtn;
        
        public string TitleSceneName = "Cover";

        private ILevelChange _levelChange;
        
        [Inject]
        private void Construct(ILevelChange levelChange)
        {
            _levelChange = levelChange;
        }
        
        private void Start()
        {
            CloseBtn.onClick.AddListener(Resume);

            MusicSlider.onValueChanged.AddListener(GlobalSetting.ChangeMusicVolume);
            SoundFxSlider.onValueChanged.AddListener(GlobalSetting.ChangeSoundVolume);
            PrevBtn.onClick.AddListener(PrevLanguage);
            NextBtn.onClick.AddListener(NextLanguage);
            
            if (ClearBtn != null)
            {
                ClearBtn.onClick.AddListener(GlobalSetting.ClearConfig);
            }

            if (BackToTitleBtn != null)
            {
                BackToTitleBtn.onClick.AddListener(BackToTitle);
            }

            if (ResumeBtn != null)
            {
                ResumeBtn.onClick.AddListener(Resume);
            }

            GlobalSetting.LanguageChangeAction += LanguageChange;

            InitializeValue();
        }

        public void OpenPauseSetting()
        {
            MainPanel.SetActive(true);
            CameraView2D.instance?.SetStopCameraFunc(true);
            CameraView3D.instance?.SetStopCamera(true);
            LevelManager.LevelManagerInstance.SetHiddenObjClickable(false);
        }

        private void Resume()
        {
            MainPanel.SetActive(false);
            CameraView2D.instance?.SetStopCameraFunc(false);
            CameraView3D.instance?.SetStopCamera(false);
            LevelManager.LevelManagerInstance?.SetHiddenObjClickable(true);
        }

        private void BackToTitle()
        {
            _levelChange.LoadLevel(TitleSceneName);
        }

        private void OnDestroy()
        {
            GlobalSetting.LanguageChangeAction -= LanguageChange;
            MusicSlider.onValueChanged.RemoveAllListeners();
            SoundFxSlider.onValueChanged.RemoveAllListeners();
            PrevBtn.onClick.RemoveAllListeners();
            NextBtn.onClick.RemoveAllListeners();
            CloseBtn.onClick.RemoveAllListeners();
        }

        private void InitializeValue()
        {
            MusicSlider.value = GlobalSetting.MusicVolume;
            SoundFxSlider.value = GlobalSetting.SoundVolume;
            LanguageTextValue.text = GlobalSetting.CurrentLanguage;
        }

        private void LanguageChange(string data)
        {
            LanguageTextValue.text = data;
        }

        private void NextLanguage()
        {
            GlobalSetting.NextLanguage();
        }

        private void PrevLanguage()
        {
            GlobalSetting.PrevLanguage();
        }

    }
}