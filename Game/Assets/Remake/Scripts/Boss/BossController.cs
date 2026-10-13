using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 보스. 행동 결정은 Behavior Tree(BossBehaviorTree)가 담당하고, 이 클래스는 트리가 쓰는 기능과 상태를 제공.
// 트리 바깥에서 처리하는 것 : 사망, 그로기(강인도 소진) → 실행 중인 패턴을 Abort로 즉시 끊음
public class BossController : BaseCharacter
{
    public static class AnimParams
    {
        public const string Moving = "moving";
        public const string Melee = "melee";
        public const string Slam = "slam";
        public const string Throw = "throw";
        public const string Grab = "grab";
        public const string Ability = "ability";
        public const string Hit = "hit";
        public const string Die = "die";
    }

    [Header("Facing")]
    [Tooltip("localScale.x가 양수일 때 스프라이트가 오른쪽을 보면 체크")]
    [SerializeField] private bool spriteFacesRight = true;

    public BossStats Stats => (BossStats)stats;
    public Rigidbody2D Rb => rb;
    public Animator Anim => anim;
    public Collider2D Body { get; private set; }

    // 패턴 예고 연출 (몸 색 변화가 잘 안 보이는 보스를 위해 링 / 아이콘 표시). 처음 쓸 때 붙임
    private BossTelegraph telegraph;
    public BossTelegraph Telegraph
    {
        get
        {
            if (telegraph == null)
            {
                telegraph = GetComponent<BossTelegraph>();
                if (telegraph == null) telegraph = gameObject.AddComponent<BossTelegraph>();
            }
            return telegraph;
        }
    }
    public BaseCharacter Target { get; private set; }

    public bool Engaged { get; private set; }
    public bool IsPhase2 { get; private set; }
    public bool IsGuarding { get; set; }
    public bool IsStaggered => Time.time < staggerEndTime;

    // UI 등 외부 구독용
    public event Action OnEngaged;
    public event Action OnPhase2;
    public event Action OnDefeated;

    // 현재 패턴이 애니메이션 Hit 이벤트(Attack_2)에 반응할 처리
    public Action HitEventHandler { get; set; }

    private BTNode tree;
    private float poise;
    private float lastHitTime = float.NegativeInfinity;
    private float staggerEndTime = float.NegativeInfinity;
    private bool wasStaggered;
    private readonly Queue<float> recentHitTimes = new Queue<float>();
    private Color baseTint = Color.white;
    private RigidbodyConstraints2D initialConstraints;

    protected override void Awake()
    {
        base.Awake();
        Body = GetComponent<Collider2D>();
        initialConstraints = rb.constraints;
        poise = Stats.maxPoise;
        tree = BuildTree();
        anim.speed = AnimSpeed;
    }

    // 보스마다 다른 행동 트리를 쓸 수 있도록 (Crab 등은 override)
    protected virtual BTNode BuildTree() => BossBehaviorTree.Build(this);

    protected override bool IsValidStats(CharacterStats s) => s is BossStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<BossStats>();

    private void Start()
    {
        Target = FindObjectOfType<PlayerController>();
    }

    private void Update()
    {
        if (IsDead) return;

        if (IsStaggered)
        {
            SetVelocityX(0f);
            return;
        }
        if (wasStaggered)
        {
            wasStaggered = false;
            RestoreTint();
        }

        if (!Engaged)
        {
            if (Target == null || Target.IsDead || DistanceToTarget > Stats.detectRange) return;
            Engaged = true;
            OnEngaged?.Invoke();
        }

        if (Target == null || Target.IsDead)
        {
            tree.Abort();
            StopMoving();
            return;
        }

        // 한동안 맞지 않으면 강인도 회복
        if (Time.time - lastHitTime >= Stats.poiseRegenDelay)
            poise = Stats.maxPoise;

        tree.Tick();
    }

    #region Helpers for BT
    public float CooldownScale => IsPhase2 ? Stats.phase2CooldownMultiplier : 1f;
    public float MoveSpeed => Stats.moveSpeed * (IsPhase2 ? Stats.phase2MoveMultiplier : 1f);
    public float AnimSpeed => IsPhase2 ? Stats.phase2AnimSpeed : Stats.baseAnimSpeed;

    public float DistanceToTarget => Target != null
        ? Mathf.Abs(Target.transform.position.x - transform.position.x)
        : float.PositiveInfinity;

    public float DirectionToTarget => Target != null
        ? Mathf.Sign(Target.transform.position.x - transform.position.x)
        : 0f;

    public float FacingDir => Mathf.Sign(transform.localScale.x) * (spriteFacesRight ? 1f : -1f);

    public void FaceTarget() => Face(DirectionToTarget);

    public void Face(float dir)
    {
        if (dir == 0f) return;
        Vector3 s = transform.localScale;
        float sign = spriteFacesRight ? dir : -dir;
        transform.localScale = new Vector3(Mathf.Abs(s.x) * sign, s.y, s.z);
    }

    // 스스로 움직일 때만 X축 이동 허용 (플레이어가 밀어도 밀리지 않도록)
    public void SetVelocityX(float x)
    {
        if (x == 0f)
            rb.constraints |= RigidbodyConstraints2D.FreezePositionX;
        else
            rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        rb.velocity = new Vector2(x, rb.velocity.y);
    }

