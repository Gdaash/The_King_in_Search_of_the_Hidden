using GUICore.Fading;
using UnityEngine;
using Zenject;
using UnityEngine.SceneManagement;

namespace Core.LevelManagement
{
    /// <summary>
    /// Менеджер уровней, отвечает за машину состояний загрузки
    /// </summary>
    public class LevelManager : ILevelChange
    {
        private bool        _isProcessing;
        private string      _currentLevelName;
        
        private IFading     _fading;
        private StateLoad   _currentState;
        
        public LevelManager([Inject(Id = "FadingScreen")]IFading  fading)
        {
            _isProcessing   = false;
            _currentState   = StateLoad.None;
            _fading         = fading;
            
            _fading.OnComplete += FadingComplete;
        }

        /// <summary>
        /// UniTask пока не используем, загружаем уровень синхронно
        /// </summary>
        /// <param name="levelName"></param>
        public void LoadLevel(string levelName)
        {
            if(_isProcessing) return;

            _isProcessing = true;
            _currentLevelName = levelName;

            _fading.In();
            _currentState = StateLoad.Waiting;
        }

        private void FadingComplete()
        {
            if (!_isProcessing) return;
            
            switch (_currentState)
            {
                case StateLoad.Waiting:
                    AsyncOperation operation = SceneManager.LoadSceneAsync(_currentLevelName);
                    operation.completed += OnCompletedLoadLevel;
                    break;
                case StateLoad.Loaded:
                    _isProcessing = false;
                    _currentState = StateLoad.None;
                    break;
                default: break;
            }
        }

        private void OnCompletedLoadLevel(AsyncOperation obj)
        {
            _currentState = StateLoad.Loaded;
            _fading.Out();
        }
    }
    
    enum  StateLoad
    {
        None,
        Waiting,
        Loaded
    }
}