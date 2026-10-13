using UnityEngine;

// 피격 경직. 짧게 멈췄다가 추적으로 복귀
public class EnemyHitState : EnemyState
{
    private float elapsed;

    public EnemyHitState(Monster enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        elapsed = 0f;
        enemy.Anim.SetBool("moving", false);
        enemy.SetVelocityX(0f);
    }

    public override void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= enemy.Stats.hitStunTime)
            stateMachine.ChangeState(enemy.ChaseState);
    }

    public override void PhysicsUpdate()
    {
        enemy.SetVelocityX(0f);
    }
}
