using Core.LevelManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace DeskCat.FindIt.Scripts.Scene.LevelSelector
{
    public class LevelSelectorView : MonoBehaviour
    {
        public Button BackToTitleBtn;

        private ILevelChange _levelChange;
        
        [Inject]
        private void Construct(ILevelChange levelChange)
        {
            _levelChange = levelChange;
        }
        
        private void Start()
        {
            BackToTitleBtn.onClick.AddListener(BackToTitleFunc);
        }

        private void BackToTitleFunc()
        {
            _levelChange.LoadLevel("Cover");
        }
    }
}