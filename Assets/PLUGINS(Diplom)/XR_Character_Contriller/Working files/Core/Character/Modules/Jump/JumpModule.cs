using UnityEngine;

[System.Serializable]
public class JumpModule
{
    public float strange = 3;
    private readonly Rigidbody rb;
    private JumpState state;

    private JumpProfile currentProfile;
    private float jumpTimer;

    public JumpModule(Rigidbody rigidbody
        //,float force
        )
    {
        rb = rigidbody;
        state.phase = JumpPhase.Grounded;
        //strange = force;
    }

    // ----------------------------------
    // PUBLIC API
    // ----------------------------------

    public void Tick(MovementIntent intent, float dt)
    {
        if (state.phase == JumpPhase.Grounded)
            return;
        ApplyJumpForce(intent, dt);
    }

    public void TryStartJump(MovementIntent intent, float jumpForce, JumpProfile profile)
    {
        if (state.phase != JumpPhase.Grounded)
            return;
        strange = jumpForce;
        currentProfile = profile;
        jumpTimer = 0f;
        state.phase = JumpPhase.RisingMin;
        
        float forwardPush;
        if (currentProfile.type == JumpType.JumpUp)
            forwardPush = currentProfile.idleForwardPush;
        else
            forwardPush = 0;
        rb.linearVelocity += rb.transform.forward * forwardPush;
        
        state.lockedDirection = rb.linearVelocity;
    }

    public void OnGrounded()
    {
        state.phase = JumpPhase.Grounded;
        jumpTimer = 0f;
        currentProfile = null;
    }

    // ----------------------------------
    // INTERNAL LOGIC
    // ----------------------------------

    private void ApplyJumpForce(MovementIntent intent, float dt)
    {
        if (currentProfile == null)
            return;

        jumpTimer += dt;

        switch (state.phase)
        {
            case JumpPhase.RisingMin:
                ApplyGuaranteedRise(dt);

                if (jumpTimer >= currentProfile.minDuration)
                {
                    jumpTimer = 0f;
                    state.phase = JumpPhase.Rising;
                }
                break;

            case JumpPhase.Rising:
                if (!intent.jumpHeld || jumpTimer >= currentProfile.duration)
                {
                    state.phase = JumpPhase.Falling;
                    break;
                }

                ApplyVariableRise();
                break;

            case JumpPhase.Falling:
                // ApplyExtraGravity();
                break;
        }
        ApplyHorizontalDirection(intent,dt);
    }

    // ----------------------------------
    // PHASES
    // ----------------------------------

    private void ApplyGuaranteedRise(float dt)
    {
        float force =
            (currentProfile.minHeight / currentProfile.minDuration) * strange;

        rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
    }

    private void ApplyVariableRise()
    {
        float t = jumpTimer / currentProfile.duration;
        float curveValue = currentProfile.curve.Evaluate(t);

        float baseForce = strange * currentProfile.height;
        float force = baseForce * curveValue;

        rb.AddForce(Vector3.up * force, ForceMode.Acceleration);
    }

    // private void ApplyExtraGravity()
    // {
    //     rb.AddForce(
    //         Physics.gravity * (currentProfile.gravityMultiplier - 1f),
    //         ForceMode.Acceleration
    //     );
    // }


    void ApplyHorizontalDirection(MovementIntent intent, float dt)
    {
        //вектор намерения
        Vector3 desired = new Vector3( intent.moveRun.x, 0f, intent.moveRun.y
        );
        
        //вектор c учетом доруливания
        Vector3 velocity = Vector3.Lerp( state.lockedDirection, desired, currentProfile.airControl
        );

       
        //применяем как смещение
        rb.linearVelocity = new Vector3( velocity.x, rb.linearVelocity.y, velocity.z);
    }
}
 










////using UnityEngine;

////[System.Serializable]
////public class JumpModule
////{
////    private Rigidbody rb;
////    JumpState state;

////    private JumpProfile currentProfile;

////    //private bool isJumping;
////    private float jumpTimer;

////    public JumpModule(Rigidbody rigidbody)
////    {
////        rb = rigidbody;
////    }

////    // ----------------------------------
////    // PUBLIC API
////    // ----------------------------------

////    public void Tick(MovementIntent intent, float dt)
////    {
////        if (state.phase != JumpPhase.Grounded)
////        {
////            ApplyJumpForce(intent, dt);
////            Debug.Log("[LumpModule] Tick, state.phase =" + state.phase);
////        }
////    }

////    public void TryStartJump(MovementIntent intent, JumpProfile profile)
////    {
////        if (!intent.jumpPressed || state.phase != JumpPhase.Grounded)
////            return;

