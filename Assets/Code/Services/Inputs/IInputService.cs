using Code.Infrastracture.Services;
using UnityEngine;

namespace Code.Services.Inputs
{
    public interface IInputService : IService
    {
        Vector2 Axis { get; }
        public void InitializeJoystick(VariableJoystick joystick);
        bool IsAttackButtonUp();
    }
}