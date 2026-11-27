using UnityEngine;

[CreateAssetMenu(fileName = "FirstPersonSettings", menuName = "Character/FirstPersonSettings")]
public class FirstPersonSettings : ScriptableObject
{
    
    [Header("Высота глаз")]
    public float EyeHeight = 1.6f;          // Высота глаз над персонажем

    [Header("Скорость сброса угла")]
    public float AutoLookRelaxSpeed = 0.5f; // Скорость сброса угла в AutoLookPivot
}
