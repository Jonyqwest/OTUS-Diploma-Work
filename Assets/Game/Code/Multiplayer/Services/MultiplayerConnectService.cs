using System;
using Unity.Netcode;
using UnityEngine;
using Zenject;

namespace Multiplayer
{
    public class MultiplayerConnectService : IInitializable, IDisposable
    {
        private NetworkObject _playerPrefab;
        private NetworkManager _manager;

        public MultiplayerConnectService(NetworkManager manager, [Inject(Id = KnownValues.ZenjectIds.Player)] NetworkObject playerPrefab)
        {
            _manager = manager;
            _playerPrefab = playerPrefab;
        }

        public void Initialize()
        {
            _manager.OnClientConnectedCallback += ClientConnect;
        }

        private void ClientConnect(ulong id)
        {
            CreatePlayer(id);
        }

        private void CreatePlayer(ulong clientID)
        {
            if (!_manager.IsServer) return;

            var clone = GameObject.Instantiate(_playerPrefab);
            clone.SpawnAsPlayerObject(clientID);
        }

        public void Dispose()
        {
            _manager.OnClientConnectedCallback -= ClientConnect;
        }
    }
}