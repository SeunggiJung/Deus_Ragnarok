using UnityEngine;

// 피해량 표시. 맞은 캐릭터 옆에 숫자가 떠올랐다가 사라짐 (월드 공간 TextMesh, 좌우 반전의 영향을 받지 않도록 별도 오브젝트).
// BaseCharacter.TakeDamage에서 실제로 피해가 들어갔을 때만 호출되므로 무적으로 무시된 공격은 표시되지 않음
//   - 일반 공격 : 흰색 / 점프 공격, 3타 찌르기(크리티컬) : 노란색, 조금 더 크게 / 플레이어 피격 : 빨간색
public class DamagePopup : MonoBehaviour
{
    public static readonly Color NormalColor = Color.white;
    public static readonly Color CriticalColor = new Color(1f, 0.85f, 0.1f, 1f);
    public static readonly Color PlayerHitColor = new Color(1f, 0.25f, 0.25f, 1f);

    // 공격하는 쪽이 TakeDamage를 부르기 직전에 true로 지정 (IDamageable 인터페이스를 바꾸지 않기 위한 전달 방법)
    public static bool NextIsCritical { get; set; }

    private const float Lifetime = 0.9f;
    private const float RiseDistance = 0.9f;

    private static Font font;

    private TextMesh text;
    private TextMesh shadow;
    private Vector3 start;
    private float age;
    private Color color;
    private float baseScale;

    public static void Show(BaseCharacter target, float damage, bool critical)
    {
        bool isPlayer = target is PlayerController;
        Color color = isPlayer ? PlayerHitColor : critical ? CriticalColor : NormalColor;

        var go = new GameObject("DamagePopup");
        var popup = go.AddComponent<DamagePopup>();
        popup.Init(GetAnchor(target), Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(), color, critical || isPlayer);
    }

    // 캐릭터 몸통 위쪽 옆에 약간 흩어서 배치 (연속 타격 시 숫자가 겹치지 않도록)
    private static Vector3 GetAnchor(BaseCharacter target)
    {
        Collider2D col = null;
        foreach (Collider2D c in target.GetComponents<Collider2D>())
        {
            if (c.enabled && !c.isTrigger) { col = c; break; }
        }

        Vector3 pos = target.transform.position;
        if (col != null)
        {
            Bounds b = col.bounds;
            pos = new Vector3(b.center.x, b.center.y + Mathf.Min(b.extents.y, 1.6f) * 0.55f, 0f);
            pos.x += Random.Range(-0.15f, 0.15f) + Mathf.Min(b.extents.x, 1.2f) * 0.5f * (Random.value < 0.5f ? -1f : 1f);
        }
        pos.y += Random.Range(0f, 0.3f);
        return pos;
    }

    private void Init(Vector3 position, string value, Color textColor, bool large)
    {
        start = position;
        transform.position = position;
        color = textColor;
        baseScale = large ? 1.35f : 1f;

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        shadow = CreateText("Shadow", value, new Color(0f, 0f, 0f, 0.85f), 60, new Vector3(0.04f, -0.04f, 0f));
        text = CreateText("Text", value, textColor, 61, Vector3.zero);
        transform.localScale = Vector3.one * baseScale * 1.4f;
    }

    private TextMesh CreateText(string objectName, string value, Color textColor, int order, Vector3 offset)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;
        var t = go.AddComponent<TextMesh>();
        t.font = font;
        t.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        t.GetComponent<MeshRenderer>().sortingOrder = order;
        t.text = value;
        t.fontSize = 64;
        t.fontStyle = FontStyle.Bold;
        t.characterSize = 0.1f;
        t.anchor = TextAnchor.MiddleCenter;
        t.alignment = TextAlignment.Center;
        t.color = textColor;
        return t;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / Lifetime);

        // 빠르게 떠오르다 천천히 멈추고, 처음에 커졌다가 줄어드는 팝 효과, 후반 40%에 걸쳐 투명해짐
        float rise = 1f - (1f - t) * (1f - t);
        transform.position = start + Vector3.up * (rise * RiseDistance);
        transform.localScale = Vector3.one * baseScale * Mathf.Lerp(1.4f, 1f, Mathf.Clamp01(t * 6f));

        float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        text.color = new Color(color.r, color.g, color.b, alpha);
        shadow.color = new Color(0f, 0f, 0f, 0.85f * alpha);

        if (age >= Lifetime) Destroy(gameObject);
    }
}
