using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnBullet : MonoBehaviour
{
    [SerializeField] private NetworkObject _bullet;

    private void Start()
    {
        MyInput myInput = new MyInput();
        myInput.Enable();
        myInput.Multiplayer.SpawnBullet.performed += Spawn;
    }

    private void Spawn(InputAction.CallbackContext context)
    {
        var bullet = Instantiate(_bullet, transform.position, transform.rotation);
        //bullet.Spawn();
    }
}