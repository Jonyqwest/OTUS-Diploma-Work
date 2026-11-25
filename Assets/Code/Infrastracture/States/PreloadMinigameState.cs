using Cysharp.Threading.Tasks;
using Code.Infrastracture.Services;
using Code.Infrastracture.Services.PersistentProgress;
using GamePlay.MiniGame;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Infrastracture.States
{
    public class PreloadMinigameState : IPayloadState<string>
    {
        private readonly GameStateMachine _stateMachine;
        private readonly AllServices _services;

        public PreloadMinigameState(GameStateMachine stateMachine, AllServices services)
        {
            _stateMachine = stateMachine;
            _services = services;
        }

        public void Enter(string targetScene)
        {
            PreloadAndRunAsync(targetScene).Forget();
        }

        private async UniTaskVoid PreloadAndRunAsync(string targetScene)
        {
            var presenter = Object.FindAnyObjectByType<MiniGamePresenter>();

            // 1. Начинаем фоновую загрузку сцены
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene);
            if (loadOp == null)
            {
                Debug.LogError($"[PreloadMinigameState] Сцена '{targetScene}' не найдена в Build Settings! Добавьте ее в File -> Build Settings.");
                return;
            }

            loadOp.allowSceneActivation = false;

            // 2. Ждем, пока сцена полностью окажется в оперативной памяти
            await UniTask.WaitUntil(() => loadOp.progress >= 0.9f);

            // 3. Оповещаем мини-игру и ждем действия игрока
            int score = 0;
            if (presenter != null)
            {
                presenter.NotifyLoadingComplete();
                score = await presenter.WaitForFinishAsync();
            }

            // 4. Начисляем заработанные кольца в профиль
            var progressService = _services.Single<IPersistentProgressService>();
            progressService?.Progress?.WalletData?.AddRings(score);

            // 5. Активируем сцену
            loadOp.allowSceneActivation = true;
            await loadOp;

            // 6. Передаем эстафету в LoadLevelState для спавна персонажа
            _stateMachine.Enter<LoadLevelState, string>(targetScene);
        }
        public void Exit() { }
    }
}