using System;
using System.Collections.Generic;

namespace GamePlay.MiniGame
{
    public class MiniGameLogic
    {
        private readonly List<FallingRing> _activeRings = new List<FallingRing>();
        private readonly Random _random = new Random();

        public IReadOnlyList<FallingRing> ActiveRings => _activeRings;
        public ChuteType CatcherPosition { get; private set; } = ChuteType.BottomLeft;
        public int Score { get; private set; } = 0;

        // Настройки баланса
        public float SpawnInterval { get; set; } = 1.2f;
        public float RingSpeed { get; set; } = 0.45f;
        public float CatchWindowStart { get; set; } = 0.85f;
        public float CatchWindowEnd { get; set; } = 1.05f;

        private float _spawnTimer = 0f;
        private int _ringIdSequence = 0;

        // События (для визуала, звуков и внешнего мира)
        public event Action<int> OnScoreChanged;
        public event Action<ChuteType> OnCatcherMoved;
        public event Action<FallingRing> OnRingSpawned;
        public event Action<FallingRing> OnRingCaught;
        public event Action<FallingRing> OnRingMissed;

        public void SetCatcherPosition(ChuteType newPosition)
        {
            if (CatcherPosition == newPosition) return;
            CatcherPosition = newPosition;
            OnCatcherMoved?.Invoke(CatcherPosition);
        }

        public void Tick(float deltaTime)
        {
            UpdateSpawn(deltaTime);
            UpdateRings(deltaTime);
        }

        private void UpdateSpawn(float deltaTime)
        {
            _spawnTimer += deltaTime;
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer = 0f;
                SpawnRing();
            }
        }

        private void SpawnRing()
        {
            ChuteType randomChute = (ChuteType)_random.Next(0, 4);
            var ring = new FallingRing(++_ringIdSequence, randomChute, RingSpeed);
            _activeRings.Add(ring);
            OnRingSpawned?.Invoke(ring);
        }

        private void UpdateRings(float deltaTime)
        {
            for (int i = _activeRings.Count - 1; i >= 0; i--)
            {
                var ring = _activeRings[i];
                ring.Advance(deltaTime);

                // Попадание в окно ловли
                if (!ring.IsCaught && ring.Progress >= CatchWindowStart && ring.Progress <= CatchWindowEnd)
                {
                    if (CatcherPosition == ring.Chute)
                    {
                        ring.IsCaught = true;
                        Score++;
                        OnScoreChanged?.Invoke(Score);
                        OnRingCaught?.Invoke(ring);
                        _activeRings.RemoveAt(i);
                        continue;
                    }
                }

                // Промах
                if (ring.Progress > CatchWindowEnd && !ring.IsCaught && !ring.IsMissed)
                {
                    ring.IsMissed = true;
                    OnRingMissed?.Invoke(ring);
                    _activeRings.RemoveAt(i);
                }
            }
        }
    }
}