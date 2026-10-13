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

        // 진입 직후에는 애니메이터가 아직 이전 상태(Idle 등)를 재생 중이므로
        // 실제로 Attack 상태에 들어온 뒤의 재생률만 보고 Idle로 전환
        AnimatorStateInfo stateInfo = player.Anim.GetCurrentAnimatorStateInfo(0);
        if (!player.Anim.IsInTransition(0) && stateInfo.IsName("Attack") && stateInfo.normalizedTime >= 1.0f)
        {
            stateMachine.ChangeState(player.IdleState);
        }
    }
    public override void Exit()
    {
        base.Exit();
    }
}
