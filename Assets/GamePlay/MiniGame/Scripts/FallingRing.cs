namespace GamePlay.MiniGame
{
    public class FallingRing
    {
        public readonly int Id;
        public readonly ChuteType Chute;
        public float Progress { get; private set; } 
        public float Speed { get; private set; }

        public bool IsCaught { get; set; }
        public bool IsMissed { get; set; }

        public FallingRing(int id, ChuteType chute, float speed)
        {
            Id = id;
            Chute = chute;
            Speed = speed;
            Progress = 0f;
        }

        public void Advance(float deltaTime)
        {
            Progress += Speed * deltaTime;
        }
    }
}