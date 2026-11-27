

using UnityEngine;

public enum ContactType
{
    None,           // нет контактов
    Ground,         // допустимая поверхность
    Obstacle     // стена / потолок
}

public struct GroundState
{
    public bool isContacted;

    public bool isGrounded;
    public bool wasGrounded;

    public bool justLanded;
    public bool justLeftGround;

    public ContactType contactType;

    public Vector3 normal;
    public float slopeAngle;

    public bool frontDetected;   // Front коллайдер чувствует землю
    public bool leftDetected;     // Left коллайдер чувствует землю
    public bool rightDetected;   // Right коллайдер чувствует землю
}

//using UnityEngine;

//public struct GroundState
//{
//    public bool isGrounded;
//    public bool wasGrounded;
//    public bool justLanded;
//    public bool justLeftGround;

//    public Vector3 normal;

//    public float distance;
//}