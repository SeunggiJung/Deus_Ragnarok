using System.Collections.Generic;
using UnityEngine;

// 구르기. 진입 시 경로를 계산해 적을 관통할지 / 적 앞에서 멈출지 결정하고,
// dodgeDuration 동안 계산된 거리만큼 일정하게 이동. 시작부터 dodgeInvincibleTime 동안 무적
public class PlayerDodgeState : PlayerState
{
    private readonly List<Collider2D> passThrough = new List<Collider2D>();
    private float dir;
    private float startX;
    private float distance;
    private float elapsed;

    public PlayerDodgeState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();

        // 방향키를 누르고 있으면 그 방향, 아니면 바라보는 방향으로 구르기
        float xInput = player.Input.MoveX;
        dir = xInput != 0 ? Mathf.Sign(xInput) : player.FacingDir;
        player.CheckFlip(dir);

        distance = player.PlanDodge(dir, passThrough);
        player.SetIgnoreCollision(passThrough, true);

        startX = player.Rb.position.x;
        elapsed = 0f;
        player.ForceInvincible = true;
    }

    public override void Update()
    {
        base.Update();
        elapsed += Time.deltaTime;

        if (elapsed >= player.Stats.dodgeInvincibleTime)
            player.ForceInvincible = false;

        if (elapsed >= player.Stats.dodgeDuration)
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

        // 속도 대신 위치로 이동시켜 마찰 등에 상관없이 계산한 거리만큼 정확히 이동
        float t = Mathf.Clamp01(elapsed / player.Stats.dodgeDuration);
        player.SetVelocity(0f, player.Rb.velocity.y);
        player.Rb.MovePosition(new Vector2(startX + dir * distance * t, player.Rb.position.y));
    }

    public override void Exit()
    {
        base.Exit();
        player.SetVelocity(0f, player.Rb.velocity.y);
        player.SetIgnoreCollision(passThrough, false);
        player.ForceInvincible = false;
    }
}
