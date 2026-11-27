using UnityEngine;

[CreateAssetMenu(fileName = "BallCharacterConfig", menuName = "Character/BallCharacterConfig")]
public class BallCharacterConfig : ScriptableObject
{
    [Header("Movement")]
    public float maxWalkSpeed = 2f;            // скорость шага
    public float maxRunSpeed = 15f;             // скорость бега
    public float maxFlySpeed = 40f;             // скорость полета
    // public float maxStopSpeed = 0.5f;            // скорость покоя

    public float acceleration = 10f;            // сила ускорения
    public float moveWalke = 3f;                // скорость шага
    // public float moveWalkThreshold = 3f;        // предел Шага для смешивания
    // public float moveStanding = 0.1f;           // скорость покоя


    [Header("Jump / Boost")]
    public float jumpForce = 10f;                   // обычный прыжок
    public float boostMultiplier = 2f;             // усиление при Boost / рывке
    public float verticalBoostMultiplier = 1.5f;   // вертикальный взлёт с места

    [Header("Boost / Nitro")]
    public float boostCooldown = 1f;   // кулдаун между Boost
    public float boostDuration = 0.5f; // длительность расхода Boost

    [Header("VR / осмотр")]
    public float maxTurnSpeed = 30f;     // скорость поворота персонажа за взлдядом юзера
    // public float minTurnSpeed = 0.1f;   // скорость поворота персонажа за взлдядом юзера на большой скорости


    [Header("силы/ dist / тормоза")]
    // убираем лишнее торможение
    // public float groundCheckDistance = 1.0f;
    public float flightLift = 2f;
    public float flightForwardForce = 4f;

    [Header("Rigidbody Physics")]
    public float mass = 30f;
    public float massFly = 0.001f;

    public float walkeDrag = 1f;
    public float groundDrag = 0.1f;
    public float flightDrag = 0f;

    public float angularDrag = 0.05f;
    public float angularDragWalke = 100f;
}
