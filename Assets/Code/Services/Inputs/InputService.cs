using UnityEngine;

namespace Code.Services.Inputs
{
    public abstract class InputService : IInputService
    {
        public abstract Vector2 Axis { get; }
        private VariableJoystick _joystick;
        
        public void InitializeJoystick(VariableJoystick joystick)
        {
            _joystick = joystick;
        }

        public bool IsAttackButtonUp()
        {
            return false;
        }
        
        protected Vector2 GetJoystickAxis() =>
            new Vector2(_joystick.Horizontal, _joystick.Vertical);
    }
}