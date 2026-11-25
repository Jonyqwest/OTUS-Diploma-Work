using UnityEngine;

public struct CameraContext
{
    public Transform Target;
    public Transform RigRoot;
    public ICameraDevice Device;
    public ICameraMode Mode;
    public CamConfigurator Settings;

    public Transform MovementPivot;
    public Transform AutoLook;
    public Transform HeadPivot;
    public Transform Arm;

    
}
