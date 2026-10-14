using UnityEngine;

// 전투 이펙트 공용 도구. 이미지 에셋 없이 코드로 스프라이트 / 파티클을 만들어 쓴다.
// - 스프라이트 : 부드러운 원, 링, 사각형, 가시(삼각형), 패턴 예고 아이콘
// - 파티클 : Burst(한 번 터짐), Gather(안쪽으로 모임), Trail(따라다니며 계속 방출)
// - Ring : 바닥에 퍼지는 충격 링
public static class Fx
{
    public enum Icon { Exclamation, DownArrow, UpArrow, Chevron, Shield, Cross, Target, Star }

    private static Sprite circle, ring, square, spike;
    private static readonly Sprite[] icons = new Sprite[8];
    private static Material softMaterial, hardMaterial;

    #region Sprites
    // 정적 캐시는 씬 전환 / 에셋 정리로 네이티브 객체만 사라질 수 있어 매번 Unity null 검사를 한다
    private static Sprite MakeSprite(Texture2D tex, float pixelsPerUnit, Vector2 pivot, FilterMode filter)
    {
        tex.filterMode = filter;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    // 지름 1 유닛의 부드러운 원 (가운데 진하고 가장자리로 투명)
    public static Sprite Circle()
    {
        if (circle != null) return circle;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(1f, 0f, d)));
            }
        return circle = MakeSprite(tex, n, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
    }

    // 지름 1 유닛의 링 (안쪽은 옅게 채움)
    public static Sprite Ring()
    {
        if (ring != null) return ring;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                float edge = Mathf.Clamp01(1f - Mathf.Abs(d - 0.9f) / 0.09f);   // 두께 있는 선
                float inner = d < 0.9f ? 0.12f : 0f;                              // 안쪽 옅은 채움
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Max(edge, inner)));
            }
        return ring = MakeSprite(tex, n, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
    }

    public static Sprite Square()
    {
        if (square != null) return square;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
        return square = MakeSprite(tex, 4, new Vector2(0.5f, 0.5f), FilterMode.Point);
    }

    // 폭 0.5 x 높이 1 유닛의 가시 (바닥 가운데가 피벗). 위로 갈수록 뾰족하고 옅어짐
    public static Sprite Spike()
    {
        if (spike != null) return spike;
        const int w = 32, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float half = (1f - y / (float)h) * (w / 2f);
                bool inside = Mathf.Abs(x + 0.5f - w / 2f) <= half;
                tex.SetPixel(x, y, inside ? new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0.75f, y / (float)h)) : Color.clear);
            }
        return spike = MakeSprite(tex, h, new Vector2(0.5f, 0f), FilterMode.Bilinear);
    }

    // 16x16 픽셀 아이콘 : 흰색 면 + 검은 외곽선 (SpriteRenderer.color로 색을 입힘). 높이 1 유닛
    public static Sprite IconSprite(Icon icon)
    {
        int index = (int)icon;
        if (icons[index] != null) return icons[index];

        const int n = 16;
        var mask = new bool[n, n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
                mask[x, y] = IconMask(icon, x, y);

        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                Color c = Color.clear;
                if (mask[x, y]) c = Color.white;
                else if (HasNeighbor(mask, x, y, n)) c = Color.black;
                tex.SetPixel(x, y, c);
            }
        return icons[index] = MakeSprite(tex, n, new Vector2(0.5f, 0.5f), FilterMode.Point);
    }

    private static bool HasNeighbor(bool[,] mask, int x, int y, int n)
    {
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx >= 0 && ny >= 0 && nx < n && ny < n && mask[nx, ny]) return true;
            }
        return false;
    }

    private static bool IconMask(Icon icon, int x, int y)
    {
        float cx = Mathf.Abs(x - 7.5f);
        switch (icon)
        {
            case Icon.Exclamation: // 느낌표 : 막대 + 점
                return (cx <= 1.6f && y >= 5 && y <= 13) || (cx <= 1.6f && y >= 1 && y <= 3);
            case Icon.DownArrow:   // ▼ : 아래로 내려찍기
                return (cx <= 2f && y >= 8 && y <= 14) || (y >= 1 && y <= 8 && cx <= (y - 0.5f) * 0.95f);
            case Icon.UpArrow:     // ▲ : 높이 도약
                return (cx <= 2f && y >= 1 && y <= 7) || (y >= 7 && y <= 14 && cx <= (14.5f - y) * 0.95f);
            case Icon.Chevron:     // >> : 돌진 (오른쪽 기준, 왼쪽을 볼 때는 flipX)
            {
                float d = Mathf.Abs(y - 7.5f);
                foreach (int off in new[] { 1, 8 })
                    if (x >= off && x <= off + 6 && Mathf.Abs((x - off) - (6f - d)) <= 1.1f && d <= 6f) return true;
                return false;
            }
            case Icon.Cross:       // X : 근접 연타
            {
                if (x < 2 || x > 13 || y < 2 || y > 13) return false;
                return Mathf.Abs(x - y) <= 1.2f || Mathf.Abs(x + y - 15f) <= 1.2f;
            }
            case Icon.Star:        // 별 : 기 모아 강타
            {
                float dx = Mathf.Abs(x - 7.5f), dy = Mathf.Abs(y - 7.5f);
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                return d <= 7f && (dx <= 1.2f || dy <= 1.2f || (Mathf.Abs(dx - dy) <= 1f && d <= 5f));
            }
            case Icon.Target:      // 조준점 : 에너지탄
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                return (d >= 4.3f && d <= 6.3f) || d <= 1.6f;
            }
            default:               // 방패
            {
                if (y < 1 || y > 14) return false;
                float half = y >= 9 ? 6f : Mathf.Max(0f, (y - 0.5f) * 0.7f);
                return cx <= half;
            }
        }
    }
    #endregion

    #region Particles
    private static Material ParticleMaterial(bool hard)
    {
        if (hard)
        {
            if (hardMaterial == null)
            {
                hardMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
                hardMaterial.mainTexture = Square().texture;
            }
            return hardMaterial;
        }
        if (softMaterial == null)
        {
            softMaterial = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
            softMaterial.mainTexture = Circle().texture;
        }
        return softMaterial;
    }

    private static ParticleSystem NewSystem(string name, bool hard, int sortingOrder)
    {
        var go = new GameObject(name);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = ParticleMaterial(hard);
        renderer.sortingOrder = sortingOrder;

        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // 시간에 따라 작아지며 사라짐
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;
        return ps;
    }

    // 한 번 터지는 파티클. spread 180 이상이면 전방위, 그보다 작으면 dir 방향 부채꼴 (각도)
    public static void Burst(Vector2 position, Vector2 dir, float spread, Color color, int count,
                             float speedMin, float speedMax, float sizeMin, float sizeMax,
                             float life, float gravity = 1f, bool hard = true)
    {
        ParticleSystem ps = NewSystem("FxBurst", hard, 30);
        ps.transform.position = position;
        if (dir.sqrMagnitude > 0f)
            ps.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, dir.y, 0f), Vector3.forward);

        var main = ps.main;
        main.loop = false;
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = spread >= 180f ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Cone;
        shape.radius = 0.1f;
        if (spread < 180f) shape.angle = spread * 0.5f;

        ps.Play();
        Object.Destroy(ps.gameObject, life + 0.5f);
    }

    // 가장자리에서 중심으로 모여드는 파티클 (기 모으기). 반환한 시스템은 호출한 쪽에서 위치를 갱신하고 Stop / Destroy
    public static ParticleSystem Gather(Color color, float radius, float life, float rate, float size)
    {
        ParticleSystem ps = NewSystem("FxGather", false, 31);
        var main = ps.main;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = -radius / life;   // 음수 속도 : 중심을 향해 이동
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startColor = color;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;

        ps.Play();
        return ps;
    }

    // 붙어 다니며 계속 내뿜는 파티클 (먼지 꼬리 등)
    public static ParticleSystem Trail(Transform parent, Color color, float rate, float size, float life, Vector2 velocity, bool hard = false)
    {
        ParticleSystem ps = NewSystem("FxTrail", hard, 19);
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = Vector3.zero;

        var main = ps.main;
        main.loop = true;
        main.startLifetime = life;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startColor = color;
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.3f, 0.5f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(velocity.x * 0.5f, velocity.x);
        vel.y = new ParticleSystem.MinMaxCurve(velocity.y * 0.5f, velocity.y);
        vel.z = 0f;

        ps.Play();
        return ps;
    }
    #endregion

    // 바닥에 납작하게 퍼지는 충격 링 (폭이 startWidth → endWidth로 커지며 투명해짐)
    public static void Ring(Vector2 position, Color color, float startWidth, float endWidth, float duration, float squash = 0.35f)
    {
        GameAudio.PlayShockwave(endWidth);   // 충격파 / 폭발 / 착지 소리 (퍼지는 폭이 클수록 크고 낮음)
        var go = new GameObject("FxRing");
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Ring();
        sr.color = color;
        sr.sortingOrder = 18;
        go.AddComponent<FxRingPulse>().Init(startWidth, endWidth, duration, squash, color);
    }
}

