using Achievements;
using UnityEngine;
using Zenject;

namespace Steamworks
{
    /// <summary>
    /// Установщик зависимостей Steam
    /// </summary>
    public class SteamInstaller :MonoInstaller
    {
        [SerializeField] private SteamManager _steamManager;
        
        public override void InstallBindings()
        {
            Container.Bind<SteamManager>()
                .FromInstance(_steamManager)
                .AsSingle()
                .NonLazy();

            Container.Bind<IAchievementsCloud>()
                .FromInstance(_steamManager)
                .AsSingle();
        }
    }
}