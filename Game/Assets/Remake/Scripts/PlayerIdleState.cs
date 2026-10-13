using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }
    public override void HandleInput()
    {
        base.HandleInput();
        if (stateMachine.CurrentState != this) return; // 공격/점프/낙하로 이미 전환됨

        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            stateMachine.ChangeState(player.MoveState);
        }
    }
    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0, player.Rb.velocity.y);
    }
}
