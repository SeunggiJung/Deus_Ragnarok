using UnityEngine;
using UnityEngine.UI;

// 플레이어 체력 / 스태미너 HUD. 플레이어의 OnHpChanged / OnStaminaChanged 이벤트를 구독해 갱신
// (플레이어는 UI를 모르고, UI가 플레이어를 구독하는 구조)
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    [Header("Layout")]
    [SerializeField] private Vector2 barSize = new Vector2(260f, 22f);
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);
    [SerializeField] private float spacing = 6f;

    [Header("Colors")]
    [SerializeField] private Color hpColor = new Color(0.85f, 0.2f, 0.2f);
    [SerializeField] private Color staminaColor = new Color(0.95f, 0.75f, 0.2f);

    private UIBar hpBar;
    private UIBar staminaBar;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        BuildCanvas();
    }

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("HUD Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        hpBar = UIBar.Create(canvasGo.transform, "HP Bar", "HP", hpColor,
                             new Vector2(margin.x, -margin.y), barSize);
        staminaBar = UIBar.Create(canvasGo.transform, "Stamina Bar", "ST", staminaColor,
                                  new Vector2(margin.x, -margin.y - barSize.y - spacing), barSize);
    }

    private void OnEnable()
    {
        if (player == null) return;
        player.OnHpChanged += hpBar.Set;
        player.OnStaminaChanged += staminaBar.Set;
    }

    private void Start()
    {
        // 이벤트는 변화가 있을 때만 오므로 시작 값은 직접 한 번 반영
        if (player == null) return;
        hpBar.Set(player.CurrentHp, player.MaxHp);
        staminaBar.Set(player.CurrentStamina, player.Stats.maxStamina);
    }

    private void OnDisable()
    {
        if (player == null) return;
        player.OnHpChanged -= hpBar.Set;
        player.OnStaminaChanged -= staminaBar.Set;
    }
}
