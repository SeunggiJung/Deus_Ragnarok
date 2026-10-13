using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 일반 몬스터. 체력/피격/무적 처리는 BaseCharacter, 행동은 FSM(Idle → Chase → Attack, Hit, Dead)으로 처리
public class Monster : BaseCharacter
{
    public StateMachine StateMachine { get; private set; }

    // 몬스터가 죽으면 알림 (경험치 지급 등). 몬스터는 누가 듣는지 모름
    public static event Action<Monster> OnKilled;

    public EnemyIdleState IdleState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    public EnemyHitState HitState { get; private set; }
    public EnemyDeadState DeadState { get; private set; }

    [Header("Facing")]
    [Tooltip("localScale.x가 양수일 때 스프라이트가 오른쪽을 보면 체크")]
    [SerializeField] private bool spriteFacesRight = true;

    public Rigidbody2D Rb => rb;
    public Animator Anim => anim;
    public EnemyStats Stats => (EnemyStats)stats;
    public BaseCharacter Target { get; private set; }
    public float LastAttackTime { get; set; } = float.NegativeInfinity;

    protected override void Awake()
    {
        base.Awake();

        // 기존 애니메이터는 attackSpeed 파라미터를 공격 재생 속도로 사용 (기본값 0이면 멈춤)
        foreach (var p in anim.parameters)
        {
            if (p.name == "attackSpeed" && p.type == AnimatorControllerParameterType.Float)
                anim.SetFloat("attackSpeed", 1f);
        }

        StateMachine = new StateMachine();
        IdleState = new EnemyIdleState(this, StateMachine);
        ChaseState = new EnemyChaseState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
        HitState = new EnemyHitState(this, StateMachine);
        DeadState = new EnemyDeadState(this, StateMachine);
    }

    protected override bool IsValidStats(CharacterStats s) => s is EnemyStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<EnemyStats>();

    private void Start()
    {
        Target = FindObjectOfType<PlayerController>();
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        StateMachine.CurrentState.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.CurrentState.PhysicsUpdate();
    }

    #region AI Helpers
    public bool HasTarget => Target != null && !Target.IsDead;

    // 수평 거리 기준 (2D 횡스크롤)
    public float DistanceToTarget => HasTarget
        ? Mathf.Abs(Target.transform.position.x - transform.position.x)
        : float.PositiveInfinity;

    public float DirectionToTarget => HasTarget
        ? Mathf.Sign(Target.transform.position.x - transform.position.x)
        : 0f;

    public bool IsAttackReady => Time.time - LastAttackTime >= Stats.attackCooldown;

    public void FaceTarget()
    {
        float dir = DirectionToTarget;
        if (dir == 0f) return;

        Vector3 scale = transform.localScale;
        float sign = spriteFacesRight ? dir : -dir;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * sign, scale.y, scale.z);
    }

    // 스스로 움직일 때만 X축 이동을 허용하고, 멈춰 있을 때는 X축을 고정해
    // 플레이어가 밀어도 밀리지 않도록 함 (Y축은 중력을 위해 항상 자유)
    public void SetVelocityX(float x)
    {
        if (x == 0f)
            rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        else
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;

        rb.velocity = new Vector2(x, rb.velocity.y);
    }
    #endregion

    // 공격 애니메이션(Golem_Attack_3 등)의 "Hit" 이벤트에서 호출
    public void Hit()
    {
        if (StateMachine.CurrentState == AttackState)
            AnimationTrigger();
    }

    // 공격 중에는 경직되지 않음 (슈퍼아머) → 맞으면서도 공격을 끝까지 내려침
    protected override void OnDamaged()
    {
        if (StateMachine.CurrentState != AttackState)
            StateMachine.ChangeState(HitState);
    }

    protected override void Die()
    {
        StateMachine.ChangeState(DeadState);
        OnKilled?.Invoke(this);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (stats is EnemyStats s)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, s.detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, s.attackRange);
            Gizmos.color = Color.white;
        }
    }
}
