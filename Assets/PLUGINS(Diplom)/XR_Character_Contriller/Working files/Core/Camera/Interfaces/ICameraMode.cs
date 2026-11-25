using UnityEngine;

public interface ICameraMode
{
    void Enter(CameraContext ctx);
    void Exit();

    Vector3 GetDesiredRigPosition(float dt);

    void Tick(float dt);
}