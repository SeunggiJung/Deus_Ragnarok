using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 모든 캐릭터(플레이어, 몬스터, 보스)의 공통 조상
public abstract class BaseCharacter : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected CharacterStats stats;
    protected float currentHp;
    public bool IsDead { get; private set; }
    public bool IsInvincible { get; private set; }

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

        if (stats == null)
        {
            Debug.LogError($"{gameObject.name}: CharacterStats가 할당되지 않아 기본값을 사용합니다.", this);
            stats = ScriptableObject.CreateInstance<CharacterStats>();
        }

        currentHp = stats.maxHp;
    }

    public virtual void TakeDamage(float damage)
    {
        // 사망 후 중복 피격, 무적 시간 중 연속 피격 방지
        if (IsDead || IsInvincible) return;

        currentHp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHp}");

        if (currentHp <= 0)
        {
            IsDead = true;
            Die();
            return;
        }

        StartCoroutine(HitRoutine());
    }

    // 피격 시 붉게 번쩍인 뒤, 무적 시간 동안 깜빡임
    private IEnumerator HitRoutine()
    {
        IsInvincible = true;

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
        IsInvincible = false;
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
            Color c = originalColors[i];
            c.a = originalColors[i].a * alpha;
            spriteRenderers[i].color = c;
        }
    }

    private void RestoreSpriteColor()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = originalColors[i];
    }

    // 애니메이션 이벤트에서 호출할 함수
    public virtual void AnimationTrigger()
    {
        if (attackCheck == null) return;

        // 주변의 IDamageable을 가진 오브젝트들을 탐색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, stats.attackRadius, whatIsEnemy);

        // 콜라이더가 여러 개인 대상이 한 번의 공격에 여러 번 맞지 않도록 대상 단위로 중복 제거
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        foreach (var hit in colliders)
        {
            // 상대방이 IDamageable을 가지고 있는지 확인 (인터페이스의 힘!)
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null && !ReferenceEquals(damageable, this) && damaged.Add(damageable))
            {
                damageable.TakeDamage(stats.attackDamage);
            }
        }
    }

    // 에디터 뷰에서 판정 범위를 시각적으로 확인하기 위함
    protected virtual void OnDrawGizmos()
    {
        if (attackCheck != null && stats != null)
            Gizmos.DrawWireSphere(attackCheck.position, stats.attackRadius);
    }
    protected abstract void Die(); // 죽는 동작은 자식 클래스에서 정의
}
