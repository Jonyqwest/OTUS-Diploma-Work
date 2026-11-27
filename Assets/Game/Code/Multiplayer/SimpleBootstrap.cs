using UnityEngine;
using Zenject;

namespace Multiplayer
{
    public class SimpleBootstrap : MonoBehaviour
    {
        [Inject] private IMultiplayerFacade _facade;
        [Inject] private IInputService _inputService;

        private void Start()
        {
            _inputService.HostConnectClicked += StartHost;
            _inputService.ClientConnectClicked += StartClient;
        }

        private void StartHost()
        {
            _facade.StartHost();
        }

        private void StartClient()
        {
            _facade.StartClient();
        }

        private void OnDestroy()
        {
            _inputService.HostConnectClicked -= StartHost;
            _inputService.ClientConnectClicked -= StartClient;
        }
    }
}