using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 모든 캐릭터(플레이어, 몬스터, 보스)의 공통 조상
public abstract class BaseCharacter : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] protected float maxHp = 100f;
    protected float currentHp;

    [Header("Components")]
    protected Rigidbody2D rb;
    protected Animator anim;
    protected SpriteRenderer spriteRenderer;

    [Header("Attack Info")]
    public Transform attackCheck;      // 판정 중심점 (캐릭터 앞쪽 빈 오브젝트)
    public float attackCheckRadius;    // 판정 범위 반지름
    public LayerMask whatIsEnemy;      // 공격 대상 레이어 (Monster 또는 Player)

    protected virtual void Awake()
    {
        // 공통 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentHp = maxHp;
    }

    public virtual void TakeDamage(float damage, Vector2 knockbackForce)
    {
        currentHp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHp}");

        // 넉백 로직 (물리)
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        if (currentHp <= 0) Die();
    }
    // 애니메이션 이벤트에서 호출할 함수
    public virtual void AnimationTrigger()
    {
        // 주변의 IDamageable을 가진 오브젝트들을 탐색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(attackCheck.position, attackCheckRadius, whatIsEnemy);

        foreach (var hit in colliders)
        {
            // 상대방이 IDamageable을 가지고 있는지 확인 (인터페이스의 힘!)
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable != null)
            {
                // 데미지 전달 (공격 방향에 따른 넉백 포함)
                Vector2 knockback = new Vector2(transform.localScale.x * 2f, 1f);
                damageable.TakeDamage(10f, knockback);
            }
        }
    }

    // 에디터 뷰에서 판정 범위를 시각적으로 확인하기 위함
    protected virtual void OnDrawGizmos()
    {
        if (attackCheck != null)
            Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
    }
    protected abstract void Die(); // 죽는 동작은 자식 클래스에서 정의
}
