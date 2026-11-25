using UnityEngine;

public class FirstPersonMode : ICameraMode
{

    [System.Serializable]
    public struct Settings
    {
        public float EyeHeight;          // Высота глаз над персонажем
        // public float AutoLookRelaxSpeed; // Скорость сброса угла в AutoLookPivot
    }

    public Settings settings = new Settings
    {
        EyeHeight = 0f,                // ← КЛЮЧ: высота глаз человека
        // AutoLookRelaxSpeed = 0.5f        // плавный сброс угла
    };

    private CameraContext ctx;
    // private Transform cachedAutoLookPivot;
    private Transform cachedHeadPivot;
    private ICameraDevice _device;
    // private Transform cachedCameraArm;
    private MovementPivotController movementPivotController;

    private Vector3 lastStableForward = Vector3.forward;

    private float SpeedRelaxAutoLook = 0.01f;



    [System.Obsolete]
    public void Enter(CameraContext context)
    {
        ctx = context;

        // cachedAutoLookPivot = ctx.AutoLook;
        cachedHeadPivot = ctx.HeadPivot;
        // cachedCameraArm = ctx.Arm;
        _device = ctx.Device;
        // movementPivotController = ctx.RigRoot.GetComponentInChildren<MovementPivotController>();
        movementPivotController = ctx.MovementPivot.GetComponent<MovementPivotController>();
        
        // Скрываем части тела текущего юнита, за которым следит камера
        ApplyViewMode(true);
    }

    [System.Obsolete]
    public void Exit() {
        // Показываем части тела текущего юнита
        ApplyViewMode(false);
    }

    /// <summary>
    /// Переключает видимость частей тела юнита (1-е / 3-е лицо).
    /// Сначала ищет IViewModeSwitchable (на цели или через RigBall.Owner),
    /// а при его отсутствии делает fallback на InvisibleObjectsFromFirstPerson.
    /// </summary>
    private void ApplyViewMode(bool isFirstPerson)
    {
        if (ctx.Target == null) return;

        var switchable = ctx.Target.GetComponentInParent<IViewModeSwitchable>();
        if (switchable == null)
        {
            var rigBall = ctx.Target.GetComponentInParent<RigBall>();
            if (rigBall != null && rigBall.Owner != null)
            {
                switchable = rigBall.Owner.GetComponentInChildren<IViewModeSwitchable>();
            }
        }

        if (switchable != null)
        {
            if (isFirstPerson) switchable.SetFirstPersonView();
            else switchable.SetThirdPersonView();
            return;
        }

        // Fallback для совместимости
        var legacy = ctx.Target.GetComponentInParent<InvisibleObjectsFromFirstPerson>();
        if (legacy != null)
        {
            if (isFirstPerson) legacy.SwitchFirstView();
            else legacy.SwitchThirdView();
        }
    }


    public Vector3 GetDesiredRigPosition(float dt)
    {
        // 🔑 ДОБАВЛЕНА ВЫСОТА ГЛАЗ!
        return ctx.Target?.position + Vector3.up * settings.EyeHeight ??
               Vector3.zero;
    }


    public void Tick(float dt)
    {
        if (!ctx.Target) return;

        ctx.RigRoot.position = ctx.Target.position;

        // movementPivotController.SetMovementForward(cachedHeadPivot.forward);
        var targetForward = _device.OrientalRotation * ctx.AutoLook.forward; 
        targetForward.y = 0;
        if (targetForward.sqrMagnitude < 0.0001f)
            targetForward = ctx.AutoLook.forward;
        else 
            targetForward = targetForward.normalized;
        movementPivotController.SetMovementForward(targetForward);
        movementPivotController?.Tick(dt);
    }
     
}



