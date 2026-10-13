using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 일반 몬스터. 체력/피격/무적 처리는 BaseCharacter, 행동은 FSM(Idle → Chase → Attack, Hit, Dead)으로 처리
public class Monster : BaseCharacter
{
    public StateMachine StateMachine { get; private set; }

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
    public float FacingDir => Mathf.Sign(transform.localScale.x) * (spriteFacesRight ? 1f : -1f);

    // 스포너(오브젝트 풀)가 지정하는 반환 처리. 없으면(씬에 직접 배치) 사망 후 Destroy
    private Action<Monster> releaseToPool;
    private RigidbodyConstraints2D initialConstraints;
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

        initialConstraints = rb.constraints;

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
            PerformAttack();
    }

    // 실제 공격 방식. 기본은 근접 판정(OverlapCircle), 원거리 몬스터는 투사체 발사로 재정의
    protected virtual void PerformAttack()
    {
        AnimationTrigger();
    }

    #region Pooling
    // 스포너가 풀에서 꺼낼 때 호출 : 위치 지정 후 체력/물리/애니메이터/AI 상태를 처음처럼 초기화
    public void Spawn(Vector3 position, Action<Monster> release)
    {
        releaseToPool = release;
        transform.position = position;

        foreach (var col in GetComponentsInChildren<Collider2D>(true))
            col.enabled = true;
        rb.simulated = true;
        rb.velocity = Vector2.zero;
        rb.constraints = initialConstraints;

        anim.Rebind();
        anim.Update(0f);
        foreach (var p in anim.parameters)
        {
            if (p.name == "attackSpeed" && p.type == AnimatorControllerParameterType.Float)
                anim.SetFloat("attackSpeed", 1f);
        }

        ResetCharacter();
        LastAttackTime = float.NegativeInfinity;
        if (Target == null)
            Target = FindObjectOfType<PlayerController>();
        StateMachine.Initialize(IdleState);
    }

    // 사망 연출이 끝난 뒤 풀로 반환 (풀이 없으면 제거)
    public void DespawnAfter(float delay)
    {
        StartCoroutine(DespawnRoutine(delay));
    }

    private IEnumerator DespawnRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (releaseToPool != null)
            releaseToPool(this);
        else
            Destroy(gameObject);
    }
    #endregion

    // 공격 중에는 경직되지 않음 (슈퍼아머) → 맞으면서도 공격을 끝까지 내려침
    protected override void OnDamaged()
    {
        if (StateMachine.CurrentState != AttackState)
            StateMachine.ChangeState(HitState);
    }

    protected override void Die()
    {
        StateMachine.ChangeState(DeadState);
        CombatEvents.RaiseEnemyKilled(this, Stats.expReward); // 몬스터는 누가 경험치를 받는지 모름
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
