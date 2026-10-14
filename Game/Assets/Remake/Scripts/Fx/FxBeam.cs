using UnityEngine;

// 잠깐 나타났다 옅어지며 사라지는 사각형 이펙트 (낙뢰 기둥, 광선 등). 이미지 에셋 없이 코드로 생성
public class FxBeam : MonoBehaviour
{
    private SpriteRenderer sr;
    private Color color;
    private float startTime, life, startAlpha;

    public static FxBeam Spawn(Vector2 center, Vector2 size, Color color, float life, int sortingOrder = 34)
    {
        GameAudio.Play(Sfx.Beam, 0.8f);
        var go = new GameObject("FxBeam");
        go.transform.position = center;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var beam = go.AddComponent<FxBeam>();
        beam.sr = go.AddComponent<SpriteRenderer>();
        beam.sr.sprite = Fx.Square();
        beam.sr.sortingOrder = sortingOrder;
        beam.color = color;
        beam.startAlpha = color.a;
        beam.life = Mathf.Max(0.02f, life);
        beam.startTime = Time.time;
        beam.sr.color = color;
        return beam;
    }

    // 지속 시간을 바꾸지 않고 색 / 크기를 갱신 (광선이 경고 → 발사로 바뀔 때)
    public void Set(Color newColor, Vector2 size)
    {
        color = newColor;
        startAlpha = newColor.a;
        startTime = Time.time;
        transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    private void Update()
    {
        float t = (Time.time - startTime) / life;
        if (t >= 1f) { Destroy(gameObject); return; }
        sr.color = new Color(color.r, color.g, color.b, startAlpha * (1f - t));
    }
}
