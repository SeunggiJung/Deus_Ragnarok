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
        AnimatorStateInfo stateInfo = player.Anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Attack"))
        {
            if (stateInfo.normalizedTime >= 1.0f)
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }
        else
        {
            // 만약 공격 상태인데 공격 애니메이션이 안 나오고 있다면? 
            // 일단 Idle로 탈출시켜서 캐릭터가 굳는 걸 방지합니다.
            //stateMachine.ChangeState(player.IdleState); 
        }
    }
    public override void Exit()
    {
        base.Exit();
        // 공격 종료 후 처리 (필요 시)
    }
}
