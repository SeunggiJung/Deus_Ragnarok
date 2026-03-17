using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine
{
    public IState CurrentState { get; private set; }

    // 처음 시작할 상태 설정 (예: IdleState)
    public void Initialize(IState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    // 상태 변경 (예: Idle -> Move)
    public void ChangeState(IState newState)
    {
        CurrentState.Exit(); // 이전 상태 종료
        CurrentState = newState;
        CurrentState.Enter(); // 새 상태 시작
    }
}
