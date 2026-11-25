using UnityEngine;

[System.Serializable]
public class GroundModule
{
    private GroundState state;

    readonly LayerMask groundMask;
    readonly float maxSlopeAngle;
    
    public Transform frontSensor;
    public Transform leftSensor;
    public Transform rightSensor;

    public GroundState State => state;
    public bool IsGrounded => state.isGrounded;
    public bool IsContacted => state.isContacted;
    public ContactType ContactType => state.contactType;
    public Vector3 GroundNormal => state.normal;

    public GroundModule(
        LayerMask? groundMask = null,
        float maxSlopeAngle = 55f)
    {
        this.groundMask = groundMask ?? LayerMask.GetMask("Default");
        this.maxSlopeAngle = maxSlopeAngle;

        ResetState();
    }

    // вызывается каждый FixedUpdate / Tick персонажа
    public void Tick()
    {
        state.justLanded = !state.wasGrounded && state.isGrounded;
        state.justLeftGround = state.wasGrounded && !state.isGrounded;

        state.wasGrounded = state.isGrounded;

        // Bump detection - проверяем дополнительные сенсоры
        UpdateBumpDetection();
    }

    // -------------------------
    // BUMP DETECTION (fallback - ручная проверка)
    // -------------------------

    public void UpdateBumpDetection()
    {
        // Fallback: используем Physics.Raycast как запасной вариант
        // Это сработает даже если триггеры не работают
        
        state.frontDetected = CheckWithRaycast(frontSensor);
        state.leftDetected = CheckWithRaycast(leftSensor);
        state.rightDetected = CheckWithRaycast(rightSensor);
    }

    bool CheckWithRaycast(Transform sensor)
    {
        if (sensor == null) return false;
        
        float distance = 1.5f; // Достаточно чтобы достать до земли
        bool hit = Physics.Raycast(sensor.position, Vector3.down, distance, groundMask);
        
        
        return hit;
    }

    // -------------------------
    // COLLISION ADAPTER API
    // -------------------------

    public void NotifyCollisionEnter(Collision collision)
    {
        ProcessCollision(collision);
    }

    public void NotifyCollisionStay(Collision collision)
    {
        ProcessCollision(collision);
    }

    public void NotifyCollisionExit(Collision collision)
    {
        // В Exit контактов уже нет — НИЧЕГО не читаем из collision
        state.isContacted = false;
        state.isGrounded = false;
        state.contactType = ContactType.None;
        state.normal = Vector3.up;
        state.slopeAngle = 0f;
    }

    // -------------------------
    // INTERNAL LOGIC
    // -------------------------

    void ProcessCollision(Collision collision)
    {
        if (!IsLayerAllowed(collision.gameObject.layer))
            return;

        if (collision.contactCount == 0)
            return;

        ContactPoint contact = collision.GetContact(0);

        state.isContacted = true;
        state.normal = contact.normal;
        state.slopeAngle = Vector3.Angle(contact.normal, Vector3.up);

        if (state.slopeAngle <= maxSlopeAngle)
        {
            // 🟢 земля
            state.isGrounded = true;
            state.contactType = ContactType.Ground;
        }
        else
        {
            // 🔴 стена / потолок
            state.isGrounded = false;
            state.contactType = ContactType.Obstacle;
        }
    }

    bool IsLayerAllowed(int layer)
    {
        return (groundMask.value & (1 << layer)) != 0;
    }

    // -------------------------
    // JUMP CHECK
    // -------------------------

    public bool CanJump()
    {
        bool canJump = state.isGrounded || state.frontDetected;
        // Debug.Log($"[CanJump] isGrounded={state.isGrounded} wasGrounded={state.wasGrounded} frontDetected={state.frontDetected} => canJump={canJump}");
        return canJump;
    }

    // Анимация равновесия - если слева и справа пусто, но основной на земле
    public bool NeedsBalanceAnimation()
    {
        return !state.leftDetected && !state.rightDetected && state.isGrounded;
    }

    // Прямое управление состоянием сенсоров (из BumpSensor)
    public void SetFrontDetected(bool detected) 
    {
        Debug.Log($"[SetFrontDetected] {state.frontDetected} -> {detected}");
        state.frontDetected = detected;
    }
    public void SetLeftDetected(bool detected) 
    {
        Debug.Log($"[SetLeftDetected] {state.leftDetected} -> {detected}");
        state.leftDetected = detected;
    }
    public void SetRightDetected(bool detected) 
    {
        Debug.Log($"[SetRightDetected] {state.rightDetected} -> {detected}");
        state.rightDetected = detected;
    }

    void ResetState()
    {
        state.isContacted = false;
        state.isGrounded = false;
        state.wasGrounded = false;
        state.justLanded = false;
        state.justLeftGround = false;
        state.contactType = ContactType.None;
        state.normal = Vector3.up;
        state.slopeAngle = 0f;
        state.frontDetected = false;
        state.leftDetected = false;
        state.rightDetected = false;
    }
}


//using UnityEngine;

//[System.Serializable]
//public class GroundModule
//{
//    GroundState state;

//    readonly LayerMask groundMask;
//    readonly float maxSlopeAngle;

//    public GroundState State => state;
//    public bool IsGrounded => state.isGrounded;
//    public Vector3 GroundNormal => state.normal;

//    public GroundModule(
//        LayerMask? groundMask = null,
//        float maxSlopeAngle = 55f)
//    {
//        this.groundMask = groundMask ?? LayerMask.GetMask("Default");
//        this.maxSlopeAngle = maxSlopeAngle;
//    }

//    // вызывается из Character.ApplyMovement
//    public void Tick()
//    {
//        state.justLanded = !state.wasGrounded && state.isGrounded;
//        state.justLeftGround = state.wasGrounded && !state.isGrounded;

//        state.wasGrounded = state.isGrounded;
//    }

//    // вызывается адаптером
//    public void NotifyCollisionEnter(Collision collision)
//    {
//        if (!IsGroundCollision(collision))
//            return;

//        state.isGrounded = true;
//        state.normal = collision.GetContact(0).normal;
//        Debug.Log("Colision state.isGrounded = " + state.isGrounded);
//    }

//    public void NotifyCollisionStay(Collision collision)
//    {
//        if (!IsGroundCollision(collision))
//            return;

//        state.isGrounded = true;
//        state.normal = collision.GetContact(0).normal;
//    }

//    public void NotifyCollisionExit(Collision collision)
//    {
//        if (!IsGroundCollision(collision))
//            return;

//        state.isGrounded = false;
//        state.normal = Vector3.up;
//    }

//    // -----------------------
//    // INTERNAL
//    // -----------------------

//    bool IsGroundCollision(Collision collision)
//    {
//        // контакт может отсутствовать на OnCollisionExit
//        if (collision.contactCount == 0)
//            return false;

//        int layer = collision.gameObject.layer;
//        if ((groundMask.value & (1 << layer)) == 0)
//            return false;

//        Vector3 normal = collision.GetContact(0).normal;
//        float slopeAngle = Vector3.Angle(normal, Vector3.up);

//        return slopeAngle <= maxSlopeAngle;
//    }
//}
