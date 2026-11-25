using UnityEngine;

namespace Code.Services.Inputs
{
    public class PCInputService : InputService
    {
        private const string Horizontal = "Horizontal";
        private const string Vertical = "Vertical";
        public override Vector2 Axis => GetUnityAxis();
                
        private Vector2 GetUnityAxis() => 
            new (Input.GetAxis(Horizontal), Input.GetAxis(Vertical));
    }
}