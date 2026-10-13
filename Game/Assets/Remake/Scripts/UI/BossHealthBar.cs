using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 하단 보스 체력바. 보스가 전투를 시작(OnEngaged)하면 나타나고, 처치되면 잠시 뒤 사라짐
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private Vector2 size = new Vector2(800f, 24f);
    [SerializeField] private float bottomMargin = 60f;
    [SerializeField] private Color fillColor = new Color(0.75f, 0.1f, 0.1f);

    private GameObject root;
    private UIBar bar;
    private Text nameLabel;

    private void Awake()
    {
        if (boss == null)
            boss = FindObjectOfType<BossController>();
        Build();
    }

    private void Build()
    {
        var canvasGo = new GameObject("Boss Bar Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root = new GameObject("Boss Bar", typeof(RectTransform));
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(canvasGo.transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottomMargin);
        rect.sizeDelta = new Vector2(size.x, size.y + 30f);

        // UIBar는 좌상단 앵커 기준으로 만들어지므로 이 영역의 좌상단 아래 30px에 배치
        bar = UIBar.Create(rect, "HP", "", fillColor, new Vector2(0f, -30f), size);

        var nameRect = new GameObject("Name", typeof(RectTransform)).GetComponent<RectTransform>();
        nameRect.SetParent(rect, false);
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.sizeDelta = new Vector2(0f, 28f);
        nameLabel = UIBar.CreateText(nameRect, "Text", 20);

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (boss == null) return;
        boss.OnEngaged += Show;
        boss.OnPhase2 += HandlePhase2;
        boss.OnDefeated += HandleDefeated;
        boss.OnHpChanged += bar.Set;
    }

    private void OnDisable()
    {
        if (boss == null) return;
        boss.OnEngaged -= Show;
        boss.OnPhase2 -= HandlePhase2;
        boss.OnDefeated -= HandleDefeated;
        boss.OnHpChanged -= bar.Set;
    }

    private void Show()
    {
        nameLabel.text = boss.Stats.bossName;
        bar.Set(boss.CurrentHp, boss.MaxHp);
        root.SetActive(true);
    }

    private void HandlePhase2()
    {
        nameLabel.text = $"{boss.Stats.bossName}  <color=#ff6666>- Enraged -</color>";
    }

    private void HandleDefeated()
    {
        StartCoroutine(HideAfter(2f));
    }

    private IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        root.SetActive(false);
    }
}
