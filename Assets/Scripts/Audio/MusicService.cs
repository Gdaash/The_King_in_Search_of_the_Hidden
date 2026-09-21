using System.Collections.Generic;
using UnityEngine;

namespace Evolution_adventure.Scripts.UI
{
    public class MusicService : MonoBehaviour, IMusicService
    {
        [SerializeField] private List<AudioClip> _levelClips;
        
        public AudioClip GetRandomLevelClip()
        {
            return _levelClips[Random.Range(0, _levelClips.Count)];
        }
    }
}