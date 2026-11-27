using UnityEngine;

public class ForwardMove : MonoBehaviour
{
    [SerializeField] private float _speed;

    void Update()
    {
        transform.Translate(transform.forward * _speed);
    }
}
