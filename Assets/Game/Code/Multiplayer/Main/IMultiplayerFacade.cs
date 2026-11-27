using Unity.Netcode;

namespace Multiplayer
{
    public interface IMultiplayerFacade
    {
        void CreatePlayer(NetworkObject prefab, ulong clientID = 0);
    }
}