using UnityEngine;

public enum JumpPhase
{
    Grounded,       // стоит на земле, можно начать прыжок

    RisingMin,      // гарантированный стартовый подъём (min jump)
    Rising,         // подъём с удержанием (по кривой)

    Falling         // падение (гравитация усилена)
}

public struct JumpState
{
    public JumpPhase phase;
    public float time;
    public float startY;
    public Vector3 lockedDirection;
    public JumpProfile profile;
}


