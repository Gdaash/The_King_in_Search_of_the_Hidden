using Zenject;

namespace Core.LevelManagement
{
    public class LevelManagerInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<LevelManager>().AsSingle().NonLazy();
        }
    }
}