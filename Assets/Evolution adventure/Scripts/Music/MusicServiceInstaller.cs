using Evolution_adventure.Scripts.UI;
using UnityEngine;
using Zenject;

namespace Evolution_adventure.Scripts
{
    public class MusicServiceInstaller : MonoInstaller
    {
        [SerializeField] private MusicService _musicService;
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<MusicService>().FromInstance(_musicService).AsSingle();
        }
    }
}