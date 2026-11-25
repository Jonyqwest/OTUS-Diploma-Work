using UnityEngine;

public class MovementPivotController :MonoBehaviour
{
    [Header("References")]
 
    private Transform headPivot;
    
    private Transform autoLookPivot;
    private ThirdPersonSettings settings;

    public Vector3 targetForward;  
    private Quaternion targetRotation; // Заменяет targetForward
    private Vector3 _viewMovementForward; // устанавливается из вне
    

    [Header("Device")]
    private ICameraDevice _device; // теперь через контекст

    /// <summary>
    /// Инициализация через контекст камеры
    /// </summary>
    public void Bind(CameraContext ctx)
    {
        _device = ctx.Device as DesktopCameraDevice;
        if (_device == null)
        {
            Debug.LogWarning("[MovementPivotController] CameraContext.Device is not DesktopCameraDevice!");
        }
        headPivot = headPivot ?? ctx.HeadPivot;
        autoLookPivot = autoLookPivot ?? ctx.AutoLook;
        settings = settings ?? ctx.Settings.thirdPersonSettings;
    }

    public void SetDevice(ICameraDevice  device)
    {
        _device = device;
    }
   
    
    

    public void SetMovementForward(Vector3 movementForward)
    {
        _viewMovementForward = movementForward;
    }
    // public void SetTargetRotation(Quaternion OrientalRotation)
    // {
    //     targetRotation = OrientalRotation;
    // }
    
    
    public void Tick(float dt)
    {
        // if (movementPivot == null || headPivot == null || autoLookPivot == null)
        //     return;
        
        bool isMouseEnable = false;
        
        //устройство десктоп с мышкой? => смотрим на ее активность
        if (_device is DesktopCameraDevice)
            isMouseEnable = ((DesktopCameraDevice)_device).IsMouseEnable;
        
        targetForward = _viewMovementForward;
        targetForward.y = 0f;
        
        if (targetForward.sqrMagnitude < 0.0001f)
            return;
        
        targetForward.Normalize();
        
        Quaternion targetRot = Quaternion.LookRotation(targetForward, Vector3.up);
        
        float speed = isMouseEnable ? settings.followHeadSpeed : settings.followAutoLookSpeed;
        
        
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            speed * dt
        );
    }

}
