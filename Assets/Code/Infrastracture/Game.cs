using Code.Infrastracture.Factory;
using Code.Infrastracture.Logic;
using Code.Infrastracture.States;
using Code.Services.Inputs;
using UnityEngine;
namespace Code.Infrastracture
{
    public class Game
    {
        public static IInputService InputService;
        public GameStateMachine StateMachine;
        public Game(ICoroutineRunner coroutineRunner, IGameFactory gameFactory, LoadingCurtain curtain, VariableJoystick joystick)
        {
            StateMachine = new GameStateMachine(new SceneLoader(coroutineRunner), curtain, gameFactory, joystick);
        }
    }
}