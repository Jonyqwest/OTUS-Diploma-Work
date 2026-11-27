using System;

namespace Multiplayer
{
    public interface IInputService
    {
        event Action ClientConnectClicked;
        event Action HostConnectClicked;
        event Action SpawnBulletClicked;
    }
}