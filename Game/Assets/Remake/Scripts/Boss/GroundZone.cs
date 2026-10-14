using UnityEngine;

// 바닥에 표시되는 공격 장판 (보스 도약 내려찍기의 착지 지점).
// 추적 중에는 주황색으로 맥동하고, Lock 하면 붉게 빠르게 점멸하며 안쪽이 차오름 → 다 차는 순간 보스가 내려찍음
public class GroundZone : MonoBehaviour
{
    private const float Flatten = 0.3f;   // 바닥에 누운 타원 (폭 대비 높이)

    private SpriteRenderer outline, fill;
    private float radius;
    private bool locked;
    private float lockStart, lockDuration = 1f;
    private float fadeStart = -1f;
    private Color idleColor = new Color(1f, 0.6f, 0.1f);
    private Color lockedColor = new Color(1f, 0.15f, 0.1f);

    public float X => transform.position.x;
    public float Radius => radius;

    public static GroundZone Create(Vector2 center, float radius)
    {
        var go = new GameObject("GroundZone");
        go.transform.position = center;
        var zone = go.AddComponent<GroundZone>();
        zone.radius = radius;
        zone.outline = zone.NewRenderer("Outline", Fx.Ring(), 12);
        zone.fill = zone.NewRenderer("Fill", Fx.Circle(), 11);
        zone.Apply(0f);
        return zone;
    }

    private SpriteRenderer NewRenderer(string name, Sprite sprite, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return sr;
    }

    // 보스 테마색에 맞춰 장판 색 변경 (기본 : 주황 → 붉은색)
    public void SetColors(Color idle, Color locked)
    {
        idleColor = idle;
        lockedColor = locked;
    }

    public void SetX(float x) => transform.position = new Vector3(x, transform.position.y, transform.position.z);

    // 위치를 고정하고 안쪽을 duration 동안 채움
    public void Lock(float duration)
    {
        locked = true;
        lockStart = Time.time;
        lockDuration = Mathf.Max(0.05f, duration);
    }

    public void Dismiss()
    {
        if (fadeStart < 0f) fadeStart = Time.time;
    }

    private void Update()
    {
        float fade = 1f;
        if (fadeStart >= 0f)
        {
            fade = 1f - (Time.time - fadeStart) / 0.25f;
            if (fade <= 0f) { Destroy(gameObject); return; }
        }
        Apply(fade);
    }

    private void Apply(float fade)
    {
        float width = radius * 2f;
        outline.transform.localScale = new Vector3(width, width * Flatten, 1f);

        float progress = locked ? Mathf.Clamp01((Time.time - lockStart) / lockDuration) : 0f;
        float fillWidth = Mathf.Max(0.02f, width * progress);
        fill.transform.localScale = new Vector3(fillWidth, fillWidth * Flatten, 1f);

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (locked ? 26f : 6f));
        Color baseColor = locked ? lockedColor : idleColor;
        outline.color = new Color(baseColor.r, baseColor.g, baseColor.b, (0.65f + 0.35f * pulse) * fade);
        fill.color = new Color(baseColor.r, baseColor.g * 0.8f, baseColor.b, (locked ? 0.5f + 0.2f * pulse : 0.25f) * fade);
    }
}
