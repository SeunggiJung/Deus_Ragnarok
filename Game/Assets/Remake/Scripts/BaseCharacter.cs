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

    protected virtual void Awake()
    {
        // 공통 컴포넌트 초기화
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        currentHp = maxHp;
    }

    // IDamageable 인터페이스 구현
    public virtual void TakeDamage(float damage, Vector2 knockbackForce)
    {
        currentHp -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {currentHp}");

        // 넉백 로직 (물리)
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        if (currentHp <= 0) Die();
    }

    protected abstract void Die(); // 죽는 동작은 자식 클래스에서 정의
}
