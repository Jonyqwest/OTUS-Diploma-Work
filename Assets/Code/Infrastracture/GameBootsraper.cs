using Code.Infrastracture.AssetManagment;
using Code.Infrastracture.Factory;
using Code.Infrastracture.Logic;
using Code.Infrastracture.States;
using Unity.VisualScripting;
using UnityEngine;

namespace Code.Infrastracture
{
    public class GameBootsraper : MonoBehaviour, ICoroutineRunner
    {
        [SerializeField] private VariableJoystick _joystick;
        [SerializeField] private LoadingCurtain _curtain;
        private IGameFactory _gameFactory;
        private AssetProvider _assetProvider;
        
        private Game _game;

        private void Awake()
        {
            _assetProvider =  new AssetProvider();
            _gameFactory = new GameFactory(_assetProvider);
            _game = new Game(this,_gameFactory, _curtain, _joystick);
            _game.StateMachine.Enter<BootstrapState>();
            DontDestroyOnLoad(this);
        }

       
    }
}

