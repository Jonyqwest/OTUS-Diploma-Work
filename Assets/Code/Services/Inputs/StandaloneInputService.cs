using UnityEngine;

namespace Code.Services.Inputs
{
    public class StandaloneInputService : InputService
    {
        private const string Horizontal = "Horizontal";
        private const string Vertical = "Vertical";
        public override Vector2 Axis
        {
            get
            {
                Vector2 axis = GetJoystickAxis();
                
                if (axis == Vector2.zero)
                    axis = GetUnityAxis();
                
                return axis;
            }
        }
        private Vector2 GetUnityAxis() => 
            new (Input.GetAxis(Horizontal), Input.GetAxis(Vertical));
    }
}