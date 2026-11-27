using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Multiplayer
{
    public class MultiplayerInstaller : MonoInstaller
    {
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private NetworkObject _playerPrefab;

        public override void InstallBindings()
        {
            Container.Bind<NetworkManager>().FromInstance(_networkManager).AsSingle();

            Container.Bind<NetworkObject>().WithId(KnownValues.ZenjectIds.Player).FromInstance(_playerPrefab).AsSingle();

            Container.Bind<IMultiplayerFacade>().To<MultiplayerFacade>().AsSingle();

            Container.BindInterfacesAndSelfTo<MultiplayerConnectService>().AsSingle();
            Container.BindInterfacesAndSelfTo<InputService>().AsSingle();
        }
    }
}