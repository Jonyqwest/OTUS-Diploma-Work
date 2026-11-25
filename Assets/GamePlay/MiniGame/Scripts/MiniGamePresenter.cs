using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GamePlay.MiniGame
{
    public class MiniGamePresenter : MonoBehaviour
    {
        [SerializeField] private MiniGameView _view;

        public MiniGameLogic Logic { get; private set; }
        public MiniGameInput Input { get; private set; }

        public event Action<int> OnGameFinished;

        private bool _isAutoPlay = true;        // Демо-режим по умолчанию
        private bool _isReadyToContinue = false;
        private bool _isEnding = false;
        private float _clumsyTimer = 0f;
        private UniTaskCompletionSource<int> _finishTaskSource;
        
        private void Awake()
        {
            if (_view == null)
                _view = GetComponent<MiniGameView>();

            Logic = new MiniGameLogic();
            Input = new MiniGameInput();
        }

        private void OnEnable()
        {
            // Подписки на инпут
            Input.OnChuteSelected += HandleManualChuteSelect;
            Input.OnContinuePressed += HandleContinue;

            // Подписки на кнопки UI
            _view.OnChuteClicked += HandleManualChuteSelect;
            _view.OnContinueClicked += HandleContinue;

            // Подписки на события модели
            Logic.OnScoreChanged += _view.SetScore;
            Logic.OnCatcherMoved += _view.SetCatcherPosition;
            Logic.OnRingSpawned += ring => _view.SpawnRing(ring.Id);
            Logic.OnRingCaught += ring => _view.DestroyRing(ring.Id);
            Logic.OnRingMissed += ring => _view.DestroyRing(ring.Id);
        }

        private void OnDisable()
        {
            Input.OnChuteSelected -= HandleManualChuteSelect;
            Input.OnContinuePressed -= HandleContinue;

            _view.OnChuteClicked -= HandleManualChuteSelect;
            _view.OnContinueClicked -= HandleContinue;

            Logic.OnScoreChanged -= _view.SetScore;
            Logic.OnCatcherMoved -= _view.SetCatcherPosition;
        }

        private void Start()
        {
            _view.SetCatcherPosition(Logic.CatcherPosition);
            _view.SetScore(0);
            _view.SetTakeControlPromptActive(true); // Показываем приглашение помочь
            _view.FadeIn(0.5f);
        }

        private void Update()
        {
            if (_isEnding) return;

            // Опрашиваем реальный ввод
            Input.UpdateInput();

            // Если игрок еще не взял управление — работает комичный автопилот
            if (_isAutoPlay)
            {
                UpdateClumsyAutoPlay();
            }

            Logic.Tick(Time.deltaTime);

            // Передаем координаты колец во Вьюху
            foreach (var ring in Logic.ActiveRings)
            {
                _view.SetRingProgress(ring.Id, ring.Chute, ring.Progress);
            }
        }

        /// <summary>
        /// Вызывается при первом нажатии игроком любой кнопки управления
        /// </summary>
        private void HandleManualChuteSelect(ChuteType chute)
        {
            if (_isAutoPlay)
            {
                _isAutoPlay = false;
                _view.SetTakeControlPromptActive(false); // Прячем приглашение, игрок в игре!
            }

            Logic.SetCatcherPosition(chute);
        }

        /// <summary>
        /// Логика неуклюжей демоницы: мечется, но всегда встает туда, где кольцо НЕ падает
        /// </summary>
        private void UpdateClumsyAutoPlay()
        {
            _clumsyTimer += Time.deltaTime;
            if (_clumsyTimer >= 0.5f)
            {
                _clumsyTimer = 0f;

                // Если есть летящие кольца, выбираем лоток, противоположный ближайшему кольцу
                if (Logic.ActiveRings.Count > 0)
                {
                    ChuteType dangerousChute = Logic.ActiveRings[0].Chute;
                    ChuteType wrongChute = (ChuteType)(((int)dangerousChute + 2) % 4);
                    Logic.SetCatcherPosition(wrongChute);
                }
                else
                {
                    // Иначе просто комично мечется
                    ChuteType randomChute = (ChuteType)UnityEngine.Random.Range(0, 4);
                    Logic.SetCatcherPosition(randomChute);
                }
            }
        }

         
        /// <summary>
        /// Вызывается стейт-машиной, когда фоновая загрузка мира готова на 100%
        /// </summary>
        public void NotifyLoadingComplete()
        {
            _isReadyToContinue = true;
            _view.SetContinueActive(true); // Ангел показался в окошке

            // Если игрок так и не взял управление — через 0.8 сек идем дальше автоматически!
            if (_isAutoPlay)
            {
                AutoExitAsync().Forget();
            }
        }

        private async UniTaskVoid AutoExitAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
            if (_isAutoPlay && !_isEnding)
                HandleContinue();
        }

        public UniTask<int> WaitForFinishAsync()
        {
            if (_finishTaskSource == null)
                _finishTaskSource = new UniTaskCompletionSource<int>();

            return _finishTaskSource.Task;
        }
         
        
        private void HandleContinue()
        {
            // Если игрок вообще не играл сам, нажатие продолжения просто пустит дальше с 0 очков
            if (!_isReadyToContinue || _isEnding) return;

            _isEnding = true;
            int finalScore = Logic.Score;

            _view.PlayTallyAnimation(finalScore, () =>
            {
                _view.FadeOut(0.5f, () =>
                {
                    _finishTaskSource?.TrySetResult(finalScore); // ← Добавить эту строчку
                    OnGameFinished?.Invoke(finalScore);
                });
            });
        }
    }
}