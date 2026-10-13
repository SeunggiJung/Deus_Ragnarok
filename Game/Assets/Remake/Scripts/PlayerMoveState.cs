using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void HandleInput()
    {
        // 공격 입력을 이동보다 먼저 확인 (한 프레임에 상태가 두 번 바뀌지 않도록 else if)
        if (Input.GetMouseButtonDown(0)) // 마우스 좌클릭 공격
        {
            stateMachine.ChangeState(player.AttackState);
        }
        else if (Input.GetAxisRaw("Horizontal") == 0) // 입력이 없으면 IdleState로 변경
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
