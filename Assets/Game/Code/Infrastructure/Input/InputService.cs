using System;
using Zenject;

namespace Multiplayer
{
    public class InputService : IInputService, IInitializable, IDisposable 
    {
        public event Action HostConnectClicked;
        public event Action SpawnBulletClicked;
        public event Action ClientConnectClicked;
        private MyInput _myInput;

        public void Initialize()
        {
            _myInput = new MyInput();
            _myInput.Enable();

            _myInput.Multiplayer.Host.performed += (_) => HostConnectClicked?.Invoke();
            _myInput.Multiplayer.Client.performed += (_) => ClientConnectClicked?.Invoke();
            _myInput.Multiplayer.SpawnBullet.performed += (_) => SpawnBulletClicked?.Invoke();
        }

        public void Dispose()
        {
            _myInput.Dispose();
            _myInput.Disable();
        }
    }
}