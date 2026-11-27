using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Multiplayer
{
    public class Test : MonoBehaviour
    {
        [Inject] private IMultiplayerFacade _facade;
        [Inject] private IInputService _inputService;
        [SerializeField] private NetworkObject _player;

        private void Start()
        {
            _inputService.HostConnectClicked += SetHost;
            _inputService.ClientConnectClicked += SetClient;
        }

        private void SetHost()
        {
            NetworkManager.Singleton.StartHost();

            _facade.CreatePlayer(_player);
        }

        private void SetClient()
        {
            NetworkManager.Singleton.StartClient();

            _facade.CreatePlayer(_player);
        }
    }
}