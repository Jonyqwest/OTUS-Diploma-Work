using Unity.Netcode;
using UnityEngine;

namespace Multiplayer
{
    public interface IMultiplayerService
    {
        void CreateObject(NetworkObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = null);
        void CreatePlayer(NetworkObject prefab, ulong clientID = 0);
    }
}