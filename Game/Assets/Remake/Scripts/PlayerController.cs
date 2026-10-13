using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : BaseCharacter
{
    public StateMachine StateMachine { get; private set; }

    // 플레이어의 각 상태들
    public PlayerMoveState MoveState { get; private set; }
    public PlayerIdleState IdleState { get; private set; }
    public PlayerAttackState AttackState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckDistance = 0.05f;

    public Rigidbody2D Rb => rb;
    public Animator Anim => anim;
    private CapsuleCollider2D bodyCollider;

    protected override void Awake()
    {
        base.Awake();
        bodyCollider = GetComponent<CapsuleCollider2D>();
        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Ground");

        StateMachine = new StateMachine();
        // 상태 객체 생성 (Jump와 Fall은 같은 애니메이션을 공유)
        IdleState = new PlayerIdleState(this, StateMachine, "Idle");
        MoveState = new PlayerMoveState(this, StateMachine, "Move");
        AttackState = new PlayerAttackState(this, StateMachine, "Attack");
        JumpState = new PlayerJumpState(this, StateMachine, "Jump");
        FallState = new PlayerFallState(this, StateMachine, "Jump");
    }

    // 캡슐 콜라이더 바닥면 바로 아래에 Ground 레이어가 있는지 검사
    public bool IsGrounded()
    {
        Bounds b = bodyCollider.bounds;
        Vector2 boxCenter = new Vector2(b.center.x, b.min.y);
        Vector2 boxSize = new Vector2(b.size.x * 0.8f, groundCheckDistance * 2f);
        return Physics2D.OverlapBox(boxCenter, boxSize, 0f, whatIsGround) != null;
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
    {   //localScale.x = 1 : 왼쪽, -1 : 오른쪽
        if (_xInput > 0 && transform.localScale.x > 0) // 입력이 오른쪽일 때, player character의 방향이 왼쪽이면
            transform.localScale = new Vector3(-1, 1, 1);
        else if (_xInput < 0 && transform.localScale.x < 0) // 입력이 왼쪽일 때, player character의 방향이 오른쪽이면
            transform.localScale = new Vector3(1, 1, 1);
    }
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // 바닥 판정 범위 표시
        CapsuleCollider2D col = bodyCollider != null ? bodyCollider : GetComponent<CapsuleCollider2D>();
        if (col == null) return;
        Bounds b = col.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, groundCheckDistance * 2f));
    }

    protected override void Die() { /* 사망 로직 */ }
}
