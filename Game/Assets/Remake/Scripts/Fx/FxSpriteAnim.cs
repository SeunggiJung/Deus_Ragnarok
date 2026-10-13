using UnityEngine;

// 스프라이트 프레임 애니메이션 이펙트 (폭발, 불줄기 조각 등). 반복하지 않으면 마지막 프레임 후 제거
public class FxSpriteAnim : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite[] frames;
    private float fps, startTime, duration, fadeTime;
    private bool loop;

    // width : 첫 프레임 기준 월드 폭. duration : 반복일 때 유지 시간
    public static FxSpriteAnim Play(Vector3 position, Sprite[] frames, float fps, float width, bool loop,
                                    float duration, int sortingOrder, float fadeTime = 0.12f)
    {
        if (frames == null || frames.Length == 0 || frames[0] == null) return null;

        var go = new GameObject("FxSprite");
        go.transform.position = position;
        var fx = go.AddComponent<FxSpriteAnim>();
        fx.sr = go.AddComponent<SpriteRenderer>();
        fx.sr.sortingOrder = sortingOrder;
        fx.frames = frames;
        fx.fps = fps;
        fx.loop = loop;
        fx.duration = loop ? duration : frames.Length / fps;
        fx.fadeTime = fadeTime;
        fx.startTime = Time.time;

        float spriteWidth = frames[0].bounds.size.x;
        float scale = spriteWidth > 0f ? width / spriteWidth : 1f;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        fx.Apply();
        return fx;
    }

    private void Update()
    {
        float elapsed = Time.time - startTime;
        if (elapsed >= duration) { Destroy(gameObject); return; }
        Apply();
    }

    private void Apply()
    {
        float elapsed = Time.time - startTime;
        int index = Mathf.FloorToInt(elapsed * fps);
        index = loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1);
        sr.sprite = frames[index];

        float remaining = duration - elapsed;
        float alpha = fadeTime > 0f ? Mathf.Clamp01(remaining / fadeTime) : 1f;
        sr.color = new Color(1f, 1f, 1f, alpha);
    }
}
