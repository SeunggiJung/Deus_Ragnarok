using System.Collections.Generic;
using UnityEngine;

// 구르기. 진입 시 경로를 계산해 적을 관통할지 / 적 앞에서 멈출지 결정하고 계산된 거리만큼 일정하게 이동.
// 시작부터 dodgeInvincibleTime 동안 무적, 적을 관통하는 구르기는 끝날 때까지 무적
// 관통으로 거리가 연장되면 같은 속도를 유지하도록 시간도 비례해서 늘어남
// 공중에서 쓰면 구르는 동안 중력을 끄고 수평으로 대시 (높이 유지) 후 낙하
public class PlayerDodgeState : PlayerState
{
    private readonly List<Collider2D> passThrough = new List<Collider2D>();
    private float dir;
    private float startX;
    private float distance;
    private float elapsed;
    private float duration;
    private bool passingEnemy;
    private bool airDodge;
    private float savedGravityScale;

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

        float baseDistance = player.Stats.dodgeDistance;
        duration = player.Stats.dodgeDuration * Mathf.Max(1f, distance / baseDistance);
        passingEnemy = passThrough.Count > 0;

        startX = player.Rb.position.x;
        elapsed = 0f;
        player.ForceInvincible = true;

        airDodge = !player.IsGrounded();
        if (airDodge)
        {
            savedGravityScale = player.Rb.gravityScale;
            player.Rb.gravityScale = 0f;
            player.SetVelocity(0f, 0f);
        }
    }

    public override void Update()
    {
        base.Update();
        elapsed += Time.deltaTime;

        // 적 몸을 통과하는 중에 맞지 않도록 관통 구르기는 끝까지 무적 유지
        if (!passingEnemy && elapsed >= player.Stats.dodgeInvincibleTime)
            player.ForceInvincible = false;

        if (elapsed >= duration)
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
        float t = Mathf.Clamp01(elapsed / duration);
        player.SetVelocity(0f, player.Rb.velocity.y);
        player.Rb.MovePosition(new Vector2(startX + dir * distance * t, player.Rb.position.y));
    }

    public override void Exit()
    {
        base.Exit();
        if (airDodge)
            player.Rb.gravityScale = savedGravityScale;
        player.SetVelocity(0f, player.Rb.velocity.y);
        player.SetIgnoreCollision(passThrough, false);
        player.ForceInvincible = false;
    }
}
