using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 모든 캐릭터(플레이어, 몬스터, 보스)의 공통 조상
public abstract class BaseCharacter : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected CharacterStats stats;
    protected float currentHp;
    public float CurrentHp => currentHp;
    // 레벨업 등으로 얻은 추가 수치. Stats 에셋(SO)은 여러 캐릭터가 공유하는 원본이므로 직접 수정하지 않고 여기에 더함
    protected float bonusMaxHp;
    protected float bonusAttack;
    public float BonusMaxHp => bonusMaxHp;
    public float BonusAttack => bonusAttack;
    public float MaxHp => stats.maxHp + bonusMaxHp;
    public float AttackDamage => stats.attackDamage + bonusAttack;
    public bool IsDead { get; private set; }

    // 피격 후 무적(hitInvincible)과 구르기 등 상태에 의한 무적(ForceInvincible)을 분리해
    // 한쪽이 끝나면서 다른 쪽 무적까지 풀어버리는 일이 없도록 함
    private bool hitInvincible;
    public bool ForceInvincible { get; set; }
    public bool IsInvincible => hitInvincible || ForceInvincible;

    // UI 등 외부에서 구독 (현재 체력, 최대 체력)
    public event Action<float, float> OnHpChanged;
    // 실제로 피해를 받았을 때 (받은 피해량). 무적 등으로 무시된 공격은 호출되지 않음
    public event Action<float> DamageTaken;
    // 사망 시 (보스방 문, 포탈 잠금 해제 등 외부에서 구독)
    public event Action OnDied;

    [Header("Components")]
    protected Rigidbody2D rb;
    protected Animator anim;
    protected SpriteRenderer[] spriteRenderers;

    [Header("Attack Info")]
    public Transform attackCheck;      // 판정 중심점 (캐릭터 앞쪽 빈 오브젝트)
    public LayerMask whatIsEnemy;      // 공격 대상 레이어 (Monster 또는 Player)

    [Header("Hit Feedback")]
    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float hitFlashTime = 0.1f;
    [SerializeField] private float blinkInterval = 0.08f;

    private Color[] originalColors;
    // 상태 연출용 색 (보스 돌진 예고, 방어 자세 등). 피격 플래시가 끝나면 이 색으로 복귀
    private Color tint = Color.white;

    protected virtual void Awake()
    {
        // 공통 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        // 플레이어처럼 스프라이트가 자식(body, Leg 등)에 나뉘어 있는 경우도 있어 자식까지 모두 수집
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;

        // 인스펙터에서 연결하지 않았다면 이름이 AttackCheck인 자식을 사용
        if (attackCheck == null)
            attackCheck = transform.Find("AttackCheck");

        if (!IsValidStats(stats))
        {
            Debug.LogError($"{gameObject.name}: 알맞은 Stats 에셋이 할당되지 않아 기본값을 사용합니다.", this);
            stats = CreateDefaultStats();
        }

        currentHp = MaxHp;
    }

    // 자식 클래스가 PlayerStats / EnemyStats 등 자신에게 맞는 타입을 요구할 수 있도록
    protected virtual bool IsValidStats(CharacterStats s) => s != null;
    protected virtual CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<CharacterStats>();

    public virtual void TakeDamage(float damage)
    {
        // 사망 후 중복 피격, 무적 시간 중 연속 피격 방지
        if (IsDead || IsInvincible) return;

        currentHp = Mathf.Max(0f, currentHp - damage);
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHp}");
        OnHpChanged?.Invoke(currentHp, MaxHp);
        DamageTaken?.Invoke(damage);
        DamagePopup.Show(this, damage, DamagePopup.NextIsCritical);
        GameAudio.PlayDamaged(this, DamagePopup.NextIsCritical);

        if (currentHp <= 0)
        {
            IsDead = true;
            GameAudio.PlayDeath(this);
            // 진행 중인 피격 연출을 정리하고 원래 색으로 복구
            StopAllCoroutines();
            RestoreSpriteColor();
            hitInvincible = false;
            Die();
            OnDied?.Invoke();
            return;
        }

        StartCoroutine(HitRoutine());
        OnDamaged();
    }

    // 피격 후 살아있을 때 호출 (경직 상태 전환 등은 자식 클래스에서)
    protected virtual void OnDamaged() { }

    // 피격 시 붉게 번쩍인 뒤, 무적 시간 동안 깜빡임
    private IEnumerator HitRoutine()
    {
        hitInvincible = true;

        SetSpriteColor(hitColor);
        yield return new WaitForSeconds(hitFlashTime);

        float elapsed = hitFlashTime;
        bool visible = false;
        while (elapsed < stats.invincibleTime)
        {
            SetSpriteAlpha(visible ? 1f : 0.4f);
            visible = !visible;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        RestoreSpriteColor();
        hitInvincible = false;
    }

    private void SetSpriteColor(Color color)
    {
        foreach (var sr in spriteRenderers)
            sr.color = color;
    }

    private void SetSpriteAlpha(float alpha)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Color c = originalColors[i] * tint;
            c.a *= alpha;
            spriteRenderers[i].color = c;
        }
    }

    private void RestoreSpriteColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = originalColors[i] * tint;
    }

    // 현재 공격의 데미지 (크리티컬 등 상황별 배율은 자식 클래스에서)
    protected virtual float GetAttackDamage() => AttackDamage;

    // 크리티컬 공격인지 (피해량 표시를 노란색으로)
    protected virtual bool IsCriticalHit() => false;

    // 상태 연출용 색 지정 (피격 플래시 중이면 플래시가 끝난 뒤 적용됨)
    public void SetTint(Color color)
    {
        tint = color;
        if (!hitInvincible)
            RestoreSpriteColor();
    }

    // 오브젝트 풀에서 재사용할 때 상태 초기화 (체력, 사망/무적 상태, 피격 연출)
    protected virtual void ResetCharacter()
    {
        StopAllCoroutines();
        IsDead = false;
        hitInvincible = false;
        ForceInvincible = false;
        tint = Color.white;
        RestoreSpriteColor();
        currentHp = MaxHp;
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    // 최대 체력 증가 (늘어난 만큼 현재 체력도 회복)
    public void AddMaxHp(float amount)
    {
        bonusMaxHp += amount;
        currentHp += amount;
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    public void AddAttack(float amount)
    {
        bonusAttack += amount;
    }

    // 체력 회복 (최대 체력을 넘지 않음, 사망 시 무시)
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHp = Mathf.Min(MaxHp, currentHp + amount);
        OnHpChanged?.Invoke(currentHp, MaxHp);
    }

    // 애니메이션 이벤트에서 호출할 함수
    public virtual void AnimationTrigger()
    {
        if (attackCheck == null) return;

        // 주변의 IDamageable을 가진 오브젝트들을 탐색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, stats.attackRadius, whatIsEnemy);

        // 콜라이더가 여러 개인 대상이 한 번의 공격에 여러 번 맞지 않도록 대상 단위로 중복 제거
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        DamagePopup.NextIsCritical = IsCriticalHit();
        foreach (var hit in colliders)
        {
            // 상대방이 IDamageable을 가지고 있는지 확인 (인터페이스의 힘!)
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null && !ReferenceEquals(damageable, this) && damaged.Add(damageable))
            {
                damageable.TakeDamage(GetAttackDamage());
            }
        }
        DamagePopup.NextIsCritical = false;
    }

    // 에디터 뷰에서 판정 범위를 시각적으로 확인하기 위함
    protected virtual void OnDrawGizmos()
    {
        if (attackCheck != null && stats != null)
            Gizmos.DrawWireSphere(attackCheck.position, stats.attackRadius);
    }
    protected abstract void Die(); // 죽는 동작은 자식 클래스에서 정의
}
