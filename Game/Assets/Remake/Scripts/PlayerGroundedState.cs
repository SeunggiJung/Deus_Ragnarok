using UnityEngine;

// 땅 위에 있는 상태(Idle, Move)의 공통 부모. 공격/점프/낙하 전환을 한 곳에서 처리
public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    public override void HandleInput()
    {
        // 한 프레임에 상태가 두 번 바뀌지 않도록 우선순위 순으로 else if
        if (!player.IsGrounded())
        {
            stateMachine.ChangeState(player.FallState); // 발판 끝에서 걸어 내려간 경우
        }
        else if (Input.GetKeyDown(KeyCode.Space)) // 스페이스 점프
        {
            stateMachine.ChangeState(player.JumpState);
        }
        else if (Input.GetMouseButtonDown(0)) // 마우스 좌클릭 공격
        {
            stateMachine.ChangeState(player.AttackState);
        }
    }
}
