using Core.LevelManagement;
using DeskCat.FindIt.Scripts.Core.Model;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace DeskCat.FindIt.Scripts.Scene.LevelSelector
{
    public class LevelButton : MonoBehaviour
    {
        public string LevelName;
        private Button button;
        public bool isActive = true;

        private ILevelChange _levelChange;
        
        [Inject]
        private void Construct(ILevelChange levelChange)
        {
            _levelChange = levelChange;
        }

        private void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => _levelChange.LoadLevel(LevelName));
            if (!GlobalSetting.LevelActiveDic.TryAdd(LevelName, isActive))
            {
                isActive = GlobalSetting.LevelActiveDic[LevelName];
            }
            SetLevelActive(isActive);
        }

        public void SetLevelActive(bool value)
        {
            isActive = value;
            button.interactable = isActive;
        }
    }
}