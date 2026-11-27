using UnityEngine;

public class CameraSystem
{
    ICameraDevice currentDevice;
    ICameraMode currentMode;

    CameraRig rig;
    CamConfigurator currentSettings;

    float positionSmooth = 1f;

    public CameraSystem(CameraRig rig)
    {
        this.rig = rig; 
    }

    public void SetDevice(ICameraDevice device)
    {
        currentDevice?.Disable();
        currentDevice = device;
        currentDevice.Enable();
        
    }

    public void SetMode(ICameraMode mode, Transform target)
    {
        currentMode?.Exit();

        currentMode = mode;
        currentMode.Enter(new CameraContext
        {
            Target = target,
            RigRoot = rig.rigRoot,
            Device = currentDevice,
            Settings = currentSettings,

            MovementPivot = rig.MovementPivot,
            AutoLook = rig.AutoLook,
            HeadPivot = rig.headPivot,
            Arm = rig.Arm
        });
    }

    public void InitConfig(CamConfigurator settings)
    {
        if (settings != null)
            currentSettings = settings;
        BindingComponents();
    }

    public void Tick(float dt)
    {
        // 1) ввод устройства
        if (currentDevice is DesktopCameraDevice desktop)
            desktop.Tick(dt);

        // 2) желаемая позиция от режима
        if (currentMode != null)
        {
            //плавная смена вида через позицию рига
            Vector3 desired = currentMode.GetDesiredRigPosition(dt);
            rig.rigRoot.position = Vector3.Lerp(
                rig.rigRoot.position,
                desired,
                1f - Mathf.Exp(-positionSmooth * dt)
            );
        }

        // 3) применяем поворот головы
        currentMode?.Tick(dt);
        rig.ApplyDevice(currentDevice);
    }

    private void BindingComponents()
    {
        rig.MovementPivot.GetComponent<MovementPivotController>()
            .Bind(
            new CameraContext
            {
                MovementPivot = rig.MovementPivot,
                AutoLook = rig.AutoLook,
                HeadPivot = rig.headPivot,
                Settings = currentSettings
            }
      );
    }
}
