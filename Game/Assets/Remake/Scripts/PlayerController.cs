using System;
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
    public PlayerJumpAttackState JumpAttackState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerFallState FallState { get; private set; }
    public PlayerDodgeState DodgeState { get; private set; }
    public PlayerHitState HitState { get; private set; }
    public PlayerDeadState DeadState { get; private set; }

    [Header("Input")]
    [SerializeField] private PlayerInput input = new PlayerInput();
    public PlayerInput Input => input;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundCheckDistance = 0.05f;

    public Rigidbody2D Rb => rb;
    public Animator Anim => anim;
    public PlayerStats Stats => (PlayerStats)stats;
    public CapsuleCollider2D BodyCollider { get; private set; }

    // 바라보는 방향 (localScale.x = 1 : 왼쪽, -1 : 오른쪽)
    public float FacingDir => transform.localScale.x < 0 ? 1f : -1f;

    // 체공 중 점프 공격 가능 여부 (착지 시 다시 true)
    public bool CanAirAttack { get; set; } = true;

    // 스태미너 (UI 등 외부에서 구독 : 현재값, 최대값)
    public float CurrentStamina { get; private set; }
    public event Action<float, float> OnStaminaChanged;
    private float lastStaminaUseTime;

    protected override void Awake()
    {
        base.Awake();
        BodyCollider = GetComponent<CapsuleCollider2D>();
        if (whatIsGround.value == 0)
            whatIsGround = LayerMask.GetMask("Ground");

        CurrentStamina = Stats.maxStamina;

        StateMachine = new StateMachine();
        // 상태 객체 생성 (Jump와 Fall은 같은 애니메이션을 공유)
        IdleState = new PlayerIdleState(this, StateMachine, "Idle");
        MoveState = new PlayerMoveState(this, StateMachine, "Move");
        AttackState = new PlayerAttackState(this, StateMachine, "Attack");
        JumpAttackState = new PlayerJumpAttackState(this, StateMachine, "Attack");
        JumpState = new PlayerJumpState(this, StateMachine, "Jump");
        FallState = new PlayerFallState(this, StateMachine, "Jump");
        DodgeState = new PlayerDodgeState(this, StateMachine, "Dodge");
        HitState = new PlayerHitState(this, StateMachine, "Idle");
        DeadState = new PlayerDeadState(this, StateMachine, "die");
    }

    protected override bool IsValidStats(CharacterStats s) => s is PlayerStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<PlayerStats>();

    private void Start()
    {
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        if (IsDead) input.Clear();
        else input.Read();

        RegenStamina();
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

    // 캡슐 콜라이더 바닥면 바로 아래에 Ground 레이어가 있는지 검사
    public bool IsGrounded()
    {
        Bounds b = BodyCollider.bounds;
        Vector2 boxCenter = new Vector2(b.center.x, b.min.y);
        Vector2 boxSize = new Vector2(b.size.x * 0.8f, groundCheckDistance * 2f);
        return Physics2D.OverlapBox(boxCenter, boxSize, 0f, whatIsGround) != null;
    }

    // 진입 직후에는 애니메이터가 아직 이전 상태(Idle 등)를 재생 중이므로
    // 실제로 Attack 상태에 들어온 뒤의 재생률만 보고 종료 판정
    public bool IsAttackAnimationFinished()
    {
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        return !anim.IsInTransition(0) && info.IsName("Attack") && info.normalizedTime >= 1.0f;
    }

    // 점프 공격은 크리티컬
    protected override float GetAttackDamage()
    {
        if (StateMachine.CurrentState == JumpAttackState)
        {
            float damage = stats.attackDamage * Stats.criticalMultiplier;
            Debug.Log($"크리티컬! (점프 공격 x{Stats.criticalMultiplier}) 데미지 {damage}");
            return damage;
        }
        return stats.attackDamage;
    }

    #region Stamina
    // 스태미너가 충분하면 소모하고 true, 부족하면 소모하지 않고 false
    public bool TryUseStamina(float cost)
    {
        if (CurrentStamina < cost) return false;

        CurrentStamina -= cost;
        lastStaminaUseTime = Time.time;
        OnStaminaChanged?.Invoke(CurrentStamina, Stats.maxStamina);
        return true;
    }

    private void RegenStamina()
    {
        if (IsDead || CurrentStamina >= Stats.maxStamina) return;
        if (Time.time - lastStaminaUseTime < Stats.staminaRegenDelay) return;

        CurrentStamina = Mathf.Min(Stats.maxStamina, CurrentStamina + Stats.staminaRegen * Time.deltaTime);
        OnStaminaChanged?.Invoke(CurrentStamina, Stats.maxStamina);
    }
    #endregion

    #region Dodge
    // 구르기 경로를 미리 계산해 실제 이동 거리를 반환.
    // - 벽(Ground)이 있으면 벽 앞까지로 거리 제한
    // - 경로상의 적은 구르기 거리 안에 완전히 통과할 수 있으면 관통(passThrough에 추가),
    //   통과할 수 없으면 그 적 앞에서 멈추도록 거리 제한
    public float PlanDodge(float dir, List<Collider2D> passThrough)
    {
        passThrough.Clear();

        Bounds b = BodyCollider.bounds;
        // 바닥과 닿지 않도록 높이를 살짝 줄인 몸통 크기로 수평 캐스트
        Vector2 castSize = new Vector2(b.size.x, b.size.y * 0.9f);
        Vector2 castDir = new Vector2(dir, 0f);
        float distance = Stats.dodgeDistance;

        // 1. 벽 체크
        RaycastHit2D wallHit = Physics2D.BoxCast(b.center, castSize, 0f, castDir, distance, whatIsGround);
        if (wallHit.collider != null)
            distance = wallHit.distance;

        // 2. 경로상의 적 체크 (가까운 순서로)
        RaycastHit2D[] enemyHits = Physics2D.BoxCastAll(b.center, castSize, 0f, castDir, distance, whatIsEnemy);
        Array.Sort(enemyHits, (a, c) => a.distance.CompareTo(c.distance));

        // 플레이어의 뒤쪽 끝 (구르기 방향의 반대편 가장자리)
        float playerBackEdge = dir > 0 ? b.min.x : b.max.x;

        foreach (var hit in enemyHits)
        {
            BaseCharacter enemy = hit.collider.GetComponentInParent<BaseCharacter>();
            if (enemy != null && enemy.IsDead)
            {
                passThrough.Add(hit.collider); // 죽은 적은 막지 않음
                continue;
            }

            // 적의 먼 쪽 가장자리까지 플레이어 몸 전체가 넘어가려면 필요한 이동 거리
            Bounds e = hit.collider.bounds;
            float enemyFarEdge = dir > 0 ? e.max.x : e.min.x;
            float clearDistance = Mathf.Abs(enemyFarEdge - playerBackEdge);

            if (clearDistance <= distance)
            {
                passThrough.Add(hit.collider);  // 끝까지 구르면 완전히 빠져나감 → 관통
                Debug.Log($"[구르기] {hit.collider.name} 관통 : 빠져나가는 데 필요한 거리 {clearDistance:F2} ≤ 구르기 거리 {distance:F2}");
            }
            else
            {
                Debug.Log($"[구르기] {hit.collider.name} 앞에서 정지 : 빠져나가는 데 필요한 거리 {clearDistance:F2} > 구르기 거리 {distance:F2} → {hit.distance:F2}만 이동");
                distance = hit.distance;        // 다 못 빠져나감 → 적 앞에서 정지
                break;
            }
        }

        // Scene 뷰 확인용 기록 (초록 : 관통한 적, 빨강 : 정지한 위치)
        lastDodgeStart = b.center;
        lastDodgeEnd = b.center + (Vector3)(castDir * distance);
        lastDodgeSize = castSize;
        lastDodgeTime = Time.time;

        return distance;
    }

    private Vector3 lastDodgeStart, lastDodgeEnd;
    private Vector2 lastDodgeSize;
    private float lastDodgeTime = float.NegativeInfinity;

    public void SetIgnoreCollision(List<Collider2D> colliders, bool ignore)
    {
        foreach (var col in colliders)
        {
            if (col != null)
                Physics2D.IgnoreCollision(BodyCollider, col, ignore);
        }
    }
    #endregion

    protected override void OnDamaged()
    {
        StateMachine.ChangeState(HitState);
    }

    protected override void Die()
    {
        StateMachine.ChangeState(DeadState);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        // 바닥 판정 범위 표시
        CapsuleCollider2D col = BodyCollider != null ? BodyCollider : GetComponent<CapsuleCollider2D>();
        if (col == null) return;
        Bounds b = col.bounds;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector2(b.center.x, b.min.y), new Vector2(b.size.x * 0.8f, groundCheckDistance * 2f));

        // 마지막 구르기 계획 (2초간 표시) : 시작 위치(회색) → 도착 예정 위치(청록)
        if (Application.isPlaying && Time.time - lastDodgeTime < 2f)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(lastDodgeStart, lastDodgeSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(lastDodgeEnd, lastDodgeSize);
            Gizmos.DrawLine(lastDodgeStart, lastDodgeEnd);
        }
        Gizmos.color = Color.white;
    }
}
