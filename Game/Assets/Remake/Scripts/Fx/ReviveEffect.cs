using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어 부활 연출 : 몸이 빛나며 공중으로 떠오르고 날개가 펼쳐진 뒤, 정점에서 충격파가 퍼지며 주변 몬스터를 밀어냄.
// 연출 동안 입력은 막히고 무적이며, 착지한 뒤에도 잠깐 더 무적 (PlayerController.TryRevive가 시작)
// 날개 / 후광 스프라이트는 별도 이미지 없이 코드로 생성
public class ReviveEffect : MonoBehaviour
{
    private const float RiseTime = 1.0f;          // 떠오르는 시간 (이 동안 날개가 펼쳐지고 빛이 커짐)
    private const float RiseHeight = 0.9f;
    private const float HoldTime = 0.55f;         // 정점에서 충격파를 내고 머무는 시간
    private const float FoldTime = 0.6f;          // 착지하며 날개가 접히고 사라지는 시간
    private const float ExtraInvincibleTime = 2f; // 연출이 끝난 뒤 더 유지되는 무적

    private const float ShockRadius = 5f;
    private const float KnockbackSpeed = 11f;
    private const float KnockbackTime = 0.5f;
    private const float KnockbackHop = 3f;

    private static readonly Color Gold = new Color(1f, 0.9f, 0.5f, 1f);
    private static Sprite wingSprite;
    private static Sprite glowSprite;

    private PlayerController player;
    private Rigidbody2D rb;
    private SpriteRenderer aura;
    private SpriteRenderer wingRight;
    private SpriteRenderer wingLeft;
    private ParticleSystem sparkles;
    private int baseOrder;

    public static void Play(PlayerController player)
    {
        var fx = player.GetComponent<ReviveEffect>();
        if (fx == null) fx = player.gameObject.AddComponent<ReviveEffect>();
        fx.player = player;
        fx.rb = player.Rb;
        fx.StopAllCoroutines();
        fx.StartCoroutine(fx.Run());
    }

    private Vector2 Center => player.BodyCollider.bounds.center;

    private IEnumerator Run()
    {
        float gravity = rb.gravityScale;
        player.InputLocked = true;
        player.ForceInvincible = true;
        rb.gravityScale = 0f;
        rb.velocity = Vector2.zero;

        Build();
        Vector2 startPos = rb.position;

        // 1. 빛나며 떠오르고 날개가 펼쳐짐
        float t = 0f;
        while (t < RiseTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / RiseTime);
            float rise = 1f - (1f - p) * (1f - p);
            rb.position = startPos + Vector2.up * (RiseHeight * rise);
            rb.velocity = Vector2.zero;
            player.ForceInvincible = true;

            Pose(unfold: EaseOutBack(p), flap: 0f, alpha: 1f);
            SetAura(0.3f + 0.7f * p, 1f + 0.15f * Mathf.Sin(Time.time * 10f));
            yield return null;
        }

        // 2. 정점 : 충격파 + 주변 몬스터 밀어내기
        Shockwave();
        t = 0f;
        while (t < HoldTime)
        {
            t += Time.deltaTime;
            rb.position = startPos + Vector2.up * RiseHeight;
            rb.velocity = Vector2.zero;
            player.ForceInvincible = true;

            Pose(unfold: 1f, flap: Mathf.Sin(t * 14f) * (1f - t / HoldTime), alpha: 1f);
            SetAura(1f, 1f + 0.1f * Mathf.Sin(Time.time * 10f));
            yield return null;
        }

        // 3. 내려앉으며 날개가 접힘 (이 시점부터 조작 가능)
        rb.gravityScale = gravity;
        player.InputLocked = false;
        StopSparkles();
        t = 0f;
        while (t < FoldTime)
        {
            t += Time.deltaTime;
            float p = t / FoldTime;
            player.ForceInvincible = true;
            Pose(unfold: 1f - p, flap: 0f, alpha: 1f - p);
            SetAura(1f - 0.6f * p, 1f);
            yield return null;
        }
        DestroyWings();

        // 4. 남은 무적 : 후광이 깜빡이며 약해짐
        t = 0f;
        while (t < ExtraInvincibleTime)
        {
            t += Time.deltaTime;
            player.ForceInvincible = true;
            float blink = 0.25f + 0.15f * Mathf.Sin(Time.time * 18f);
            SetAura(blink * (1f - t / ExtraInvincibleTime), 1f);
            yield return null;
        }

