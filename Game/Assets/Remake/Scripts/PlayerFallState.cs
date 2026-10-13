using UnityEngine;

// 하강 구간. 착지하면 입력 여부에 따라 Idle / Move로 복귀
public class PlayerFallState : PlayerAirState
{
    public PlayerFallState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void Update()
    {
        base.Update();

        if (player.IsGrounded())
        {
            if (Input.GetAxisRaw("Horizontal") != 0)
                stateMachine.ChangeState(player.MoveState);
            else
                stateMachine.ChangeState(player.IdleState);
        }
    }
}
