using UnityEngine;

// 점프 공격 (크리티컬). 체공 중 한 번만 가능하며 데미지에 criticalMultiplier 배율 적용.
// 공격 중에는 방향 전환 없이 점프 관성을 유지하고, 착지하면 그 자리에 멈춰 공격을 마무리
public class PlayerJumpAttackState : PlayerState
{
    public PlayerJumpAttackState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();
        player.CanAirAttack = false;
    }

    // 점프 공격 도중 구르기로 캔슬
    public override void HandleInput()
    {
        base.HandleInput();
        TryDodge();
    }

    public override void Update()
    {
        base.Update();

        if (player.IsAttackAnimationFinished())
        {
            if (!player.IsGrounded())
                stateMachine.ChangeState(player.FallState);
            else if (player.Input.MoveX != 0)
                stateMachine.ChangeState(player.MoveState);
            else
                stateMachine.ChangeState(player.IdleState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 착지했으면 미끄러지지 않도록 수평 속도 제거
        if (player.IsGrounded() && player.Rb.velocity.y <= 0f)
            player.SetVelocity(0f, player.Rb.velocity.y);
    }
}
