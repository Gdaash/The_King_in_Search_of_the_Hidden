using Core.LevelManagement;
using DeskCat.FindIt.Scripts.Core.Model;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace DeskCat.FindIt.Scripts.Scene.Cover
{
    public class CoverView : MonoBehaviour
    {
        public Button PlayBtn;
        public Button SettingBtn;
        public Button ExitBtn;
        public GameObject SettingPanel;

        public string LevelSelectorSceneName = "LevelSelector";

        private ILevelChange _levelChange;
        
        [Inject]
        private void Construct(ILevelChange levelChange)
        {
            _levelChange = levelChange;
        }
        
        private void Start()
        {
            PlayBtn.onClick.AddListener(PlayBtnFunction);
            ExitBtn.onClick.AddListener(ExitBtnFunction);
            SettingBtn.onClick.AddListener(SettingBtnFunction);
        }

        private void PlayBtnFunction()
        {
            _levelChange.LoadLevel(LevelSelectorSceneName);
        }

        private void SettingBtnFunction()
        {
            SettingPanel.SetActive(true);
        }

    	private void ExitBtnFunction()
    	{
        	Debug.Log("Игра закрывается..."); // Для проверки в редакторе
        	Application.Quit(); // Закрывает игру (в собранной версии)
    	}
    }
}