    public void StopMoving()
    {
        anim.SetBool(AnimParams.Moving, false);
        SetVelocityX(0f);
    }

    public void PlayAction(string trigger)
    {
        StopMoving();
        anim.SetTrigger(trigger);
    }

    public bool IsPressured()
    {
        while (recentHitTimes.Count > 0 && Time.time - recentHitTimes.Peek() > Stats.guardWindow)
            recentHitTimes.Dequeue();
        return recentHitTimes.Count >= Stats.guardHitCount;
    }

    public void ClearPressure() => recentHitTimes.Clear();

    public bool NeedsPhase2 => !IsPhase2 && CurrentHp <= MaxHp * Stats.phase2Threshold;

    public void EnterPhase2()
    {
        IsPhase2 = true;
        baseTint = Stats.phase2Tint;
        anim.speed = AnimSpeed;
        RestoreTint();
        OnPhase2?.Invoke();
        Debug.Log($"[Boss] {Stats.bossName} 2페이즈 진입");
    }

    public void RestoreTint() => SetTint(baseTint);

    // 근접 판정 (Attack_2의 Hit 이벤트)
    public void MeleeHit() => AnimationTrigger();

    // 범위 판정 : 중심/크기 박스 안의 대상에게 한 번씩 피해
    public void DamageArea(Vector2 center, Vector2 size, float damage)
    {
        var damaged = new HashSet<IDamageable>();
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f, whatIsEnemy))
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null && !ReferenceEquals(target, this) && damaged.Add(target))
                target.TakeDamage(damage);
        }
    }

    public Vector2 FeetPosition => new Vector2(Body.bounds.center.x, Body.bounds.min.y);
    #endregion

    // 애니메이션 이벤트 (Golem_Reinforced_Attack_2의 Hit)
    public void Hit() => HitEventHandler?.Invoke();

    public override void TakeDamage(float damage)
    {
        if (IsGuarding) damage *= Stats.guardDamageMultiplier;
        pendingDamage = damage;
        base.TakeDamage(damage);
    }
    private float pendingDamage;

    protected override void OnDamaged()
    {
        lastHitTime = Time.time;
        recentHitTimes.Enqueue(Time.time);

        // 방어 중에는 강인도가 깎이지 않음
        if (IsGuarding) return;

        poise -= pendingDamage;
        if (poise <= 0f)
            Stagger();
    }

    // 그로기 : 실행 중인 패턴을 즉시 끊고 staggerTime 동안 무방비
    private void Stagger()
    {
        tree.Abort();
        poise = Stats.maxPoise;
        staggerEndTime = Time.time + Stats.staggerTime;
        wasStaggered = true;
        IsGuarding = false;
        StopMoving();
        anim.speed = AnimSpeed;
        anim.SetTrigger(AnimParams.Hit);
        SetTint(Stats.staggerTint);
        Debug.Log($"[Boss] 그로기! ({Stats.staggerTime}초)");
    }

    protected override void Die()
    {
        tree.Abort();
        HitEventHandler = null;
        IsGuarding = false;
        anim.speed = 1f;
        anim.SetBool(AnimParams.Moving, false);

        // 남아있는 행동 트리거가 있으면 Any State 전이로 Die에서 다시 빠져나가 시체가 서 있는 문제가 생김 → 모두 해제
        foreach (string trigger in new[] { AnimParams.Melee, AnimParams.Slam, AnimParams.Throw, AnimParams.Grab, AnimParams.Ability, AnimParams.Hit })
            anim.ResetTrigger(trigger);
        anim.SetTrigger(AnimParams.Die);
        rb.velocity = Vector2.zero;

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        rb.simulated = false;

        CombatEvents.RaiseEnemyKilled(this, Stats.expReward);
        OnDefeated?.Invoke();
        Debug.Log($"[Boss] {Stats.bossName} 처치");

        StartCoroutine(DeathSequence());
    }

    // 사망 연출 : 슬로모션 → 무너지는 모션 도중 파편으로 부서짐 → 제거
    private IEnumerator DeathSequence()
    {
        Time.timeScale = Stats.deathSlowMotionScale;
        yield return new WaitForSecondsRealtime(Stats.deathSlowMotionDuration);
        Time.timeScale = 1f;

        yield return new WaitForSeconds(Stats.shatterDelay);

        foreach (var sr in spriteRenderers)
        {
            if (sr == null || !sr.enabled) continue;
            SpriteShatter.Shatter(sr, Stats.shatterGrid, Stats.shatterGrid, Stats.shatterForce, Stats.shardLifetime);
            sr.enabled = false;
        }

        yield return new WaitForSeconds(Stats.shardLifetime + 0.5f);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // 슬로모션 도중 씬이 바뀌거나 제거돼도 시간 배율이 남지 않도록
        if (IsDead) Time.timeScale = 1f;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!(stats is BossStats s)) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, s.detectRange);

        var col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f);
            Vector2 feet = new Vector2(col.bounds.center.x, col.bounds.min.y);
            Gizmos.DrawWireCube(feet + Vector2.up * s.slamArea.y * 0.5f, s.slamArea);
        }
        Gizmos.color = Color.white;
    }
}
