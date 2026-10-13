using UnityEngine;

// 땅 위에 있는 상태(Idle, Move)의 공통 부모. 낙하/구르기/점프/공격 전환을 한 곳에서 처리
public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void HandleInput()
    {
        PlayerInput input = player.Input;
        PlayerStats stats = player.Stats;

        // 한 프레임에 상태가 두 번 바뀌지 않도록 우선순위 순으로 else if
        // 스태미너가 부족하면 해당 행동은 무시
        if (!player.IsGrounded())
        {
            stateMachine.ChangeState(player.FallState); // 발판 끝에서 걸어 내려간 경우
        }
        else if (input.DodgePressed && player.TryUseStamina(stats.dodgeCost))
        {
            stateMachine.ChangeState(player.DodgeState);
        }
        else if (input.JumpPressed && player.TryUseStamina(stats.jumpCost))
        {
            stateMachine.ChangeState(player.JumpState);
        }
        else if (input.AttackPressed && player.TryUseStamina(stats.attackCost))
        {
            stateMachine.ChangeState(player.AttackState);
        }
    }
}
