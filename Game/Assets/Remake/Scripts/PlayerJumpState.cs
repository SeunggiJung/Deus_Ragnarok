using UnityEngine;

// 상승 구간. 수직 속도가 0 이하가 되면(정점 도달) Fall로 전환
public class PlayerJumpState : PlayerAirState
{
    public PlayerJumpState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Enter()
    {
        base.Enter();
        player.SetVelocity(player.Rb.velocity.x, player.JumpVelocity);
    }

    public override void Update()
    {
        base.Update();

        if (player.Rb.velocity.y <= 0f)
        {
            stateMachine.ChangeState(player.FallState);
        }
    }
}
