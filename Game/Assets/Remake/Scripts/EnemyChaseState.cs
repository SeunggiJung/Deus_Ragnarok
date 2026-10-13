using UnityEngine;

// 추적. 공격 사거리에 들어오면 쿨다운이 끝나는 대로 공격, 너무 멀어지면 대기로 복귀
public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(Monster enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    private bool InAttackRange => enemy.DistanceToTarget <= enemy.Stats.attackRange;

    public override void Update()
    {
        if (enemy.DistanceToTarget > enemy.Stats.loseRange)
        {
            stateMachine.ChangeState(enemy.IdleState);
            return;
        }

        enemy.FaceTarget();

        if (InAttackRange && enemy.IsAttackReady)
        {
            stateMachine.ChangeState(enemy.AttackState);
            return;
        }

        // 사거리 안에서 쿨다운을 기다리는 동안은 제자리 대기 모션
        enemy.Anim.SetBool("moving", !InAttackRange);
    }

    public override void PhysicsUpdate()
    {
        float vx = InAttackRange ? 0f : enemy.DirectionToTarget * enemy.Stats.moveSpeed;
        enemy.SetVelocityX(vx);
    }

    public override void Exit()
    {
        enemy.Anim.SetBool("moving", false);
        enemy.SetVelocityX(0f);
    }
}
