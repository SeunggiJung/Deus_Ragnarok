using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerController player, StateMachine stateMachine, string animBoolName) 
        : base(player, stateMachine, animBoolName) { }
    public override void HandleInput()
    {
        // 공격 입력을 이동보다 먼저 확인 (한 프레임에 상태가 두 번 바뀌지 않도록 else if)
        if (Input.GetMouseButtonDown(0)) // 마우스 좌클릭 공격
        {
            stateMachine.ChangeState(player.AttackState);
        }
        else if (Input.GetAxisRaw("Horizontal") != 0)
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
