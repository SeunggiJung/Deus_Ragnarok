using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController player, StateMachine stateMachine, string animBoolName) 
        : base(player, stateMachine, animBoolName) { }
    public override void HandleInput()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        if(xInput != 0)
        {
            stateMachine.ChangeState(player.MoveState);
        }
        if (Input.GetKeyDown(KeyCode.Z)) // 또는 원하는 공격 키
        {
            stateMachine.ChangeState(player.AttackState);
        }
    }
    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(0, player.Rb.velocity.y);
    }
}
