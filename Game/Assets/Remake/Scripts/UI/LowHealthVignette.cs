using UnityEngine;
using UnityEngine.UI;

// 체력이 낮을 때 화면 가장자리가 붉게 일렁이는 연출.
// 가장자리로 갈수록 진해지는 붉은 텍스처를 코드로 만들어 화면 전체에 덮고, 알파를 맥동시킴
public class LowHealthVignette : MonoBehaviour
{
    [SerializeField] private PlayerController player;

    [Header("Trigger")]
    [Range(0f, 1f)]
    [SerializeField] private float threshold = 0.2f;      // 최대 체력의 이 비율 이하에서 시작

    [Header("Look")]
    [SerializeField] private Color color = new Color(0.8f, 0f, 0f, 1f);
    [Range(0f, 1f)]
    [SerializeField] private float innerRadius = 0.55f;   // 이 안쪽은 투명 (0 : 중앙, 1 : 가장자리)
    [SerializeField] private float minAlpha = 0.25f;      // 맥동 최저 투명도
    [SerializeField] private float maxAlpha = 0.75f;      // 맥동 최고 투명도
    [SerializeField] private float pulseSpeed = 1.5f;     // 초당 맥동 횟수 (체력이 낮을수록 최대 2배까지 빨라짐)
    [SerializeField] private float fadeSpeed = 3f;        // 켜지고 꺼질 때의 전환 속도

    private RawImage overlay;
    private float hpRatio = 1f;
    private float strength;   // 0 ~ 1, 연출 전체 세기 (부드럽게 켜고 끄기용)
    private float pulseTime;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        Build();
    }

    private void Build()
    {
        var canvasGo = new GameObject("Vignette Canvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; // HUD(100)보다 아래

        var imageGo = new GameObject("Vignette", typeof(RectTransform), typeof(RawImage));
        var rect = imageGo.GetComponent<RectTransform>();
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        overlay = imageGo.GetComponent<RawImage>();
        overlay.texture = CreateVignetteTexture(256, innerRadius);
        overlay.raycastTarget = false;
        SetAlpha(0f);
    }

    // 중앙은 투명, 가장자리로 갈수록 불투명해지는 흰색 텍스처 (색은 RawImage.color로 입힘)
    private static Texture2D CreateVignetteTexture(int size, float inner)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // -1 ~ 1 정규화 좌표, 모서리 쪽이 더 진하도록 원형 거리 사용
                float u = x / (size - 1f) * 2f - 1f;
                float v = y / (size - 1f) * 2f - 1f;
                float dist = Mathf.Sqrt(u * u + v * v) / Mathf.Sqrt(2f) * 1.4f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, 1f, dist));
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void OnEnable()
    {
        if (player != null) player.OnHpChanged += HandleHpChanged;
    }

    private void Start()
    {
        if (player != null) HandleHpChanged(player.CurrentHp, player.MaxHp);
    }

    private void OnDisable()
    {
        if (player != null) player.OnHpChanged -= HandleHpChanged;
    }

    private void HandleHpChanged(float current, float max)
    {
        hpRatio = max > 0f ? current / max : 0f;
    }

    private void Update()
    {
        bool active = hpRatio <= threshold;
        strength = Mathf.MoveTowards(strength, active ? 1f : 0f, fadeSpeed * Time.deltaTime);

        if (strength <= 0f)
        {
            SetAlpha(0f);
            return;
        }

        // 체력이 낮을수록(임계값 → 0) 더 빠르게 맥동
        float danger = threshold > 0f ? 1f - Mathf.Clamp01(hpRatio / threshold) : 1f;
        pulseTime += Time.deltaTime * pulseSpeed * (1f + danger);
        float pulse = 0.5f + 0.5f * Mathf.Sin(pulseTime * Mathf.PI * 2f);

        SetAlpha(Mathf.Lerp(minAlpha, maxAlpha, pulse) * strength);
    }

    private void SetAlpha(float a)
    {
        Color c = color;
        c.a = a;
        overlay.color = c;
    }
}
