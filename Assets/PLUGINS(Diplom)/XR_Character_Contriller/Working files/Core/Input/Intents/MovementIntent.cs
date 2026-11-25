using System.Collections.Generic;
using UnityEngine;

public struct MovementIntent
{
    /// <summary>
    /// Унифицированное намерение игрока.
    /// Только "что хочу", без логики и физики.
    /// </summary>
    ///

    // ─── направление ВЗГЛЯДА ─────────────────────────────────────────
    public Vector3 viewDirection;
    // ─── направление ДВИЖЕНИЯ ─────────────────────────────────────────
    public Vector3 moveDirection;

    public Vector3 moveRun;        // движение wasd(XZ)
    public Vector3 moveWalk;        // движение wasd(XZ)
    
    public bool freeLook;       // если true фиксируем направление движения

    public bool jumpHeld;       // кнопка зажата
    private bool jumpPressed;    // момент нажатия
    // public float jumpDuration;

    public bool walkMode;       // ходьба shift
    public bool slide;          // подкат
    public bool flight; 
    public bool dart;
    public bool boostPressed;  // готовность нитро

    public void Update(MovementIntent intent)
    {
        var _jumpPressed = jumpPressed;
        this = intent;
        if (_jumpPressed == true)
            jumpPressed = true;
    }
    
    public void ClearTransient()
    {
        jumpHeld = false;     
        jumpPressed = false;   
        slide = false;
        flight = false;
        dart = false;
    }

    public void SetjumpPressed(bool pressed)
    {
        if (pressed && !jumpPressed)
            jumpPressed = true;
    }

    public bool JumpPressedDequeue()
    {   var pressed = jumpPressed;
        jumpPressed = false;
        return pressed;
    }
    public bool ReadjumpPressed()
    {
        return jumpPressed;
    }
}