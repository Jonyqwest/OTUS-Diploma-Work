public static class DeviceDetectUtility  
{
    public static bool VrIsAvailable()
    {
        bool isActive = UnityEngine.XR.XRSettings.isDeviceActive;
        // isActive = true;
        return isActive;
    }
}
