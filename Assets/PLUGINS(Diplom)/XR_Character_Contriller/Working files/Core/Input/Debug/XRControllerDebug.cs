using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

// Явное использование XR InputDevice
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDevices = UnityEngine.XR.InputDevices;

public class XRControllerDebug : MonoBehaviour
{
    // Настройки
    [SerializeField] private Vector2 screenPosition = new Vector2(30, 30);
    [SerializeField] private int fontSize = 16;
    [SerializeField] private Color textColor = Color.green;
    
    private GUIStyle _style;
    private string _debugInfo = "";
    
    private void Update()
    {
        RefreshDevices();
    }
    
    private void RefreshDevices()
    {
        var sb = new System.Text.StringBuilder();
        
        // Получить все XR устройства
        List<XRInputDevice> devices = new List<XRInputDevice>();
        XRInputDevices.GetDevices(devices);
        
        sb.AppendLine("=== XR CONTROLLERS DEBUG ===");
        sb.AppendLine($"Devices: {devices.Count}");
        
        // Найти устройства
        XRInputDevice leftHand = default;
        XRInputDevice rightHand = default;
        
        foreach (var d in devices)
        {
            if (d.role == InputDeviceRole.LeftHanded)
                leftHand = d;
            else if (d.role == InputDeviceRole.RightHanded)
                rightHand = d;
        }
        
        // Левый контроллер
        sb.AppendLine("\n=== LEFT HAND ===");
        if (leftHand.isValid)
        {
            TestDevice(leftHand, sb);
        }
        else
        {
            sb.AppendLine("Not found");
        }
        
        // Правый контроллер
        sb.AppendLine("\n=== RIGHT HAND ===");
        if (rightHand.isValid)
        {
            TestDevice(rightHand, sb);
        }
        else
        {
            sb.AppendLine("Not found");
        }
        
        // Gamepad (Input System) - используем UnityEngine.InputSystem.Gamepad
        sb.AppendLine("\n=== GAMEPAD (InputSystem) ===");
        var gamepad = UnityEngine.InputSystem.Gamepad.current;
        if (gamepad != null)
        {
            sb.AppendLine($"Name: {gamepad.name}");
            sb.AppendLine($"Left Stick: {gamepad.leftStick.ReadValue()}");
            sb.AppendLine($"Right Stick: {gamepad.rightStick.ReadValue()}");
            sb.AppendLine($"Trigger L: {gamepad.leftTrigger.ReadValue():F2}");
            sb.AppendLine($"Trigger R: {gamepad.rightTrigger.ReadValue():F2}");
        }
        else
        {
            sb.AppendLine("null");
        }
        
        _debugInfo = sb.ToString();
    }
    
    private void TestDevice(XRInputDevice device, System.Text.StringBuilder sb)
    {
        sb.AppendLine($"Name: {device.name}");
        sb.AppendLine($"Valid: {device.isValid}");
        
        // Vector2 - используем CommonUsages из XR
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 p2a))
            sb.AppendLine($"primary2DAxis: {p2a}");
        
        if (device.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 s2a))
            sb.AppendLine($"secondary2DAxis: {s2a}");
        
        // Buttons
        sb.AppendLine("Buttons:");
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger))
            sb.AppendLine($"  trigger: {trigger}");
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip))
            sb.AppendLine($"  grip: {grip}");
        if (device.TryGetFeatureValue(CommonUsages.menuButton, out bool menu))
            sb.AppendLine($"  menu: {menu}");
        
        // Float values
        if (device.TryGetFeatureValue(CommonUsages.trigger, out float trigVal))
            sb.AppendLine($"  trigger(float): {trigVal:F2}");
        if (device.TryGetFeatureValue(CommonUsages.grip, out float gripVal))
            sb.AppendLine($"  grip(float): {gripVal:F2}");
    }
    
    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = fontSize;
            _style.normal.textColor = textColor;
        }
        
        // Фон
        GUI.color = new Color(0, 0, 0, 0.8f);
        GUI.DrawTexture(new Rect(screenPosition.x - 10, screenPosition.y - 10, 1000, 1000), Texture2D.whiteTexture);
        GUI.color = Color.white;
        
        GUI.Label(new Rect(screenPosition.x, screenPosition.y, 980, 980), _debugInfo, _style);
    }
}