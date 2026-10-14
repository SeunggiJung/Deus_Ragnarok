using UnityEngine;
using UnityEngine.UI;

// 플레이어 HUD : 체력 / 스태미너 / 경험치 게이지 + 레벨업 스탯 선택 패널.
// 플레이어와 PlayerProgression의 이벤트를 구독해 갱신 (플레이어는 UI를 모르고, UI가 구독하는 구조)
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerProgression progression;

    [Header("Layout")]
    [SerializeField] private Vector2 barSize = new Vector2(260f, 22f);
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);
    [SerializeField] private float spacing = 6f;

    [Header("Colors")]
    [SerializeField] private Color hpColor = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private Color staminaColor = new Color(0.95f, 0.75f, 0.2f);
    [SerializeField] private Color expColor = new Color(0.3f, 0.6f, 1f);

    private UIBar hpBar;
    private UIBar staminaBar;
    private UIBar expBar;
    private GameObject levelUpPanel;
    private Text levelUpTitle;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();
        if (progression == null && player != null)
            progression = player.GetComponent<PlayerProgression>();

        BuildCanvas();

        // 사망 화면 (홈으로 / 체크포인트에서 재시작)
        if (player != null && GetComponent<DeathScreen>() == null)
            gameObject.AddComponent<DeathScreen>().Init(player);
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        float step = barSize.y + spacing;
        hpBar = UIBar.Create(canvasGo.transform, "HP Bar", "HP", hpColor,
                             new Vector2(margin.x, -margin.y), barSize);
        staminaBar = UIBar.Create(canvasGo.transform, "Stamina Bar", "ST", staminaColor,
                                  new Vector2(margin.x, -margin.y - step), barSize);
        expBar = UIBar.Create(canvasGo.transform, "Exp Bar", "LV 1  EXP", expColor,
                              new Vector2(margin.x, -margin.y - step * 2f), barSize);

        BuildLevelUpPanel(canvasGo.transform, new Vector2(margin.x, -margin.y - step * 3f - spacing));
    }

    // 레벨업 시 나타나는 스탯 선택 패널 (버튼 클릭 또는 숫자키 2/3/4, 1은 포션)
    private void BuildLevelUpPanel(Transform parent, Vector2 anchoredPos)
    {
        levelUpPanel = new GameObject("Level Up Panel", typeof(RectTransform), typeof(Image));
        var rect = levelUpPanel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(barSize.x, 130f);
        levelUpPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        var titleRect = new GameObject("Title", typeof(RectTransform)).GetComponent<RectTransform>();
        titleRect.SetParent(rect, false);
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 28f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        levelUpTitle = UIBar.CreateText(titleRect, "Text", 16);
        levelUpTitle.color = new Color(1f, 0.9f, 0.4f);

        PlayerStats s = player != null ? player.Stats : null;
        CreateStatButton(rect, 0, $"[2]  HP +{s?.hpPerPoint}", StatType.Hp);
        CreateStatButton(rect, 1, $"[3]  ATK +{s?.attackPerPoint}", StatType.Attack);
        CreateStatButton(rect, 2, $"[4]  ST +{s?.staminaPerPoint}", StatType.Stamina);

        levelUpPanel.SetActive(false);
    }

    private void CreateStatButton(RectTransform parent, int index, string label, StatType type)
    {
        var go = new GameObject($"{type} Button", typeof(RectTransform), typeof(Image), typeof(Button));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-16f, 26f);
        rect.anchoredPosition = new Vector2(0f, -36f - index * 30f);
        go.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f, 1f);

        UIBar.CreateText(rect, "Text", 14).text = label;
        go.GetComponent<Button>().onClick.AddListener(() => progression.SpendPoint(type));
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.OnHpChanged += hpBar.Set;
            player.OnStaminaChanged += staminaBar.Set;
        }
        if (progression != null)
        {
            progression.OnExpChanged += HandleExpChanged;
            progression.OnLevelUp += HandleLevelUp;
            progression.OnStatPointsChanged += HandleStatPointsChanged;
        }
    }

    private void Start()
    {
        // 이벤트는 변화가 있을 때만 오므로 시작 값은 직접 한 번 반영
        if (player != null)
        {
            hpBar.Set(player.CurrentHp, player.MaxHp);
            staminaBar.Set(player.CurrentStamina, player.MaxStamina);
        }
        if (progression != null)
        {
            HandleLevelUp(progression.Level);
            HandleExpChanged(progression.Exp, progression.ExpToNext);
            HandleStatPointsChanged(progression.StatPoints);
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.OnHpChanged -= hpBar.Set;
            player.OnStaminaChanged -= staminaBar.Set;
        }
        if (progression != null)
        {
            progression.OnExpChanged -= HandleExpChanged;
            progression.OnLevelUp -= HandleLevelUp;
            progression.OnStatPointsChanged -= HandleStatPointsChanged;
        }
    }

    private void Update()
    {
        // 레벨업 패널이 떠 있을 때 숫자키로도 선택 가능 (UI 전용 단축키라 PlayerInput이 아닌 HUD에서 처리, 1은 포션 키라 2부터)
        if (progression == null || !levelUpPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Alpha2)) progression.SpendPoint(StatType.Hp);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) progression.SpendPoint(StatType.Attack);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) progression.SpendPoint(StatType.Stamina);
    }

    private void HandleExpChanged(float exp, float expToNext)
    {
        expBar.Set(exp, expToNext);
    }

    private void HandleLevelUp(int level)
    {
        expBar.SetPrefix($"LV {level}  EXP");
        expBar.Set(progression.Exp, progression.ExpToNext);
    }

    private void HandleStatPointsChanged(int points)
    {
        levelUpPanel.SetActive(points > 0);
        levelUpTitle.text = $"LEVEL UP!  Points : {points}";
    }
}
