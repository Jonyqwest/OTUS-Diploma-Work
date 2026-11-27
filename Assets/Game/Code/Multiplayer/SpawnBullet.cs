using Multiplayer;
using Unity.Netcode;
using UnityEngine;
using Zenject;

public class SpawnBullet : MonoBehaviour
{
    [SerializeField] private NetworkObject _bullet;
    [Inject] private IInputService _inputService;

    private void Start()
    {
        _inputService.SpawnBulletClicked += Spawn;
    }

    private void Spawn()
    {
        var bullet = Instantiate(_bullet, transform.position, transform.rotation);
        bullet.Spawn();
    }
}