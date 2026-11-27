using Unity.Netcode;
using Zenject;

namespace Multiplayer
{
    public class MultiplayerInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<NetworkManager>().FromInstance(NetworkManager.Singleton).AsSingle();

            Container.Bind<IMultiplayerFacade>().To<MultiplayerFacade>().AsSingle();
            Container.Bind<IClientIDService>().To<ClientIDService>().AsSingle();
            Container.Bind<IMultiplayerService>().To<MultiplayerService>().AsSingle();
        }
    }
}