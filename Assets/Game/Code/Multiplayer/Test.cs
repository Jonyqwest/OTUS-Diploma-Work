using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Multiplayer
{
    public class Test : MonoBehaviour
    {
        [Inject] private IMultiplayerFacade _facade;
        [SerializeField] private NetworkObject _player;

        private void Start()
        {
            MyInput myInput = new MyInput();
            myInput.Enable();
            myInput.Multiplayer.Host.performed += SetHost;
            myInput.Multiplayer.Client.performed += SetClient;
        }

        private void SetClient(InputAction.CallbackContext context)
        {
            NetworkManager.Singleton.StartClient();

            _facade.CreatePlayer(_player);
        }

        private void SetHost(InputAction.CallbackContext context)
        {
            NetworkManager.Singleton.StartHost();

            _facade.CreatePlayer(_player);
        }
    }
}