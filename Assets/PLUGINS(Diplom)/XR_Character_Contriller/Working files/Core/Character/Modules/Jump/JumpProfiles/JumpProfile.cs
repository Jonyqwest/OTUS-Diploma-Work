using UnityEngine;


[CreateAssetMenu(menuName = "Character/Jump Profile")]
public class JumpProfile : ScriptableObject
{
    [Header("Type")]

    public JumpType type;

    [Header("Up Velocity")]
    public AnimationCurve upCurve;

    [Header("Shape")]
    public float height = 1f;
    public float minHeight = 0.2f;
    public float duration = 1f;
    public float minDuration = 0.2f;
    public AnimationCurve curve;

    [Header("Limits")]
    public float softMaxUpVelocity = 12f; // мягкий лимит, не жесткий clamp

    // [Header("Physics")]
    // public float gravityMultiplier = 2.2f;

    [Header("Air Control")]
    [Range(0f, 1f)]
    public float airControl = 0f; // 0 = нет доруливания
    public Vector3 lockedDirection; //фиксируем направление прыжка

    [Tooltip("Небольшой толчок вперёд при прыжке с места")]
    public float idleForwardPush = 0.2f;
}


public enum JumpType
{
    JumpUp,          // нет контактов
    JumpRun,         // допустимая поверхность
    JumpObstacle     // отскок стена / потолок
}