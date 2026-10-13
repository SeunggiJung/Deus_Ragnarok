using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerState
{
    public PlayerAttackState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }
    // Start is called before the first frame update
    public override void Enter()
    {
        base.Enter();
        // 공격 시작 시 속도를 0으로 만들어 제자리에서 공격하게 함
        player.SetVelocity(0, player.Rb.velocity.y);
    }

    public override void Update()
    {
        base.Update();

        // 애니메이션의 재생률이 100%가 된다면 Idle 상태로 전환
        if (player.Anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void Exit()
    {
        base.Exit();
    }
}
