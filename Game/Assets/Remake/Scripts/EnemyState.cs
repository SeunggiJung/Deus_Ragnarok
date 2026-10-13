using UnityEngine;

// 몬스터 상태의 공통 부모. 몬스터 애니메이터는 상태마다 파라미터 사용 방식이 달라(moving bool, attack/die trigger)
// 플레이어처럼 animBoolName 하나로 묶지 않고 각 상태에서 직접 제어
public abstract class EnemyState : IState
{
    protected Monster enemy;
    protected StateMachine stateMachine;

    protected EnemyState(Monster enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void HandleInput() { }
    public virtual void Update() { }
    public virtual void PhysicsUpdate() { }
    public virtual void Exit() { }
}
