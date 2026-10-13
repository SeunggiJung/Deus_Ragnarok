using System.Collections;
using UnityEngine;

// LV+1 아이템. 상자에서 튀어나온 뒤 공중에 떠 있고, 플레이어가 닿으면 남은 경험치를 채워 즉시 레벨업
public class LevelUpPickup : MonoBehaviour
{
    private const float PopHeight = 1.2f;   // 튀어나오는 높이
    private const float PopTime = 0.35f;
    private const float BobHeight = 0.12f;
    private const float Size = 0.7f;        // 월드 크기 (지름)

    private static Sprite sprite;

    private Vector3 restPosition;
    private bool ready;
    private bool collected;

    public static LevelUpPickup Spawn(Vector2 position)
    {
        var go = new GameObject("LevelUpPickup");
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.sortingOrder = 45;
        go.transform.localScale = Vector3.one * Size;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        // 트리거 감지를 위해 Kinematic Rigidbody (중력 없음)
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 위에 "LV+1" 글자
        var label = new GameObject("Label").AddComponent<TextMesh>();
        label.transform.SetParent(go.transform, false);
        label.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        label.text = "LV+1";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        label.GetComponent<MeshRenderer>().sortingOrder = 46;
        label.fontSize = 48;
        label.characterSize = 0.1f;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.9f, 0.3f);

        var pickup = go.AddComponent<LevelUpPickup>();
        pickup.StartCoroutine(pickup.PopRoutine());
        return pickup;
    }

    private IEnumerator PopRoutine()
    {
        Vector3 start = transform.position;
        restPosition = start + Vector3.up * PopHeight;
        float elapsed = 0f;
        while (elapsed < PopTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / PopTime;
            transform.position = Vector3.Lerp(start, restPosition, 1f - (1f - t) * (1f - t)); // 감속하며 위로
            yield return null;
        }
        ready = true; // 튀어오르는 도중에 바로 먹히지 않도록
    }

    private void Update()
    {
        if (!ready || collected) return;
        transform.position = restPosition + Vector3.up * Mathf.Sin(Time.time * 3f) * BobHeight;
        transform.localScale = Vector3.one * Size * (1f + 0.08f * Mathf.Sin(Time.time * 6f)); // 반짝임
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!ready || collected) return;

        PlayerProgression progression = other.GetComponentInParent<PlayerProgression>();
        if (progression == null) return;

        collected = true;
        progression.LevelUpInstantly();
        StartCoroutine(CollectRoutine());
    }

    // 먹었을 때 커지면서 사라짐
    private IEnumerator CollectRoutine()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        Vector3 baseScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.25f;
            transform.localScale = baseScale * (1f + t);
            foreach (var r in renderers)
            {
                if (r is SpriteRenderer sr) sr.color = new Color(1f, 1f, 1f, 1f - t);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    // 금색 원 안에 흰색 위쪽 화살표 (별도 이미지 에셋 없이 코드로 생성)
    private static Sprite GetSprite()
    {
        if (sprite != null) return sprite;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        Vector2 c = new Vector2(15.5f, 15.5f);
        var gold = new Color(1f, 0.78f, 0.15f, 1f);
        var goldDark = new Color(0.8f, 0.5f, 0.05f, 1f);
        var white = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                Color col = new Color(0f, 0f, 0f, 0f);
                if (d <= 15f) col = d >= 13.5f ? goldDark : gold;

                // 화살표 : 머리(삼각형) + 몸통(사각형)
                bool head = y >= 16 && y <= 25 && Mathf.Abs(x - 15.5f) <= (25 - y) * 0.9f;
                bool body = y >= 7 && y < 16 && x >= 13 && x <= 18;
                if (d <= 13f && (head || body)) col = white;

                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return sprite;
    }
}
