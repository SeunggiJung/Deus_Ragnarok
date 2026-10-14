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

        Vector2 shoulder = Center + new Vector2(0f, 0.1f);
        float fold = Mathf.Clamp01(1f - unfold);
        float angle = Mathf.Lerp(8f, 50f, fold) + flap * 8f;
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
    // 천사 날개 (오른쪽 날개. 왼쪽은 flipX). 어깨에서 위 / 바깥으로 휘어 오르는 윗선(팔)과, 거기서 아래로 늘어지는
    // 긴 깃털 한 층 + 짧은 덧깃털 한 층. 깃털 끝으로 갈수록 금색을 띠고 가장자리에 옅은 윤곽선을 둠. 피벗은 어깨
    private static Sprite GetWing()
    {
        if (wingSprite != null) return wingSprite;

        const int w = 270, h = 260;
        var origin = new Vector2(14f, 95f);
        var pixels = new Color[w * h];
        const int featherCount = 10;

        // 깃털 : 윗선 위의 한 점에서 아래 바깥쪽으로 뻗는 둥근 막대
        for (int layer = 0; layer < 2; layer++)
        {
            for (int i = 0; i < featherCount; i++)
            {
                float s = layer == 0 ? (i + 0.5f) / featherCount : (i + 1f) / featherCount;
                if (s > 0.98f) continue;

                float angle = Mathf.Lerp(-108f, -32f, s) * Mathf.Deg2Rad;               // 안쪽은 아래로, 바깥쪽은 옆으로
                float length = Mathf.Lerp(70f, 165f, Mathf.Sin(s * Mathf.PI * 0.5f));  // 바깥쪽일수록 김
                if (s > 0.9f) length *= 0.9f;
                if (layer == 1) length *= 0.55f;

                Vector2 from = WingArm(origin, s);
                Vector2 to = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
                float radius = layer == 0 ? 17f : 14f;
                Color tipColor = layer == 0 ? new Color(1f, 0.9f, 0.62f) : new Color(1f, 0.96f, 0.82f);
                PaintCapsule(pixels, w, h, from, to, radius, radius * (layer == 0 ? 0.35f : 0.5f), Color.white, tipColor, true); // 끝이 뾰족한 깃털
            }
        }

        // 윗선(팔) : 맨 위에 덧그려 깃털 뿌리를 가림
        var cream = new Color(1f, 0.97f, 0.88f);
        var edge = new Color(0.82f, 0.72f, 0.5f);
        for (int pass = 0; pass < 2; pass++)   // 먼저 한 겹 두껍게 윤곽색으로, 그 위에 본색
        {
            for (int k = 0; k < 80; k++)
            {
                float s = k / 79f;
                Vector2 c = WingArm(origin, s);
                float r = Mathf.Lerp(9f, 4.5f, s) + (pass == 0 ? 1.5f : 0f);
                PaintCapsule(pixels, w, h, c, c, r, r, pass == 0 ? edge : cream, pass == 0 ? edge : cream, false);
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels(pixels);
        tex.Apply();
        wingSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(origin.x / w, origin.y / h), 125f);
        return wingSprite;
    }

    // 윗선 : 어깨에서 바깥으로 나가며 위로 휘어 오르다 완만해지는 곡선 (s : 0 어깨 ~ 1 날개 끝)
    private static Vector2 WingArm(Vector2 origin, float s)
    {
        return origin + new Vector2(215f * s, 135f * (1f - (1f - s) * (1f - s)));
    }

    // 두 점 사이의 둥근 막대를 그림 (반지름은 from에서 to로 갈수록 변함, 색도 from → to로 번짐). 기존 픽셀 위에 덮어씀
    private static void PaintCapsule(Color[] pixels, int w, int h, Vector2 a, Vector2 b, float radiusA, float radiusB, Color colorA, Color colorB, bool outline)
    {
        float pad = Mathf.Max(radiusA, radiusB) + 2f;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - pad));
        int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + pad));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - pad));
        int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + pad));
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;

        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x, y);
                float t = lenSq > 0.0001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq) : 0f;
                float dist = Vector2.Distance(p, a + ab * t);
                float radius = Mathf.Lerp(radiusA, radiusB, t);
                if (dist > radius) continue;

                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                Color col = Color.Lerp(colorA, colorB, t * t);
                if (outline && radius - dist < 2.5f) col *= 0.86f;   // 가장자리 윤곽선

                Color old = pixels[y * w + x];
                float outA = alpha + old.a * (1f - alpha);
                Vector3 rgb = (new Vector3(col.r, col.g, col.b) * alpha + new Vector3(old.r, old.g, old.b) * old.a * (1f - alpha)) / Mathf.Max(0.0001f, outA);
                pixels[y * w + x] = new Color(rgb.x, rgb.y, rgb.z, outA);
            }
        }
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
