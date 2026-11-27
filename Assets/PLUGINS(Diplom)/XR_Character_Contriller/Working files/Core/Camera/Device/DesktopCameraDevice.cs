using UnityEngine;

public class DesktopCameraDevice : ICameraDevice
{
    public bool IsStereo => false;
    public float IPD => 0f;
    public bool IsMouseEnable { get; private set;}

    public Quaternion HeadRotation { get; private set; }
    public Quaternion OrientalRotation => HeadRotation;
    public Vector3 HeadPosition => Vector3.zero;

    // 🔑 Для устройств с мышкой: состояние активности мыши (внутреннее, не в интерфейсе)
    public bool IsMouseActiveThisFrame { get; private set; }
    public float LastMouseActivityTime { get; private set; }

    float yaw;
    float pitch;
    const float MouseSens = 2.5f;
    const float MouseDisableTime = 2f;

    public void Enable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Disable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Tick(float dt)
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Отслеживаем активность мыши
        IsMouseActiveThisFrame = Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f;
        if (IsMouseActiveThisFrame)
            LastMouseActivityTime = Time.time;
        IsMouseEnable = (Time.time - LastMouseActivityTime) < MouseDisableTime;
        
        // Стандартная логика поворота
        yaw += mouseX * MouseSens;
        pitch -= mouseY * MouseSens;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        HeadRotation = Quaternion.Euler(pitch, yaw, 0);
    }
}