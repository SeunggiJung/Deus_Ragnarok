using UnityEngine;
using UnityEngine.UI;

// 화면 UI용 단순 게이지. 배경 Image 위에 채움 Image를 깔고, 채움의 오른쪽 앵커로 비율을 표현
// (스프라이트 없이 동작하도록 Image.fillAmount 대신 앵커 사용)
public class UIBar : MonoBehaviour
{
    [SerializeField] private RectTransform fill;
    [SerializeField] private Text label;
    [SerializeField] private string labelPrefix;

    // 라벨 앞 문구 변경 (예 : 경험치 바의 "LV 3 EXP")
    public void SetPrefix(string prefix)
    {
        labelPrefix = prefix;
    }

    public void Set(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        fill.anchorMax = new Vector2(ratio, 1f);

        if (label != null)
            label.text = $"{labelPrefix} {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    // 코드로 게이지를 생성 (씬에 UI를 미리 배치하지 않아도 되도록)
    public static UIBar Create(Transform parent, string name, string labelPrefix, Color fillColor,
                               Vector2 anchoredPos, Vector2 size)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 1f); // 좌상단 기준
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = anchoredPos;
        rootRect.sizeDelta = size;
        var rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.6f);
        rootImage.raycastTarget = false; // 게이지 위를 클릭해도 공격 입력이 막히지 않도록

        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.SetParent(rootRect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
        var fillImage = fillGo.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.raycastTarget = false;

        Text text = CreateText(rootRect, "Label", 14);

        var bar = root.AddComponent<UIBar>();
        bar.fill = fillRect;
        bar.label = text;
        bar.labelPrefix = labelPrefix;
        return bar;
    }

    // 부모 영역을 꽉 채우는 가운데 정렬 텍스트 생성 (HUD 공용)
    public static Text CreateText(RectTransform parent, string name, int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
