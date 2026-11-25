using UnityEngine;
using Core.Character.Controllers;

public class ThirdPersonMode : ICameraMode
{
    private ThirdPersonSettings settings;

    private CameraContext ctx;
    private Vector3 smoothVelocity = Vector3.zero;
    private Transform cachedAutoLookPivot;
    private Transform cachedHeadPivot;
    private Transform cachedCameraArm;
    private GameObject debugTargetSphere;
    private MovementPivotController movementPivotController;
    private ICameraDevice _device;
    private Vector3 lastStableForward = Vector3.forward;

    public void Enter(CameraContext context)
    {
        

        ctx = context;

        settings = ctx.Settings.thirdPersonSettings;
        cachedAutoLookPivot = ctx.AutoLook;
        cachedHeadPivot = ctx.HeadPivot;
        cachedCameraArm = ctx.Arm;
        _device = ctx.Device;
        movementPivotController = ctx.RigRoot.GetComponentInChildren<MovementPivotController>();


        if (cachedCameraArm != null)
            cachedCameraArm.localPosition = new Vector3(0f, 0f, -settings.Distance);


        if (ctx.RigRoot != null)
        {
            if (settings.debugShowOrientationTargetShow)
            {
                debugTargetSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugTargetSphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            }
            else
            {
                debugTargetSphere = new GameObject();
            }
            debugTargetSphere.name = "OrientationTarget";
            debugTargetSphere.transform.SetParent(ctx.RigRoot, false);

        }
    }

    public void Exit()
    {
        if (cachedCameraArm)
            cachedCameraArm.localPosition = new Vector3(0, 0, 0);

        if (debugTargetSphere)
            GameObject.Destroy(debugTargetSphere);
    }

    public Vector3 GetDesiredRigPosition(float dt)
    {
        return ctx.Target?.position + Vector3.up * settings.Height ?? Vector3.zero;
    }

    public void Tick(float dt)
    {
        if (ctx.Target is null || ctx.RigRoot is null)
            return;

        // 1. RigRoot следует за персонажем
        ctx.RigRoot.position = Vector3.SmoothDamp(
            ctx.RigRoot.position,
            GetDesiredRigPosition(dt),
            ref smoothVelocity,
            settings.SmoothTime,
            Mathf.Infinity,
            dt
        );

        
        // 3. Debug target sphere
        if (debugTargetSphere)
        {

            Vector3 forwardDir = ctx.Target.forward;
            Vector3 targetPos = ctx.Target.position + forwardDir * 2.0f;

            if (cachedAutoLookPivot)
                targetPos.y = cachedAutoLookPivot.position.y;
            else
                targetPos.y = ctx.Target.position.y + settings.Height;

            debugTargetSphere.transform.position = targetPos;
        }

        // 4. Автовозврат
        HandleAutoLookReturn(dt);
        movementPivotController?.Tick(dt);
    }

    private void HandleAutoLookReturn(float dt)
    {
        if (cachedAutoLookPivot is null ||
            cachedHeadPivot is null ||
            debugTargetSphere is null ||
            movementPivotController is null)
            return;

        if (!(ctx.Device is DesktopCameraDevice desktopDevice))
            return;

        // bool isMouseIdle = (Time.time - desktopDevice.LastMouseActivityTime_) > settings.IdleMouseTimeout;
        if (desktopDevice.IsMouseEnable)
        {
            // var targetForward = _device.OrientalRotation * Vector3.forward; 
            var targetForward = _device.OrientalRotation * ctx.AutoLook.forward;
            targetForward.y = 0;
            if (targetForward.sqrMagnitude < 0.0001f)
                targetForward = ctx.AutoLook.forward;
            else 
                targetForward = targetForward.normalized;
            movementPivotController.SetMovementForward(targetForward);
           
            return;
        }

        // 1) Берём forward направления движения (из сферы)
        Vector3 movementForward = GetForwardFromDebugSphere(
            cachedAutoLookPivot.position,
            debugTargetSphere.transform.position
        );

        movementPivotController.SetMovementForward(movementForward);
        

        // 2) Берём yaw головы ТОЛЬКО ЛОКАЛЬНЫЙ (ключевой фикс!)
        float headLocalYaw = cachedHeadPivot.localEulerAngles.y;
        Quaternion headYawOnly = Quaternion.Euler(0f, headLocalYaw, 0f);

        // 3) Корректируем цель: AutoLookPivot должен так повернуться,
        // чтобы (AutoLookPivot * HeadPivot) смотрел в movementForward
        Vector3 correctedTargetForward = Quaternion.Inverse(headYawOnly) * movementForward;
        correctedTargetForward.y = 0f;

        if (correctedTargetForward.sqrMagnitude < 0.0001f)
            return;

        correctedTargetForward.Normalize();

        // 4) Текущий forward AutoLookPivot
        Vector3 currentForward = cachedAutoLookPivot.forward;
        currentForward.y = 0f;

        if (currentForward.sqrMagnitude < 0.0001f)
            return;

        currentForward.Normalize();

        // 5) Deadzone по углу чтобы не было вечной дрожи
        float angle = Vector3.Angle(currentForward, correctedTargetForward);
        if (angle < settings.MinAngleToRotate)
            return;

        // 6) Плавный поворот
        Vector3 newForward = Vector3.RotateTowards(
            currentForward,
            correctedTargetForward,
            settings.ReturnSpeed * dt,
            0f
        );

        cachedAutoLookPivot.rotation = Quaternion.LookRotation(newForward, Vector3.up);
    }

    private Vector3 GetForwardFromDebugSphere(Vector3 pivotWorldPos, Vector3 debugSphereWorldPos)
    {
        Vector3 dir = debugSphereWorldPos - pivotWorldPos;
        dir.y = 0f;

        if (dir.sqrMagnitude < settings.MinDistanceToUseTarget * settings.MinDistanceToUseTarget)
        {
            return lastStableForward;
        }
        lastStableForward = dir.normalized;
        return lastStableForward;
    }
}
