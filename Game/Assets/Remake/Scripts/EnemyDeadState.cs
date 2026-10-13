using UnityEngine;

// 사망. 사망 애니메이션 재생 후 deathDelay 뒤 풀로 반환 (스포너 없이 배치된 몬스터는 제거)
public class EnemyDeadState : EnemyState
{
    public EnemyDeadState(Monster enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void Enter()
    {
        enemy.Anim.SetBool("moving", false);
        enemy.Anim.SetTrigger("die");
        enemy.SetVelocityX(0f);

        // 시체가 플레이어 이동/공격을 막지 않도록 충돌 해제 (물리도 꺼서 바닥으로 떨어지지 않게)
        foreach (var col in enemy.GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        enemy.Rb.simulated = false;

        enemy.DespawnAfter(enemy.Stats.deathDelay);
    }
}
