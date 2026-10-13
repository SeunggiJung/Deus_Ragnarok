using UnityEngine;

// 사망. 다른 상태로 전환되지 않으며 입력도 받지 않음
public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        // 애니메이터의 die 파라미터는 Trigger라서 SetBool 대신 SetTrigger 사용
        player.Anim.SetTrigger(animBoolName);
        player.SetVelocity(0f, player.Rb.velocity.y);
    }

    public override void PhysicsUpdate()
    {
        player.SetVelocity(0f, player.Rb.velocity.y);
    }

    public override void Exit() { }
}
