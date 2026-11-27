using UnityEngine;

public interface ICameraDevice
{
    //bool IsActive { get; }
    bool IsStereo { get; }

    Quaternion HeadRotation { get; }
    Vector3 HeadPosition { get;}
    
    Quaternion OrientalRotation { get; }
    

    float IPD { get; }

    void Enable();
    void Disable();
}
