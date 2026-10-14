using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ESC 일시정지 메뉴 : 계속하기 / 다시시작 / 메인으로 나가기 + 오른쪽 아래 볼륨 슬라이더.
//   - 열려 있는 동안 게임이 멈춤(Time.timeScale = 0)이고 플레이어 입력도 잠김. ESC를 다시 누르거나 계속하기로 닫음
//   - 다시시작 / 메인으로 : 사망 화면(DeathScreen)과 같은 동작 (체크포인트 시점으로 되돌려 재시작 / 성장 초기화 후 시작 화면)
//   - 볼륨은 GameAudio.MasterVolume (모든 소리에 적용, 다음 실행에도 유지)
// PlayerHUD가 자동으로 붙여 주므로 모든 스테이지에서 동작. 사망 중이거나 컷신(입력 잠금) 중에는 열리지 않음
public class PauseMenu : MonoBehaviour
{
    private const string HomeScene = "start";

    private PlayerController player;
    private CanvasGroup group;
    private Text volumeLabel;
    private Slider slider;
    private bool built;
    private bool open;
    private bool leaving;

    private float previousTimeScale = 1f;
    private bool wasInputLocked;
    private float nextVolumeClick;

    public void Init(PlayerController owner)
    {
        player = owner;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || leaving) return;

        if (open) Close();
        else if (player != null && !player.IsDead && !player.InputLocked) Open();
    }

    private void OnDestroy()
    {
        // 열린 채로 씬이 바뀌어도 멈춘 상태가 남지 않도록
        if (open) Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    }

    private void Open()
    {
        if (!built) Build();
        EnsureEventSystem();

        open = true;
        previousTimeScale = Time.timeScale;
        wasInputLocked = player.InputLocked;
        Time.timeScale = 0f;
        player.InputLocked = true;

        slider.SetValueWithoutNotify(GameAudio.MasterVolume);
        UpdateVolumeLabel(GameAudio.MasterVolume);
        group.gameObject.SetActive(true);
        GameAudio.Play(Sfx.Click);
    }

    private void Close()
    {
        open = false;
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        player.InputLocked = wasInputLocked;
        group.gameObject.SetActive(false);
        GameAudio.SaveSettings();
        GameAudio.Play(Sfx.Click);
    }

    private void Restart()
    {
        if (leaving) return;
        leaving = true;
        ResumeTime();
        PlayerSession.PrepareRestart(SceneManager.GetActiveScene().name);
        ScreenFader.FadeAndLoad(SceneManager.GetActiveScene().name, 0.5f);
    }

    private void GoMain()
    {
        if (leaving) return;
        leaving = true;
        ResumeTime();
        PlayerSession.Reset();
        ScreenFader.FadeAndLoad(HomeScene, 0.5f);
    }

    // 장면 전환 연출(ScreenFader)은 실제 시간 기준이지만, 새 씬이 멈춘 채 시작하지 않도록 시간을 되돌림
    private void ResumeTime()
    {
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
        GameAudio.SaveSettings();
        GameAudio.Play(Sfx.Click);
    }

    private void OnVolumeChanged(float value)
    {
        GameAudio.SetMasterVolume(value);
        UpdateVolumeLabel(value);

        // 소리 크기를 가늠할 수 있게 조절 중에 짧은 소리를 들려줌
        if (Time.unscaledTime >= nextVolumeClick)
        {
            nextVolumeClick = Time.unscaledTime + 0.12f;
            GameAudio.Play(Sfx.Hit, 0.7f, 0f);
        }
    }

    private void UpdateVolumeLabel(float value)
    {
        volumeLabel.text = $"볼륨  {Mathf.RoundToInt(value * 100f)}%";
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

        var canvasGo = new GameObject("Pause Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210; // 사망 화면(200)보다 위
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        group = canvasGo.GetComponent<CanvasGroup>();

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.SetParent(canvasGo.transform, false);
        Stretch(dimRect);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

        Font font = DeathScreen.LoadFont();

        var title = CreateLabel(dimRect, "Title", "일시정지", font, 90, Color.white);
        title.rectTransform.anchoredPosition = new Vector2(0f, 250f);
        title.rectTransform.sizeDelta = new Vector2(1000f, 140f);
        title.fontStyle = FontStyle.Bold;

        CreateButton(dimRect, "계속하기", font, new Vector2(0f, 80f), Close);
        CreateButton(dimRect, "다시시작", font, new Vector2(0f, -50f), Restart);
        CreateButton(dimRect, "메인으로 나가기", font, new Vector2(0f, -180f), GoMain);

        BuildVolumePanel(dimRect, font);
        group.gameObject.SetActive(false);
    }

    // 오른쪽 아래 볼륨 조절
    private void BuildVolumePanel(Transform parent, Font font)
    {
        var panel = new GameObject("Volume Panel", typeof(RectTransform), typeof(Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-50f, 50f);
        rect.sizeDelta = new Vector2(520f, 130f);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

        volumeLabel = CreateLabel(rect, "Label", "볼륨", font, 34, Color.white);
        volumeLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
        volumeLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        volumeLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
        volumeLabel.rectTransform.anchoredPosition = new Vector2(0f, -12f);
        volumeLabel.rectTransform.sizeDelta = new Vector2(0f, 50f);

        slider = CreateSlider(rect);
        slider.value = GameAudio.MasterVolume;
        slider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private static Slider CreateSlider(Transform parent)
    {
        var root = new GameObject("Volume Slider", typeof(RectTransform), typeof(Slider));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 0f);
        rootRect.pivot = new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = new Vector2(0f, 22f);
        rootRect.sizeDelta = new Vector2(-60f, 40f);

        var background = NewImage("Background", rootRect, new Color(1f, 1f, 1f, 0.2f));
        background.anchorMin = new Vector2(0f, 0.35f);
        background.anchorMax = new Vector2(1f, 0.65f);
        background.offsetMin = background.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(rootRect, false);
        fillArea.anchorMin = new Vector2(0f, 0.35f);
        fillArea.anchorMax = new Vector2(1f, 0.65f);
        fillArea.offsetMin = new Vector2(10f, 0f);
        fillArea.offsetMax = new Vector2(-10f, 0f);
        var fill = NewImage("Fill", fillArea, new Color(1f, 0.82f, 0.3f, 1f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(rootRect, false);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(10f, 0f);
        handleArea.offsetMax = new Vector2(-10f, 0f);
        var handle = NewImage("Handle", handleArea, Color.white);
        handle.anchorMin = new Vector2(0f, 0f);
        handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(24f, 0f);

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        return slider;
    }

    private static RectTransform NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return rect;
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
        colors.highlightedColor = new Color(1f, 0.75f, 0.6f, 1f);
        colors.pressedColor = new Color(0.7f, 0.4f, 0.3f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var text = CreateLabel(rect, "Label", label, font, 42, Color.white);
        Stretch(text.rectTransform);
    }
    #endregion
}
