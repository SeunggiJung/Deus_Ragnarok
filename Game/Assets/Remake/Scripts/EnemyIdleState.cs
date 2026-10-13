using UnityEngine;

// 대기. 플레이어가 감지 범위에 들어오면 추적
public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Monster enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        enemy.Anim.SetBool("moving", false);
        enemy.SetVelocityX(0f);
    }

    public override void Update()
    {
        if (enemy.DistanceToTarget <= enemy.Stats.detectRange)
            stateMachine.ChangeState(enemy.ChaseState);
    }
}