        player.ForceInvincible = false;
        if (aura != null) Destroy(aura.gameObject);
    }

    private void OnDestroy()
    {
        // 연출 도중 씬이 바뀌거나 오브젝트가 제거될 때 남은 오브젝트 정리
        DestroyWings();
        if (aura != null) Destroy(aura.gameObject);
    }

    #region Visuals
    private void Build()
    {
        baseOrder = int.MaxValue;
        foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>())
            baseOrder = Mathf.Min(baseOrder, sr.sortingOrder);
        if (baseOrder == int.MaxValue) baseOrder = 0;

        DestroyWings();
        if (aura != null) Destroy(aura.gameObject);

        aura = NewRenderer("ReviveAura", GetGlow(), baseOrder - 2);
        wingRight = NewRenderer("ReviveWingR", GetWing(), baseOrder - 1);
        wingLeft = NewRenderer("ReviveWingL", GetWing(), baseOrder - 1);
        wingLeft.flipX = true;

        // 몸 주변에서 위로 피어오르는 빛 입자
        sparkles = Fx.Gather(Gold, 1.6f, 0.9f, 28f, 0.14f);
    }

    private static SpriteRenderer NewRenderer(string objectName, Sprite sprite, int order)
    {
        var go = new GameObject(objectName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // unfold : 0 접힘 ~ 1 펼침(오버슈트 가능), flap : 퍼덕임 각도 비율
    private void Pose(float unfold, float flap, float alpha)
    {
        if (wingRight == null) return;

        Vector2 shoulder = Center + new Vector2(0f, 0.2f);
        float fold = Mathf.Clamp01(1f - unfold);
        float angle = Mathf.Lerp(0f, 55f, fold) + flap * 8f;
        Vector3 scale = new Vector3(Mathf.Lerp(1f, 0.25f, fold), Mathf.Lerp(1f, 0.5f, fold), 1f) * Mathf.Max(1f, unfold); // 펼칠 때 살짝 오버슈트

        wingRight.transform.position = shoulder + new Vector2(0.08f, 0f);
        wingRight.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        wingRight.transform.localScale = scale;
        wingLeft.transform.position = shoulder + new Vector2(-0.08f, 0f);
        wingLeft.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
        wingLeft.transform.localScale = scale;

        Color c = new Color(1f, 1f, 1f, alpha * Mathf.Clamp01(unfold * 3f));
        wingRight.color = wingLeft.color = c;

        if (sparkles != null) sparkles.transform.position = Center;
    }

    private void SetAura(float alpha, float scale)
    {
        if (aura == null) return;
        aura.transform.position = Center;
        aura.transform.localScale = Vector3.one * 3.4f * scale;
        aura.color = new Color(1f, 0.93f, 0.6f, Mathf.Clamp01(alpha));
    }

    private void StopSparkles()
    {
        if (sparkles == null) return;
        sparkles.Stop();
        Destroy(sparkles.gameObject, 1.5f);
        sparkles = null;
    }

    private void DestroyWings()
    {
        if (wingRight != null) Destroy(wingRight.gameObject);
        if (wingLeft != null) Destroy(wingLeft.gameObject);
        if (sparkles != null) { Destroy(sparkles.gameObject); sparkles = null; }
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
    #endregion

    #region Shockwave
    private void Shockwave()
    {
        Vector2 center = Center;
        Fx.Ring(center, new Color(1f, 0.95f, 0.6f, 0.9f), 0.8f, ShockRadius * 2f, 0.55f, 0.6f);
        Fx.Ring(center, new Color(1f, 1f, 1f, 0.8f), 0.4f, ShockRadius * 1.4f, 0.4f, 0.6f);
        Fx.Burst(center, Vector2.up, 360f, Gold, 40, 3f, 9f, 0.12f, 0.28f, 0.9f, 0.3f);

        // 주변 몬스터를 플레이어에게서 멀어지는 방향으로 밀어냄 (보스는 제외)
        var pushed = new HashSet<Monster>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, ShockRadius, player.whatIsEnemy))
        {
            Monster monster = hit.GetComponentInParent<Monster>();
            if (monster == null || !pushed.Add(monster)) continue;

            float dx = monster.transform.position.x - center.x;
            float dir = Mathf.Abs(dx) < 0.05f ? (Random.value < 0.5f ? -1f : 1f) : Mathf.Sign(dx);
            float falloff = 1f - 0.4f * Mathf.Clamp01(Mathf.Abs(dx) / ShockRadius);
            monster.Knockback(dir, KnockbackSpeed * falloff, KnockbackTime, KnockbackHop);
        }
        Debug.Log($"[부활] 충격파 : 몬스터 {pushed.Count}마리 밀어냄");
    }
    #endregion

    #region Sprites
    // 흰색에서 금색으로 번지는 깃털 날개 (오른쪽 날개. 왼쪽은 flipX). 피벗은 어깨 쪽 뿌리
    private static Sprite GetWing()
    {
        if (wingSprite != null) return wingSprite;

        const int w = 256, h = 210;
        var origin = new Vector2(10f, 60f);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[w * h];
        var gold = new Color(1f, 0.88f, 0.55f);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float dx = x - origin.x, dy = y - origin.y;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                Color c = new Color(1f, 1f, 1f, 0f);

                if (r > 6f && a > -30f && a < 100f)
                {
                    float t = Mathf.InverseLerp(100f, -30f, a);                       // 0 위쪽 ~ 1 아래쪽
                    float profile = Mathf.Sin(Mathf.Clamp01(t * 0.85f + 0.08f) * Mathf.PI);
                    float longest = 80f + 150f * profile;                              // 가운데 깃털이 가장 김
                    float f = t * 8f;                                                  // 깃털 8장
                    float frac = f - Mathf.Floor(f);
                    float rMax = longest * (1f - 0.12f * (1f - Mathf.Sin(frac * Mathf.PI))); // 깃털 사이는 살짝 파임

                    if (r <= rMax)
                    {
                        float tip = Mathf.Clamp01((rMax - r) / 4f);
                        float side = Mathf.Clamp01(Mathf.Min(a + 30f, 100f - a) / 3f);
                        float k = r / rMax;
                        Color body = Color.Lerp(Color.white, gold, k * k);
                        float seam = (frac < 0.05f || frac > 0.95f) && r > 40f ? 0.82f : 1f; // 깃털 경계선
                        c = new Color(body.r * seam, body.g * seam, body.b * seam, tip * side);
                    }
                }
                pixels[y * w + x] = c;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        wingSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(origin.x / w, origin.y / h), 170f);
        return wingSprite;
    }

    // 가운데가 밝고 가장자리로 부드럽게 사라지는 둥근 빛
    private static Sprite GetGlow()
    {
        if (glowSprite != null) return glowSprite;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2f) * 0.85f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return glowSprite;
    }
    #endregion
}
