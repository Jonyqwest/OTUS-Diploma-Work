using Code.CameraLogic;
using Code.Infrastracture.Factory;
using Code.Infrastracture.Logic;
using UnityEngine;

namespace Code.Infrastracture.States
{
    public class LoadLevelState : IPayloadState<string>
    {
        private readonly GameStateMachine _stateMachine;
        private SceneLoader _scenLoader;
        private readonly LoadingCurtain curtain;
        private LoadingCurtain _curtain;
        private IGameFactory _gameFactory;

        public LoadLevelState(GameStateMachine stateMachine, SceneLoader scenLoader, IGameFactory gameFactory,  LoadingCurtain curtain)
        {
            _stateMachine = stateMachine;
            _scenLoader = scenLoader;
            _curtain = curtain;
            _gameFactory = gameFactory;
        }

        public void Enter(string scene)
        {
            _curtain.Show();
            _scenLoader.Load(scene, OnLoaded);
        }

        private void OnLoaded()
        {
            
            GameObject hero = _gameFactory.CreateHero();
            // Insnstantiate("Hud/Hud");
            CameraFolow(hero);
            _stateMachine.Enter<GameLoopState>();
        }

        private void CameraFolow(GameObject hero)
        {
           Camera.main.GetComponent<CameraFollow>().SetFollower(hero);
        }

        public void Exit() => _curtain.Hide();
    }
}