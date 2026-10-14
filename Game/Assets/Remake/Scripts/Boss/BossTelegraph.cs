using UnityEngine;

public enum TelegraphKind { Pound, Grab, Leap, Charge, Guard, Melee, Spikes, Bolt, Heavy }

// 보스 패턴 예고 연출. 몸 색 변화는 보스가 붉은 계열이면 구분이 안 되므로,
// 몸 색과 무관하게 눈에 띄는 별도 신호를 몸 주변에 띄운다.
//  1) 몸을 감싸며 줄어드는 링 : 링이 몸에 닿는 순간이 공격 타이밍
//  2) 머리 위 아이콘 : 패턴 종류 (느낌표 = 잡기 / ▼ = 바닥 찍기 / ▲ = 도약 / >> = 돌진 / 방패 = 방어)
//  3) 패턴 고유 색 : 주황 = 바닥 찍기, 노랑 = 잡기(구르기!), 보라 = 도약, 청록 = 돌진, 하늘 = 방어,
//     흰색 X = 근접 연타, 연두 ▼ = 지면 가시, 분홍 조준점 = 에너지탄, 파랑 별 = 기 모아 강타(점프 공격 카운터)
//  4) 일부 패턴은 기를 모으는 파티클
// 사용 : boss.Telegraph.Show(kind, 공격까지 남은 시간) → 공격이 시작되면 Hide()
public class BossTelegraph : MonoBehaviour
{
    private BossController boss;
    private SpriteRenderer ring, icon;
    private ParticleSystem gather;
    private TelegraphKind kind;
    private Color color;
    private float startTime, duration;
    private bool active;

    private void Awake() => boss = GetComponent<BossController>();

    public static Color ColorOf(TelegraphKind kind)
    {
        switch (kind)
        {
            case TelegraphKind.Pound: return new Color(1f, 0.6f, 0.1f);
            case TelegraphKind.Grab: return new Color(1f, 0.95f, 0.2f);
            case TelegraphKind.Leap: return new Color(0.75f, 0.45f, 1f);
            case TelegraphKind.Charge: return new Color(0.3f, 0.9f, 1f);
            case TelegraphKind.Melee: return new Color(1f, 1f, 1f);
            case TelegraphKind.Spikes: return new Color(0.5f, 1f, 0.4f);
            case TelegraphKind.Bolt: return new Color(1f, 0.45f, 0.9f);
            case TelegraphKind.Heavy: return new Color(0.45f, 0.55f, 1f);
            default: return new Color(0.6f, 0.85f, 1f);
        }
    }

    private static Fx.Icon IconOf(TelegraphKind kind)
    {
        switch (kind)
        {
            case TelegraphKind.Pound: return Fx.Icon.DownArrow;
            case TelegraphKind.Grab: return Fx.Icon.Exclamation;
            case TelegraphKind.Leap: return Fx.Icon.UpArrow;
            case TelegraphKind.Charge: return Fx.Icon.Chevron;
            case TelegraphKind.Melee: return Fx.Icon.Cross;
            case TelegraphKind.Spikes: return Fx.Icon.DownArrow;
            case TelegraphKind.Bolt: return Fx.Icon.Target;
            case TelegraphKind.Heavy: return Fx.Icon.Star;
            default: return Fx.Icon.Shield;
        }
    }

    public void Show(TelegraphKind kind, float duration)
    {
        Hide();
        GameAudio.PlayTelegraph(kind);   // 패턴별 예고음
        this.kind = kind;
        this.duration = Mathf.Max(0.05f, duration);
        color = ColorOf(kind);
        startTime = Time.time;

        ring = NewRenderer("TelegraphRing", Fx.Ring(), 40);
        icon = NewRenderer("TelegraphIcon", Fx.IconSprite(IconOf(kind)), 41);

        if (kind == TelegraphKind.Leap || kind == TelegraphKind.Pound || kind == TelegraphKind.Heavy)
        {
            float radius = BodySize() * 0.9f;
            gather = Fx.Gather(new Color(color.r, color.g, color.b, 0.9f), radius, duration * 0.6f, kind == TelegraphKind.Leap ? 60f : 35f, 0.35f);
        }

        active = true;
        LateUpdate();
    }

    public void Hide()
    {
        active = false;
        if (ring != null) Destroy(ring.gameObject);
        if (icon != null) Destroy(icon.gameObject);
        if (gather != null)
        {
            gather.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gather.gameObject, 1f);
        }
        ring = icon = null;
        gather = null;
    }

    private void OnDestroy() => Hide();

    private SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    private float BodySize()
    {
        Bounds b = boss.Body.bounds;
        return Mathf.Max(b.size.x, b.size.y);
    }

    private void LateUpdate()
    {
        if (!active) return;
        if (boss == null || boss.IsDead) { Hide(); return; }

        Bounds b = boss.Body.bounds;
        float elapsed = Time.time - startTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // 링 : 큼 → 몸 크기로 수축 (끝에 가까울수록 빠르게). 수축이 끝난 뒤에는 번쩍임
        float size = BodySize();
        float eased = t * t;
        float scale = Mathf.Lerp(size * 2.6f, size * 1.05f, eased);
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * (8f + 14f * t));
        ring.transform.position = b.center;
        ring.transform.localScale = new Vector3(scale, scale, 1f);
        ring.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 0.9f, t) * (0.7f + 0.3f * pulse));

        // 아이콘 : 머리 위, 처음에 크게 튀어나왔다 자리잡고 위아래로 까딱임. 임박하면 하얗게 점멸
        float punch = Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(elapsed / 0.2f));
        float iconScale = 2.4f * punch;
        icon.transform.position = new Vector3(b.center.x, b.max.y + 1.6f + 0.12f * Mathf.Sin(elapsed * 12f), 0f);
        icon.transform.localScale = new Vector3(iconScale, iconScale, 1f);
        icon.flipX = kind == TelegraphKind.Charge && boss.FacingDir < 0f;
        bool flash = t > (kind == TelegraphKind.Heavy ? 0.6f : 0.7f) && Mathf.FloorToInt(elapsed * 14f) % 2 == 0;
        icon.color = flash ? Color.white : color;

        if (gather != null)
            gather.transform.position = b.center;
    }
}
