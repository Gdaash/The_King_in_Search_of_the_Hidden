using Unity.VisualScripting;
using UnityEngine;
using Zenject;

namespace Evolution_adventure.Scripts
{
    /// <summary>
    /// Переключает музыку на уровне
    /// </summary>
    public class BackgroundMusicSwitcher :  MonoBehaviour
    {
        [SerializeField] private AudioSource _music;
        
        private IMusicService _musicService;

        [Inject]
        private void Construct(IMusicService music)
        {
            _musicService = music;
        }
        
        private void OnEnable()
        {
            _music.clip = _musicService.GetRandomLevelClip();
            _music.Play();
            _music.loop = true;
        }
    }
}