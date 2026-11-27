using UnityEngine;
using UnityEngine.XR;

public class OpenXRCameraDevice : ICameraDevice
{
    public bool IsStereo => true;

    public Quaternion HeadRotation { get; private set; }
     
    public Quaternion OrientalRotation
    {
        get
        {
            if (!isEnabled || xrHead == null)
                return Quaternion.identity;
            // Получаем текущую ориентацию шлема
            Quaternion headRotation = xrHead.localRotation;
        
            // Получаем ориентации контроллеров (можно использовать deviceRotation или gripRotation)
            Quaternion leftControllerRot = GetControllerRotation(XRNode.LeftHand);
            Quaternion rightControllerRot = GetControllerRotation(XRNode.RightHand);
        
            // Усредняем контроллеры (руки обычно смотрят вперед)
            Quaternion controllerAvgRot = Quaternion.Slerp(leftControllerRot, rightControllerRot, 0.5f);
        
            // Смешиваем голову и контроллеры согласно настраиваемому коэффициенту
            // Этот коэффициент можно сделать ползунком в настройках VR
            float blendFactor = 0.2f; // 0 = только контроллеры, 1 = только голова (пример)
            return Quaternion.Slerp(controllerAvgRot, headRotation, blendFactor);
        }
    }
    public Vector3 HeadPosition { get; private set; }

    public float IPD { get; private set; }

    private Transform xrHead; // XR Camera transform
    private bool isEnabled;

    public OpenXRCameraDevice(Transform xrHead)
    {
        this.xrHead = xrHead;
    }

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
    }
    private Quaternion GetControllerRotation(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return Quaternion.identity;
        
        // Пробуем получить device rotation (полную позу устройства)
        if (device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            return rot;
    
        // Альтернатива: grip rotation
        // if (device.TryGetFeatureValue(CommonUsages.gripRotation, out Quaternion gripRot))
        //     return gripRot;
    
        return Quaternion.identity;
    }
    public void Tick(float dt)
    {
        if (!isEnabled || xrHead == null)
            return;

        // XR head pose (local относительно XR Origin)
        HeadPosition = xrHead.localPosition;
        HeadRotation = xrHead.localRotation;

        // IPD
        if (XRSettings.enabled)
            IPD = XRSettings.eyeTextureWidth > 0 ? XRSettings.eyeTextureWidth : IPD;

        // правильнее через InputDevices:
        TryUpdateIPD();
    }

    private void TryUpdateIPD()
    {
        var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (!device.isValid) return;

        if (device.TryGetFeatureValue(CommonUsages.leftEyePosition, out Vector3 leftEye) &&
            device.TryGetFeatureValue(CommonUsages.rightEyePosition, out Vector3 rightEye))
        {
            IPD = Vector3.Distance(leftEye, rightEye);
        }
    }
}
