using Code.Data;
using Code.Infrastracture.AssetManagment;
using Code.Infrastracture.Factory;
using Code.Infrastracture.Services;
using Code.Infrastracture.Services.PersistentProgress;
using Code.Services.Inputs;
using UnityEngine;

namespace Code.Infrastracture.States
{
    public class BootstrapState: IState
    { 
        private const string initial = "Initialize";
        private const string main = "Main";
        private readonly GameStateMachine _stateMachine;
        private VariableJoystick _joystick;
        private SceneLoader _scenLoader;

        public BootstrapState(GameStateMachine stateMachine, SceneLoader scenLoader, VariableJoystick joystick)
        {
            _stateMachine = stateMachine;
            _scenLoader = scenLoader;
            _joystick = joystick;
        }

        public void Enter()
        {
            RegisterServices(_joystick);
            _scenLoader.Load(initial, EnderLoadLevel);
        }

        private void EnderLoadLevel()
        {
            _stateMachine.Enter<PreloadMinigameState, string>(main);
        }
        public void Exit()
        {
        }
        
        
        private void RegisterServices(VariableJoystick joystick)
        {
            AllServices.Container.RegisterSingle<IPersistentProgressService>(new PersistentProgressService());
            AllServices.Container.RegisterSingle<IAssets>(new AssetProvider());
            AllServices.Container.RegisterSingle<IGameFactory>(new GameFactory(AllServices.Container.Single<IAssets>() ));
            AllServices.Container.RegisterSingle<IInputService>(RegisterInputServices(joystick));
            // Game.InputService = RegisterInputServices(joystick);
            Game.InputService = AllServices.Container.Single<IInputService>();
        }

        private IInputService RegisterInputServices(VariableJoystick joystick)
        {
            IInputService InputService;
            if (Application.isEditor)
                InputService = new StandaloneInputService();
            else
            {
                if (Application.isMobilePlatform)
                    InputService = new MobileInputService();
                else
                    InputService = new PCInputService();
            }
            InputService.InitializeJoystick(joystick);
            return InputService;
        }
    }
}