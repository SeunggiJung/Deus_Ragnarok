using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void HandleInput()
    {
        // 입력이 없으면 IdleState로 변경
        float xInput = Input.GetAxisRaw("Horizontal");
        if (xInput == 0)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float xInput = Input.GetAxisRaw("Horizontal");
        // 부드러운 이동 처리를 위해 PlayerController의 함수 호출
        player.SetVelocity(xInput * player.moveSpeed, player.Rb.velocity.y);
        player.CheckFlip(xInput);
    }
}
