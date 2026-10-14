using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이어 사망 화면 (원작 "YOU DIED." + 재시도 / 홈 버튼 계승)
//   - 체크포인트에서 재시작 : 지금까지의 성장(레벨 / 스탯 / 무기)과 체크포인트를 저장하고 현재 씬을 다시 불러와 그 위치에서 시작
//     (씬을 다시 불러오므로 몬스터 / 보스 / 퍼즐도 처음 상태로 돌아감)
//   - 홈으로 : 성장 / 체크포인트를 초기화하고 시작 화면으로
// PlayerHUD가 자동으로 붙여 주므로 모든 스테이지에서 동작
public class DeathScreen : MonoBehaviour
{
    private const string HomeScene = "start";
    private const float ShowDelay = 1.6f;   // 사망 모션을 보여 준 뒤 나타남
    private const float FadeTime = 0.8f;

    private PlayerController player;
    private CanvasGroup group;
    private bool built;
    private bool choosing;

    public void Init(PlayerController owner)
    {
        player = owner;
        player.OnDied += HandlePlayerDied;
    }

    private void OnDestroy()
    {
        if (player != null) player.OnDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied()
    {
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return new WaitForSecondsRealtime(ShowDelay);
        if (!built) Build();
        EnsureEventSystem();

        group.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < FadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / FadeTime);
            yield return null;
        }
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void RestartAtCheckpoint()
    {
        if (choosing) return;
        choosing = true;
        PlayerSession.Save(player); // 성장 유지, 체크포인트는 PlayerRespawn.SetCheckpoint 때 이미 저장됨
        ScreenFader.FadeAndLoad(SceneManager.GetActiveScene().name, 0.5f);
    }

    private void GoHome()
    {
        if (choosing) return;
        choosing = true;
        PlayerSession.Reset();
        ScreenFader.FadeAndLoad(HomeScene, 0.5f);
    }

    #region UI
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void Build()
    {
        built = true;

        var canvasGo = new GameObject("Death Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200; // HUD(100)와 화면 전환(1000) 사이
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        group = canvasGo.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.SetParent(canvasGo.transform, false);
        Stretch(dimRect);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        Font font = LoadFont();

        var title = CreateLabel(dimRect, "Title", "YOU DIED.", font, 120, new Color(0.75f, 0.08f, 0.08f, 1f));
        title.rectTransform.anchoredPosition = new Vector2(0f, 150f);
        title.rectTransform.sizeDelta = new Vector2(1200f, 180f);
        title.fontStyle = FontStyle.Bold;

        CreateButton(dimRect, "체크포인트에서 재시작", font, new Vector2(0f, -40f), RestartAtCheckpoint);
        CreateButton(dimRect, "홈으로", font, new Vector2(0f, -170f), GoHome);

        canvasGo.SetActive(false);
        group.gameObject.SetActive(false);
    }

    // 기본 폰트에는 한글이 없어서 OS 폰트(맑은 고딕)를 사용
    private static Font LoadFont()
    {
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "NanumGothic", "Arial" }, 40);
        return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Text CreateLabel(Transform parent, string objectName, string value, Font font, int size, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static void CreateButton(Transform parent, string label, Font font, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(640f, 100f);
        rect.anchoredPosition = position;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.55f, 0.55f, 1f);
        colors.pressedColor = new Color(0.7f, 0.3f, 0.3f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var text = CreateLabel(rect, "Label", label, font, 42, Color.white);
        Stretch(text.rectTransform);
    }
    #endregion
}
