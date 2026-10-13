using UnityEngine;

// 공격. attack 트리거로 애니메이션을 재생하고, 실제 판정은 애니메이션의 "Hit" 이벤트에서 처리.
// 애니메이션이 끝나면 쿨다운을 시작하고 추적으로 복귀
public class EnemyAttackState : EnemyState
{
    private const float SafetyTimeout = 3f; // 애니메이터 설정 문제로 Attack 상태에 못 들어가도 갇히지 않도록
    private bool started;
    private float elapsed;

    public EnemyAttackState(Monster enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        started = false;
        elapsed = 0f;
        enemy.FaceTarget();
        enemy.SetVelocityX(0f);
        enemy.Anim.SetTrigger("attack");
    }

    public override void Update()
    {
        elapsed += Time.deltaTime;

        // 플레이어와 같은 방식 : 실제로 Attack 상태에 들어간 뒤 끝났는지 확인
        AnimatorStateInfo info = enemy.Anim.GetCurrentAnimatorStateInfo(0);
        bool inAttack = info.IsName(enemy.AttackStateName);
        if (inAttack) started = true;

        bool finished = started && (!inAttack || info.normalizedTime >= 1f);
        if (finished || elapsed >= SafetyTimeout)
        {
            stateMachine.ChangeState(enemy.ChaseState);
        }
    }

    public override void PhysicsUpdate()
    {
        enemy.SetVelocityX(0f);
    }

    public override void Exit()
    {
        enemy.LastAttackTime = Time.time;
        enemy.Anim.ResetTrigger("attack");
    }
}
