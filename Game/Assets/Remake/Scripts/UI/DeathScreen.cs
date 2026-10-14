using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 플레이어 사망 화면 (원작 "YOU DIED." + 재시도 / 홈 버튼 계승)
//   - 부활하기 : 부활 기회가 있을 때만 표시. 제한 시간(10초) 안에 누르면 제자리에서 부활 (연출은 ReviveEffect)
//   - 체크포인트에서 재시작 : 마지막 체크포인트 시점의 성장(레벨 / 스탯 / 무기 / 부활 기회)으로 되돌린 뒤 현재 씬을 다시 불러와
//     그 위치에서 시작 (체크포인트 이후에 얻은 것은 모두 사라지고, 몬스터 / 보스 / 퍼즐도 처음 상태로 돌아감)
//   - 홈으로 : 성장 / 체크포인트를 초기화하고 시작 화면으로
// PlayerHUD가 자동으로 붙여 주므로 모든 스테이지에서 동작
public class DeathScreen : MonoBehaviour
{
    private const string HomeScene = "start";
    private const float ShowDelay = 1.6f;    // 사망 모션을 보여 준 뒤 나타남
    private const float FadeTime = 0.8f;
    private const float ReviveTimeLimit = 10f;

    private PlayerController player;
    private CanvasGroup group;
    private bool built;
    private bool choosing;

    private RectTransform reviveButton;
    private Text reviveLabel;
    private Button reviveButtonComponent;
    private Text timerLabel;
    private RectTransform timerBar;
    private RectTransform restartButton;
    private RectTransform homeButton;
    private Coroutine timerRoutine;

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
        if (!player.IsDead) yield break;   // 그 사이 다른 방법으로 되살아난 경우

        if (!built) Build();
        EnsureEventSystem();

        bool canRevive = player.ReviveCount > 0;
        reviveButton.gameObject.SetActive(canRevive);
        timerLabel.gameObject.SetActive(canRevive);
        timerBar.parent.gameObject.SetActive(canRevive);

        // 부활 버튼이 있으면 아래로 한 칸씩 밀어서 배치
        float top = canRevive ? 70f : -40f;
        reviveButton.anchoredPosition = new Vector2(0f, 70f);
        restartButton.anchoredPosition = new Vector2(0f, top - (canRevive ? 130f : 0f));
        homeButton.anchoredPosition = new Vector2(0f, top - (canRevive ? 260f : 130f));

        group.gameObject.SetActive(true);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

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

        if (canRevive)
            timerRoutine = StartCoroutine(TimerRoutine());
    }

    // 부활 제한 시간 : 숫자 + 줄어드는 바. 시간이 끝나면 부활 버튼만 비활성화 (재시작 / 홈은 계속 선택 가능)
    private IEnumerator TimerRoutine()
    {
        reviveButtonComponent.interactable = true;
        reviveLabel.text = $"부활하기  (남은 기회 {player.ReviveCount})";

        float remaining = ReviveTimeLimit;
        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            timerLabel.text = $"{Mathf.Max(0f, remaining):F1}초";
            timerBar.anchorMax = new Vector2(Mathf.Clamp01(remaining / ReviveTimeLimit), 1f);
            yield return null;
        }

        reviveButtonComponent.interactable = false;
        reviveLabel.text = "부활 시간 종료";
        timerLabel.text = "";
        timerBar.anchorMax = new Vector2(0f, 1f);
        timerRoutine = null;
    }

    private void Revive()
    {
        if (choosing || !player.IsDead) return;
        if (!player.TryRevive()) return;

        if (timerRoutine != null) StopCoroutine(timerRoutine);
        timerRoutine = null;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(false);
    }

    private void RestartAtCheckpoint()
    {
        if (choosing) return;
        choosing = true;
        PlayerSession.PrepareRestart(SceneManager.GetActiveScene().name); // 체크포인트 시점의 성장으로 되돌림
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

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.SetParent(canvasGo.transform, false);
        Stretch(dimRect);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        Font font = LoadFont();

        var title = CreateLabel(dimRect, "Title", "YOU DIED.", font, 120, new Color(0.75f, 0.08f, 0.08f, 1f));
        title.rectTransform.anchoredPosition = new Vector2(0f, 270f);
        title.rectTransform.sizeDelta = new Vector2(1200f, 180f);
        title.fontStyle = FontStyle.Bold;

        // 부활 버튼 + 남은 시간(숫자, 바)
        reviveButton = CreateButton(dimRect, "부활하기", font, Vector2.zero, Revive, out reviveLabel, out reviveButtonComponent);
        reviveLabel.color = new Color(1f, 0.9f, 0.5f, 1f);
        reviveButton.GetComponent<Image>().color = new Color(0.25f, 0.18f, 0.05f, 0.95f);

        timerLabel = CreateLabel(dimRect, "Timer", "", font, 44, new Color(1f, 0.9f, 0.5f, 1f));
        timerLabel.rectTransform.anchoredPosition = new Vector2(0f, 175f);
        timerLabel.rectTransform.sizeDelta = new Vector2(640f, 60f);

        var barBack = new GameObject("Timer Bar", typeof(RectTransform), typeof(Image));
        var barRect = barBack.GetComponent<RectTransform>();
        barRect.SetParent(dimRect, false);
        barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.sizeDelta = new Vector2(640f, 10f);
        barRect.anchoredPosition = new Vector2(0f, 12f);
        barBack.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

        var barFill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        timerBar = barFill.GetComponent<RectTransform>();
        timerBar.SetParent(barRect, false);
        timerBar.anchorMin = Vector2.zero;
        timerBar.anchorMax = Vector2.one;
        timerBar.offsetMin = timerBar.offsetMax = Vector2.zero;
        barFill.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f, 1f);

        restartButton = CreateButton(dimRect, "체크포인트에서 재시작", font, Vector2.zero, RestartAtCheckpoint, out _, out _);
        homeButton = CreateButton(dimRect, "홈으로", font, Vector2.zero, GoHome, out _, out _);

        group.gameObject.SetActive(false);
    }

    // 기본 폰트에는 한글이 없어서 OS 폰트(맑은 고딕)를 사용
    public static Font LoadFont()
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

    private static RectTransform CreateButton(Transform parent, string label, Font font, Vector2 position,
        UnityEngine.Events.UnityAction onClick, out Text labelText, out Button button)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(640f, 100f);
        rect.anchoredPosition = position;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

        button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.75f, 0.6f, 1f);
        colors.pressedColor = new Color(0.7f, 0.4f, 0.3f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        labelText = CreateLabel(rect, "Label", label, font, 42, Color.white);
        Stretch(labelText.rectTransform);
        return rect;
    }
    #endregion
}
