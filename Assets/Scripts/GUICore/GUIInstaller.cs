using GUICore.Fading;
using UnityEngine;
using Zenject;

namespace GUICore
{
    /// <summary>
    /// Устанавливает GUI
    /// </summary>
    public class GUIInstaller : MonoInstaller
    {
        [SerializeField] private FadingScreen _fadingScreen;

        public override void InstallBindings()
        {
            Container.Bind<IFading>()
                .WithId("FadingScreen")
                .FromInstance(_fadingScreen)
                .AsSingle();
        }
    }
}