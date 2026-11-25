using UnityEngine;

[CreateAssetMenu(fileName = "AutoLookSettings", menuName = "Character/AutoLookSettings")]
public class AutoLookSettings : ScriptableObject
{
    [Header("Скорость поворота ориентации за мышкой")]
    public float followHeadSpeed = 20f;
    [Header("Скорость поворота ориентации автослежения")]
    public float followAutoLookSpeed = 2f;
    [Header("Мышка неактивка, в режим слежения через:")]
    public float mouseIdleTimeout = 10.2f;


}
