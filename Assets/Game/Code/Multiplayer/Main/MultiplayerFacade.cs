using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Multiplayer
{
    public class MultiplayerFacade : IMultiplayerFacade
    {
        [Inject] private IMultiplayerService _multiplayerService;

        public void CreatePlayer(NetworkObject prefab, ulong clientID = 0) =>
            _multiplayerService.CreatePlayer(prefab, clientID);

        public void CreateObject(NetworkObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null) =>
            _multiplayerService.CreateObject(prefab, position, rotation, parent);
    }
}