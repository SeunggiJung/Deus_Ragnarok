using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 화면을 검게 덮었다가 씬을 불러오고, 새 씬에서 다시 밝아지는 전환 연출.
// 씬이 바뀌어도 유지되도록 DontDestroyOnLoad, 전환이 끝나면 스스로 제거
public class ScreenFader : MonoBehaviour
{
    private Image overlay;

    public static void FadeAndLoad(string sceneName, float duration)
    {
        var go = new GameObject("ScreenFader");
        DontDestroyOnLoad(go);
        var fader = go.AddComponent<ScreenFader>();
        fader.Build();
        fader.StartCoroutine(fader.Run(sceneName, duration));
    }

    private void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 모든 UI 위

        var imageGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
        var rect = imageGo.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        overlay = imageGo.GetComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.raycastTarget = true; // 전환 중 UI 클릭 차단
    }

    private IEnumerator Run(string sceneName, float duration)
    {
        yield return Fade(0f, 1f, duration);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        if (load == null)
        {
            Debug.LogError($"씬 '{sceneName}'을(를) 불러올 수 없습니다. Build Settings에 추가돼 있는지 확인하세요.");
            yield return Fade(1f, 0f, duration);
            Destroy(gameObject);
            yield break;
        }
        while (!load.isDone) yield return null;

        yield return Fade(1f, 0f, duration);
        Destroy(gameObject);
    }

    // 슬로모션 등 timeScale의 영향을 받지 않도록 실제 시간 기준
    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        overlay.color = new Color(0f, 0f, 0f, to);
    }
}
