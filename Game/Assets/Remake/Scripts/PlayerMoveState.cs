using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerGroundedState
{
    public PlayerMoveState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void HandleInput()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this) return; // 공격/점프/낙하로 이미 전환됨

        if (player.Input.MoveX == 0) // 입력이 없으면 IdleState로 변경
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float xInput = player.Input.MoveX;
        // 부드러운 이동 처리를 위해 PlayerController의 함수 호출
        player.SetVelocity(xInput * player.moveSpeed, player.Rb.velocity.y);
        player.CheckFlip(xInput);
    }
}
