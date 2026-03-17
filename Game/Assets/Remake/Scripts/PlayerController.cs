using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : BaseCharacter
{
    public StateMachine StateMachine { get; private set; }

    // 플레이어의 각 상태들
    public PlayerMoveState MoveState { get; private set; }
    public PlayerIdleState IdleState { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public Rigidbody2D Rb => rb;
    protected override void Awake()
    {
        base.Awake();
        StateMachine = new StateMachine();

        // 상태 객체 생성 (이후 구현)
        IdleState = new PlayerIdleState(this, StateMachine, "Idle");
        MoveState = new PlayerMoveState(this, StateMachine, "Move");
    }

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentState.HandleInput();
        StateMachine.CurrentState.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }
    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
    }

    public void CheckFlip(float _xInput)
    {
        if (_xInput > 0 && transform.localScale.x > 0)
            transform.localScale = new Vector3(-1, 1, 1);
        else if (_xInput < 0 && transform.localScale.x < 0)
            transform.localScale = new Vector3(1, 1, 1);
    }
    protected override void Die() { /* 사망 로직 */ }
}
