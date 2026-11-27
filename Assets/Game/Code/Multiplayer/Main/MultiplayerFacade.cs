using Unity.Netcode;
using Zenject;

namespace Multiplayer
{
    public class MultiplayerFacade : IMultiplayerFacade
    {
        [Inject] private NetworkManager _networkManager;

        public void StartClient()
        {
            _networkManager.StartClient();
        }

        public void StartHost()
        {
            _networkManager.StartHost();
        }
    }
}