using UnityEngine;
using Core.Character.Controllers;

public class SlideState
{
    private BallCharacterController controller;

    public SlideState(BallCharacterController c) { controller = c; }

    public void Enter() { /* анимация подката, коллайдер низкий */ }
    public void Exit() { /* восстановление коллайдера */ }

    public void Update(MovementIntent intent)
    {
        if (!intent.slide) controller.CurrentState = BallCharacterController.CharacterState.Running;

        // движение во время скольжения
        //controller.ApplyMovement(intent);
    }
}