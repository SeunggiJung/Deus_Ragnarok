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

    [Header("Natural Motion")]
    [Tooltip("몬스터마다 이동 속도 / 공격 거리 / 공격 간격을 조금씩 다르게 해서 여러 마리가 같은 자리에 겹쳐 같은 타이밍에 움직이지 않도록")]
    [SerializeField] private bool varyMotion = false;
    public float SpeedMultiplier { get; private set; } = 1f;
    private float rangeMultiplier = 1f;

    [Header("Animator")]
    [Tooltip("attack 트리거로 진입하는 애니메이터 공격 상태 이름 (몬스터마다 다름 : Golem 'Attack', Crab 'Enemy Attack 3' 등)")]
    [SerializeField] private string attackStateName = "Attack";
    public string AttackStateName => attackStateName;

    public Rigidbody2D Rb => rb;
    public Animator Anim => anim;
    public EnemyStats Stats => (EnemyStats)stats;
    public BaseCharacter Target { get; private set; }
    // 개체별 공격 사거리 / 공격 직후 추가 대기 (varyMotion일 때만 차이)
    public float AttackRange => Stats.attackRange * rangeMultiplier;
    public float AttackJitter => varyMotion ? UnityEngine.Random.Range(0f, 0.5f) : 0f;
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

        if (varyMotion)
        {
            SpeedMultiplier = UnityEngine.Random.Range(0.9f, 1.2f);
            rangeMultiplier = UnityEngine.Random.Range(0.55f, 1f);   // 멈추는 거리가 제각각이라 한 줄로 겹쳐 서지 않음
        }

        StateMachine = new StateMachine();
        IdleState = new EnemyIdleState(this, StateMachine);
        ChaseState = new EnemyChaseState(this, StateMachine);
        AttackState = new EnemyAttackState(this, StateMachine);
        HitState = new EnemyHitState(this, StateMachine);
        DeadState = new EnemyDeadState(this, StateMachine);
    }

    #region Pass-through (몸끼리 부딪히지 않음)
    // 몬스터끼리 / 플레이어와는 몸이 부딪히지 않음 : 여러 마리가 서로 막아 1대1로만 싸우게 되거나, 플레이어가 몬스터에 막혀
    // 구르기 없이는 지나가지 못하는 문제. 공격 / 피격 판정은 Overlap 질의라 서로 때리고 맞는 것은 그대로
    private static readonly List<Monster> activeMonsters = new List<Monster>();

    private void OnEnable()
    {
        activeMonsters.Add(this);
        StartCoroutine(PassThroughRoutine());
    }

    private void OnDisable()
    {
        activeMonsters.Remove(this);
    }

    private IEnumerator PassThroughRoutine()
    {
        yield return null;   // Awake / Start 이후
        var wait = new WaitForSeconds(0.25f);
        while (true)
        {
            ApplyPassThrough();
            yield return wait;   // 플레이어의 구르기가 끝나며 충돌 무시를 되돌릴 수 있어 주기적으로 다시 적용
        }
    }

    private void ApplyPassThrough()
    {
        var mine = GetComponentsInChildren<Collider2D>();
        var player = FindObjectOfType<PlayerController>();
        if (player != null)
            foreach (var theirs in player.GetComponentsInChildren<Collider2D>())
                foreach (var m in mine)
                    Ignore(m, theirs);

        foreach (var other in activeMonsters)
        {
            if (other == null || other == this) continue;
            foreach (var theirs in other.GetComponentsInChildren<Collider2D>())
                foreach (var m in mine)
                    Ignore(m, theirs);
        }
    }

    // 꺼진 콜라이더(사망한 몬스터 등)는 건너뜀
    private static void Ignore(Collider2D a, Collider2D b)
    {
        if (a == null || b == null || !a.enabled || !b.enabled) return;
        Physics2D.IgnoreCollision(a, b, true);
    }
    #endregion

    protected override bool IsValidStats(CharacterStats s) => s is EnemyStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<EnemyStats>();

    private void Start()
    {
        Target = FindObjectOfType<PlayerController>();
        StateMachine.Initialize(IdleState);
    }

    private void Update()
    {
        if (IsKnockedBack) return;
        StateMachine.CurrentState.Update();
    }

    private void FixedUpdate()
    {
        if (IsKnockedBack)
        {
            // 밀려나는 동안 AI는 멈추고 속도가 점점 줄어듦
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            float remaining = (knockbackUntil - Time.time) / knockbackDuration;
            rb.velocity = new Vector2(knockbackVelocityX * Mathf.Clamp01(remaining), rb.velocity.y);
            return;
        }
        StateMachine.CurrentState.PhysicsUpdate();
    }

    #region Knockback
    // 플레이어 부활 연출처럼 특정 상황에서만 쓰는 밀어내기 (평소 전투에는 넉백이 없음 : 원작에 없는 규칙)
    private float knockbackUntil = float.NegativeInfinity;
    private float knockbackDuration = 1f;
    private float knockbackVelocityX;
    private bool IsKnockedBack => Time.time < knockbackUntil && !IsDead;

    public void Knockback(float directionX, float speed, float duration, float hop)
    {
        if (IsDead) return;
        knockbackDuration = Mathf.Max(0.01f, duration);
        knockbackUntil = Time.time + knockbackDuration;
        knockbackVelocityX = Mathf.Sign(directionX) * speed;
        rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        rb.velocity = new Vector2(knockbackVelocityX, hop);
    }
    #endregion

    #region AI Helpers
    public bool HasTarget => Target != null && !Target.IsDead;

    // 수평 거리 기준 (2D 횡스크롤)
    public float DistanceToTarget => HasTarget
        ? Mathf.Abs(Target.transform.position.x - transform.position.x)
        : float.PositiveInfinity;

    // 플레이어와 거의 겹쳤을 때는 방향을 0으로 → 좌우로 계속 뒤집히며 떨리는 현상 방지
    public float DirectionToTarget
    {
        get
        {
            if (!HasTarget) return 0f;
            float dx = Target.transform.position.x - transform.position.x;
            return Mathf.Abs(dx) < 0.2f ? 0f : Mathf.Sign(dx);
        }
    }

    // 등장(spawn) 모션이 재생되는 동안 : 제자리에서 모션이 끝나길 기다림 (등장 모션 중 미끄러져 이동 / 공격하는 문제 방지)
    public bool IsSpawning
    {
        get
        {
            var clips = anim.GetCurrentAnimatorClipInfo(0);
            return clips.Length > 0 && clips[0].clip != null && clips[0].clip.name.ToLowerInvariant().Contains("spawn");
        }
    }

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
        knockbackUntil = float.NegativeInfinity;

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
        StartCoroutine(PassThroughRoutine());   // ResetCharacter가 코루틴을 모두 끊으므로 다시 시작
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
