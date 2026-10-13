using UnityEngine;

// 스프라이트를 격자로 잘라 조각(파편)으로 흩뿌리는 연출. 보스 사망 등에 사용.
// 텍스처 픽셀을 읽지 않고 원본 텍스처의 영역만 잘라 쓰므로 Read/Write 설정이 필요 없음
public static class SpriteShatter
{
    public static void Shatter(SpriteRenderer source, int columns, int rows, float force, float lifetime)
    {
        Sprite sprite = source.sprite;
        if (sprite == null) return;

        Rect rect = sprite.rect;
        float ppu = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;            // rect 기준 픽셀 좌표
        float cellW = rect.width / columns;
        float cellH = rect.height / rows;

        Transform t = source.transform;
        Vector3 scale = t.lossyScale;
        float flip = source.flipX ? -1f : 1f;
        Vector3 center = source.bounds.center;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var pieceRect = new Rect(rect.x + x * cellW, rect.y + y * cellH, cellW, cellH);
                // FullRect : 픽셀을 읽어 외곽선을 만드는 Tight 메시 대신 사각형 메시 사용 (텍스처 읽기 불필요)
                Sprite piece = Sprite.Create(sprite.texture, pieceRect, new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);

                // 원본에서 이 조각이 있던 자리 (스프라이트 피벗 기준 → 월드)
                Vector2 localPx = new Vector2(x * cellW + cellW * 0.5f - pivot.x, y * cellH + cellH * 0.5f - pivot.y);
                Vector3 world = t.TransformPoint(new Vector3(localPx.x * flip / ppu, localPx.y / ppu, 0f));

                var go = new GameObject("Shard");
                go.transform.SetPositionAndRotation(world, t.rotation);
                go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = piece;
                sr.flipX = source.flipX;
                sr.color = source.color;
                sr.sortingLayerID = source.sortingLayerID;
                sr.sortingOrder = source.sortingOrder + 1;

                // 중심에서 바깥쪽 + 약간 위로 튀어나가며 회전
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 2f;
                Vector2 dir = ((Vector2)(world - center)).normalized + Vector2.up * 0.6f;
                rb.velocity = dir.normalized * force * Random.Range(0.5f, 1.2f);
                rb.angularVelocity = Random.Range(-540f, 540f);

                go.AddComponent<ShardFade>().Init(lifetime * Random.Range(0.8f, 1.2f), piece);
            }
        }
    }
}

public static class SpriteDebris
{
    // 미리 준비된 조각 스프라이트(예 : 상자 Break 시트)를 중심에서 사방으로 흩뿌림
    public static void Spawn(Sprite[] pieces, Vector2 center, Vector3 scale, int sortingOrder, float force, float lifetime)
    {
        if (pieces == null) return;

        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i] == null) continue;

            var go = new GameObject("Debris");
            go.transform.position = center;
            go.transform.localScale = scale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pieces[i];
            sr.sortingOrder = sortingOrder + 1;

            // 조각마다 다른 방향으로 (위쪽 반원에 고르게 분산)
            float angle = Mathf.Lerp(30f, 150f, pieces.Length > 1 ? i / (float)(pieces.Length - 1) : 0.5f) + Random.Range(-15f, 15f);
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2f;
            rb.velocity = dir * force * Random.Range(0.8f, 1.2f);
            rb.angularVelocity = Random.Range(-360f, 360f);

            go.AddComponent<ShardFade>().Init(lifetime, null);
        }
    }
}

// 파편이 서서히 투명해지다가 사라짐
public class ShardFade : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite generatedSprite;
    private float lifetime;
    private float elapsed;
    private float startAlpha;

    public void Init(float lifetime, Sprite generated)
    {
        this.lifetime = lifetime;
        generatedSprite = generated;
        sr = GetComponent<SpriteRenderer>();
        startAlpha = sr.color.a;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / lifetime;

        Color c = sr.color;
        c.a = startAlpha * (1f - t * t); // 처음엔 천천히, 끝으로 갈수록 빠르게 사라짐
        sr.color = c;

        if (t >= 1f) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (generatedSprite != null) Destroy(generatedSprite); // 런타임에 만든 스프라이트 정리
    }
}
