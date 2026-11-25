using UnityEngine;

namespace Core.Character.States
{
    public class FlightState
    {
        private readonly Rigidbody rb;
        private readonly BallCharacterConfig config;

        public FlightState(Rigidbody rb, BallCharacterConfig config)
        {
            this.rb = rb;
            this.config = config;
        }

        public void Tick(Vector3 controlDirection)
        {
            // подъёмная сила (компенсация гравитации)
            Vector3 lift = Vector3.up * config.flightLift;
            rb.AddForce(lift, ForceMode.Acceleration);

            // управление полётом
            rb.AddForce(controlDirection * config.flightForwardForce,
                ForceMode.Acceleration);
        }
    }
}
