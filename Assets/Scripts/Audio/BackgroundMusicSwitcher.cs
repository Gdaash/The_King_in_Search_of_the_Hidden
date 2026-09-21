using UnityEngine;
using Zenject;

namespace Evolution_adventure.Scripts
{
    /// <summary>
    /// Переключает музыку на уровне
    /// </summary>
    public class BackgroundMusicSwitcher : MonoBehaviour
    {
        [SerializeField] private AudioSource _music;
        
        private IMusicService _musicService;

        [Inject]
        private void Construct(IMusicService music)
        {
            _musicService = music;
        }
        
        // Используем Start вместо OnEnable, так как Zenject гарантированно 
        // заполнит зависимости к моменту вызова Start.
        private void Start()
        {
            if (_musicService == null)
            {
                Debug.LogError($"[BackgroundMusicSwitcher] MusicService не внедрен на объекте {gameObject.name}!");
                return;
            }

            if (_music == null)
            {
                Debug.LogError($"[BackgroundMusicSwitcher] AudioSource не назначен в инспекторе на объекте {gameObject.name}!");
                return;
            }

            PlayMusic();
        }

        private void PlayMusic()
        {
            _music.clip = _musicService.GetRandomLevelClip();
            _music.loop = true;
            _music.Play();
        }
    }
}