// Fx.Ring가 쓰는 확산 / 페이드 애니메이션
public class FxRingPulse : MonoBehaviour
{
    private float startWidth, endWidth, duration, squash, elapsed;
    private Color color;
    private SpriteRenderer sr;

    public void Init(float startWidth, float endWidth, float duration, float squash, Color color)
    {
        this.startWidth = startWidth; this.endWidth = endWidth; this.duration = duration;
        this.squash = squash; this.color = color;
        sr = GetComponent<SpriteRenderer>();
        Apply(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        Apply(t);
        if (t >= 1f) Destroy(gameObject);
    }

    private void Apply(float t)
    {
        float eased = 1f - (1f - t) * (1f - t);   // 빠르게 퍼지다 느려짐
        float width = Mathf.Lerp(startWidth, endWidth, eased);
        transform.localScale = new Vector3(width, width * squash, 1f);
        sr.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
    }
}

// 대상을 따라다니다가 duration 뒤에 사라지는 오브젝트 (그로기 별빛 등)
public class FxFollow : MonoBehaviour
{
    private Transform target;
    private Vector2 offset;
    private float endTime;

    public void Init(Transform target, Vector2 offset, float duration)
    {
        this.target = target;
        this.offset = offset;
        endTime = Time.time + duration;
        Follow();
    }

    private void LateUpdate()
    {
        if (target == null || Time.time >= endTime)
        {
            // 입자가 자연스럽게 사라지도록 방출만 멈추고 잠시 뒤 제거
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, 1f);
            enabled = false;
            return;
        }
        Follow();
    }

    private void Follow() => transform.position = (Vector2)target.position + offset;
}
