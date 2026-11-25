//using UnityEngine;
//using Core.Character.Controllers;

//public class ThirdPersonMode : ICameraMode
//{
//    [System.Serializable]
//    public struct Settings
//    {
//        public float Distance;
//        public float Height;
//        public float SmoothTime;
//        public float IdleMouseTimeout;
//        public float ReturnSpeed;
//        public bool debugShowOrientationTarget;
//    }

//    public Settings settings = new Settings
//    {
//        Distance = 5f,
//        Height = 2f,
//        SmoothTime = 0.12f,
//        IdleMouseTimeout = 0.3f,
//        ReturnSpeed = 5f,
//        debugShowOrientationTarget = true
//    };

//    private CameraContext ctx;
//    private Vector3 smoothVelocity = Vector3.zero;
//    private Transform cachedAutoLookPivot;
//    private Transform cachedHeadPivot;
//    private Transform cachedCameraArm;
//    private GameObject debugTargetSphere;

//    public void Enter(CameraContext context)
//    {
//        ctx = context;
//        cachedAutoLookPivot = ctx.AutoLook; // ← ваша рабочая структура
//        cachedHeadPivot = ctx.HeadPivot;
//        cachedCameraArm = ctx.Arm;

//        if (settings.debugShowOrientationTarget && ctx.RigRoot != null)
//        {
//            debugTargetSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//            debugTargetSphere.name = "OrientationTarget";
//            debugTargetSphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
//            debugTargetSphere.transform.SetParent(ctx.RigRoot, false);

//            // Минимальный материал для видимости
//            var mat = new Material(Shader.Find("Standard"));
//            mat.color = new Color(0f, 1f, 1f, 0.85f);
//            debugTargetSphere.GetComponent<Renderer>().material = mat;
//            //Destroy(debugTargetSphere.GetComponent<Collider>());
//        }
//    }

//    public void Exit()
//    {
//        if (debugTargetSphere != null) GameObject.Destroy(debugTargetSphere);
//    }

//    public Vector3 GetDesiredRigPosition(float dt)
//    {
//        return ctx.Target?.position + Vector3.up * settings.Height ?? Vector3.zero;
//    }

//    public void Tick(float dt)
//    {
//        if (ctx.Target == null || ctx.RigRoot == null) return;

//        // 1. RigRoot следует за персонажем
//        ctx.RigRoot.position = Vector3.SmoothDamp(
//            ctx.RigRoot.position,
//            GetDesiredRigPosition(dt),
//            ref smoothVelocity,
//            settings.SmoothTime,
//            Mathf.Infinity,
//            dt
//        );

//        // 2. CameraArm смещает камеру назад
//        if (cachedCameraArm != null)
//        {
//            cachedCameraArm.localPosition = new Vector3(0f, 0f, -settings.Distance);
//        }

//        // 3. 🔑 ВЫРАВНИВАЕМ СФЕРУ ПО ВЫСОТЕ С AutoLookPivot (ключевая правка!)
//        if (debugTargetSphere != null)
//        {
//            Vector3 forwardDir = ctx.Target.forward;
//            Vector3 targetPos = ctx.Target.position + forwardDir * 2.0f;

//            // 🔑 ГЛАВНОЕ: выравниваем по Y с AutoLookPivot чтобы вектор был строго горизонтальным
//            if (cachedAutoLookPivot != null)
//                targetPos.y = cachedAutoLookPivot.position.y;
//            else
//                targetPos.y = ctx.Target.position.y + settings.Height;

//            debugTargetSphere.transform.position = targetPos;
//        }

//        // 4. Автовозврат — ваш рабочий код без изменений!
//        HandleAutoLookReturn(dt);
//    }

//    private void HandleAutoLookReturn(float dt)
//    {
//        if (cachedAutoLookPivot == null || debugTargetSphere == null || !(ctx.Device is DesktopCameraDevice desktopDevice))
//            return;

//        bool isMouseIdle = (Time.time - desktopDevice.LastMouseActivityTime) > settings.IdleMouseTimeout;
//        if (!isMouseIdle) return;

//        // 1. Вектор к цели (горизонтальный в МИРОВЫХ координатах)
//        Vector3 toTarget = debugTargetSphere.transform.position - cachedAutoLookPivot.position;
//        toTarget.y = 0f;
//        if (toTarget.sqrMagnitude < 0.01f) return;

//        // 2. 🔑 ГЛАВНОЕ: поворачиваем ВОКРУГ МИРОВОЙ ВЕРТИКАЛИ (ось Y мира)
//        //    А НЕ относительно искажённого локального пространства родителя!
//        Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);

//        // 3. Применяем поворот
//        cachedAutoLookPivot.rotation = Quaternion.Slerp(
//            cachedAutoLookPivot.rotation,
//            targetRot,
//            settings.ReturnSpeed * dt
//        );

//        //// 4. 🔑 КРИТИЧЕСКИ ВАЖНО: сразу выравниваем вертикаль по миру
//        ////    Это убивает накопленный крен за один кадр!
//        //cachedAutoLookPivot.up = Vector3.up;
//    }
//}