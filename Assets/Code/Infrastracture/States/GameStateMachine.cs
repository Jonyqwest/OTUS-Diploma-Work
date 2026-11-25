using System;
using System.Collections.Generic;
using Code.Infrastracture.Factory;
using Code.Infrastracture.Logic;
using Code.Infrastracture.Services;

namespace Code.Infrastracture.States
{
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IExitableState> _states;
        private IExitableState _activeState;

        public GameStateMachine(SceneLoader scenLoader, LoadingCurtain curtain, IGameFactory gameFactory, VariableJoystick joystick)
        {
            _states = new Dictionary<Type, IExitableState>()
            {
                [typeof(BootstrapState)] = new BootstrapState(this, scenLoader, joystick),
                [typeof(PreloadMinigameState)] = new PreloadMinigameState(this, AllServices.Container),
                [typeof(LoadLevelState)] = new LoadLevelState(this, scenLoader, gameFactory, curtain),
                [typeof(GameLoopState)] = new GameLoopState(this)
            };
        }

        public void Enter<TState>() where TState : IState
        {
            _activeState?.Exit();
            IState state = _states[typeof(TState)] as IState;
            _activeState = state;
            state.Enter();
        }
        
        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadState<TPayload>
        {
            _activeState?.Exit();
            IPayloadState<TPayload> state = _states[typeof(TState)] as IPayloadState<TPayload>;
            _activeState = state;
            state.Enter(payload);
        }
        
        // private TState GetState<TState>() where TState : class, IExitableState => 
        //     _states[typeof(TState)] as TState;
    }
}