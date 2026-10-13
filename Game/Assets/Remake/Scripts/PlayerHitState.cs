using UnityEngine;

// 피격 경직. hitStunTime 동안 입력을 받지 않고 멈춘 뒤 복귀 (공격 중에 맞으면 공격도 끊김)
public class PlayerHitState : PlayerState
{
    private float elapsed;

    public PlayerHitState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();
        elapsed = 0f;
        player.SetVelocity(0f, player.Rb.velocity.y);
    }

    public override void Update()
    {
        base.Update();
        elapsed += Time.deltaTime;

        if (elapsed >= player.Stats.hitStunTime)
        {
            if (player.IsGrounded())
                stateMachine.ChangeState(player.IdleState);
            else
                stateMachine.ChangeState(player.FallState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        player.SetVelocity(0f, player.Rb.velocity.y);
    }
}
