using UnityEngine;

[CreateAssetMenu(fileName = "ThirdPersonSettings", menuName = "Character/ThirdPersonSettings")]
public class ThirdPersonSettings : ScriptableObject
{
    
    [Header("Как далеко камера")]
    public float Distance = 7f;
    [Header("Как высако камера")]
    public float Height = 2f;
    [Header("Клавность камеры")]
    public float SmoothTime = 1.02f;

    // [Header("Задержка мыши")]
    // public float IdleMouseTimeout = 1.4f;
    [Header("Скорость восстановления")]
    public float ReturnSpeed = .8f;

    [Header("Показать визуально ориентацию")]
    public bool debugShowOrientationTargetShow = false;


    [Header("Скорость поворота ориентации за мышкой")]
    public float followHeadSpeed = 150f;
    [Header("Скорость поворота ориентации автослежения")]
    public float followAutoLookSpeed = 1.8f;

    [Header("Stability")]
    public float MinAngleToRotate = 0.2f; //чтобы не дрожало
    public float MinDistanceToUseTarget = 0.25f;

}
