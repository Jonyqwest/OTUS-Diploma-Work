using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public class MultiplayerService : IMultiplayerService
    {
        private IClientIDService _clientIDService;

        public MultiplayerService(IClientIDService clientIDService)
        {
            _clientIDService = clientIDService;
        }

        public void CreatePlayer(NetworkObject prefab, ulong clientID = 0)
        {
            if (clientID == 0)
            {
                clientID = _clientIDService.GetNext();
            }

            prefab.SpawnAsPlayerObject(clientID);
        }

        public void CreateObject(NetworkObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            NetworkObject copy;

            if (parent != null)
                copy = GameObject.Instantiate(prefab, position, rotation, parent);
            else
                copy = GameObject.Instantiate(prefab, position, rotation);

            copy.Spawn();
        }
    }
}