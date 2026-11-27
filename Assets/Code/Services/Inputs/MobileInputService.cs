using UnityEngine;

namespace Code.Services.Inputs
{
    public class MobileInputService : InputService
    {
        public override Vector2 Axis => GetJoystickAxis();
    }
}