using UnityEngine;

// 공중 상태(Jump, Fall)의 공통 부모. 공중에서도 좌우 이동과 방향 전환 가능
public class PlayerAirState : PlayerState
{
    public PlayerAirState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float xInput = player.Input.MoveX;
        player.SetVelocity(xInput * player.moveSpeed, player.Rb.velocity.y);
        player.CheckFlip(xInput);
    }
}
