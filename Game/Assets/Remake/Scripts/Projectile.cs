using UnityEngine;

// 직선으로 날아가는 투사체. 대상 레이어에 닿으면 피격, 벽(Ground)에 닿거나 수명이 끝나면 풀로 반환.
// 대상이 구르기 등으로 무적이면 맞히지 않고 그대로 통과
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Projectile : MonoBehaviour
{
    private static Sprite orbSprite;

    private Rigidbody2D rb;
    private CircleCollider2D circle;
    private SpriteRenderer spriteRenderer;

    private Vector2 direction;
    private float speed;
    private float damage;
    private float lifetime;
    private float elapsed;
    private LayerMask targetMask;
    private LayerMask obstacleMask;
    private bool active;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 40;
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast"); // 구르기/공격 판정 캐스트에 걸리지 않도록
    }

    public void Launch(Vector2 position, Vector2 dir, float speed, float damage, float lifetime,
                       LayerMask targetMask, LayerMask obstacleMask,
                       Sprite sprite, Color color, float radius)
    {
        transform.position = position;
        rb.position = position;
        direction = dir.normalized;
        this.speed = speed;
        this.damage = damage;
        this.lifetime = lifetime;
        this.targetMask = targetMask;
        this.obstacleMask = obstacleMask;
        elapsed = 0f;
        active = true;

        spriteRenderer.sprite = sprite != null ? sprite : GetOrbSprite();
        spriteRenderer.color = color;
        // 진행 방향으로 회전 (오른쪽을 향한 스프라이트 기준, 조준 발사도 자연스럽게 보이도록)
        transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        // 스프라이트 높이를 지름에 맞추고 비율 유지 (길쭉한 에너지탄도 찌그러지지 않도록)
        float diameter = radius * 2f;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        float scale = diameter / spriteSize.y;
        transform.localScale = new Vector3(scale, scale, 1f);
        circle.radius = spriteSize.y * 0.5f;
    }

    private void FixedUpdate()
    {
        if (!active) return;

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        elapsed += Time.fixedDeltaTime;
        if (elapsed >= lifetime)
            Release();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!active) return;

        int otherBit = 1 << other.gameObject.layer;

        if ((obstacleMask.value & otherBit) != 0)
        {
            Release();
            return;
        }

        if ((targetMask.value & otherBit) == 0) return;

        BaseCharacter character = other.GetComponentInParent<BaseCharacter>();
        if (character != null && (character.IsDead || character.IsInvincible))
            return; // 구르기 무적 등 → 통과

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            Release();
        }
    }

    private void Release()
    {
        if (!active) return; // 같은 프레임에 두 번 반환되지 않도록
        active = false;
        ProjectilePool.Instance.Release(this);
    }

    // 가운데가 밝고 바깥으로 갈수록 투명해지는 원형 스프라이트 (별도 에셋 없이 사용)
    private static Sprite GetOrbSprite()
    {
        if (orbSprite != null) return orbSprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                float a = Mathf.Clamp01(1f - d);
                float core = Mathf.Clamp01(1f - d * 2.5f); // 중심부는 흰색에 가깝게
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a * a + core))); // 색은 SpriteRenderer.color로 입힘
            }
        }
        tex.Apply();
        orbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return orbSprite;
    }
}