////        state.phase = JumpPhase.Rising;
////        currentProfile = profile;
////        //isJumping = true;
////        jumpTimer = 0f;
////    }

////    public void OnGrounded()
////    {
////        state.phase = JumpPhase.Grounded;
////        jumpTimer = 0f;
////        currentProfile = null;
////    }

////    // ----------------------------------
////    // INTERNAL LOGIC
////    // ----------------------------------

////    private void ApplyJumpForce(MovementIntent intent, float dt)
////    {
////        if (currentProfile == null)
////        {
////            Debug.Log("Нет профайла");
////            return;
////        }


////        jumpTimer += dt;
////        bool canApplyForce =
////            intent.jumpHeld &&
////            jumpTimer <= currentProfile.duration;

////        if (!canApplyForce)
////        {
////            state.phase = JumpPhase.Falling;
////            Debug.Log("прыжек прекращен! state.phase= "+ state.phase);
////            return;
////        }

////        // нормализованное время 0..1
////        float t = jumpTimer / currentProfile.duration;

////        // значение кривой
////        float curveValue = currentProfile.curve.Evaluate(t);
////        // расчёт силы:
////        // F = h / t² (упрощённая баллистика)
////        float baseForce = 15f * currentProfile.height;

////        float force = baseForce * curveValue;

////        rb.AddForce(Vector3.up * force, ForceMode.Acceleration);

////        // усиленная гравитация при отпускании
////        if (state.phase == JumpPhase.Falling)
////        {
////            rb.AddForce(
////                Physics.gravity * (currentProfile.gravityMultiplier - 1f),
////                ForceMode.Acceleration
////            );
////        }
////    }
////}











//////using UnityEngine;

//////public class JumpModule
//////{
//////    readonly Rigidbody rb;

//////    JumpState state;

//////    public bool IsJumping => state.phase != JumpPhase.Grounded;

//////    public JumpModule(Rigidbody rb)
//////    {
//////        this.rb = rb;
//////        state.phase = JumpPhase.Grounded;
//////    }

//////    // вызывается Character'ом
//////    public void TryStartJump(MovementIntent intent, JumpProfile profile)
//////    {
//////        if (state.phase != JumpPhase.Grounded)
//////            return;

//////        if (!intent.jumpPressed)
//////            return;

//////        state.profile = profile;
//////        state.phase = JumpPhase.Rising;
//////        state.time = 0f;
//////        state.startY = rb.position.y;

//////        // 🔒 фиксируем горизонтальное направление
//////        state.lockedDirection = rb.velocity;
//////        state.lockedDirection.y = 0f;

//////        rb.velocity = new Vector3(
//////            state.lockedDirection.x,
//////            0f,
//////            state.lockedDirection.z
//////        );
//////    }

//////    public void Tick(MovementIntent intent, float dt)
//////    {
//////        if (state.phase == JumpPhase.Grounded)
//////            return;

//////        state.time += dt;

//////        UpdateVertical(intent);
//////        UpdateHorizontal(intent, dt);
//////        ApplyGravity(dt);
//////    }

//////    void UpdateVertical(MovementIntent intent)
//////    {

//////        var profile = state.profile;
//////        float t = Mathf.Clamp01(state.time / profile.duration);
//////        float curveY = profile.curve.Evaluate(t);

//////        float targetY = state.startY + curveY * profile.height;

//////        if (targetY > rb.position.y)
//////        {
//////            rb.MovePosition(new Vector3(
//////                rb.position.x,
//////                targetY,
//////                rb.position.z
//////            ));
//////        }

//////        // ✂️ ранний отпуск
//////        if (!intent.jumpHeld && state.phase == JumpPhase.Rising)
//////        {
//////            state.phase = JumpPhase.Falling;
//////        }

//////        // ⛔ достигли пика
//////        if (t >= 1f)
//////        {
//////            state.phase = JumpPhase.Falling;
//////        }
//////    }

//////    void UpdateHorizontal(MovementIntent intent, float dt)
//////    {
//////        var profile = state.profile;

//////        Vector3 desired = new Vector3(
//////            intent.move.x,
//////            0f,
//////            intent.move.y
//////        );

//////        Vector3 velocity = Vector3.Lerp(
//////            state.lockedDirection,
//////            desired,
//////            profile.airControl
//////        );

//////        rb.velocity = new Vector3(
//////            velocity.x,
//////            rb.velocity.y,
//////            velocity.z
//////        );
//////    }

////    //void ApplyGravity(float dt)
//////    {
//////        rb.velocity += Physics.gravity * state.profile.gravityMultiplier * dt;
//////    }

//////    public void OnGrounded()
//////    {
//////        state.phase = JumpPhase.Grounded;
//////        state.time = 0f;
//////    }
//////}
///
