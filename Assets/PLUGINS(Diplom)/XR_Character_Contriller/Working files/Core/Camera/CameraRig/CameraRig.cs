//только компоненты
using UnityEngine;

public class CameraRig : MonoBehaviour
{
    [Header("Main")]
    public Transform rigRoot;
    public Transform headPivot;

    [Header("Second")]
    public Transform MovementPivot;
    public Transform AutoLook;
    public Transform Arm;

    [Header("Cameras")]
    public Camera CameraMono;
    public Camera CameraVR;
    public Camera CameraStereoL;
    public Camera CameraStereoR;

    public void ApplyDevice(ICameraDevice device)
    {
        headPivot.localRotation = device.HeadRotation;
        headPivot.localPosition = device.HeadPosition;

        //SetStereo(device.IsStereo, device.IPD);
    }
}
