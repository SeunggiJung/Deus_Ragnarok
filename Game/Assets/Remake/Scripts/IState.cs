using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    void Enter();       // 상태 진입 시 (애니메이션 재생 등)
    void HandleInput(); // 입력 체크
    void Update();      // 로직 업데이트
    void PhysicsUpdate(); // 물리 연산 (FixedUpdate 대용)
    void Exit();        // 상태 탈출 시
}
