using UnityEngine;
using UnityEngine.UI;

// 화면 왼쪽 아래 포션 슬롯. 쿨타임 동안 시계 방향으로 걷히는 어두운 덮개와 남은 초를 표시,
// 회복 중에는 슬롯 테두리가 초록색으로 빛남
public class PotionSlotUI : MonoBehaviour
{
    [SerializeField] private PlayerPotion potion;
    [SerializeField] private float slotSize = 72f;
    [SerializeField] private Vector2 margin = new Vector2(20f, 20f);
    [SerializeField] private string keyLabel = "1";

    private Image border;
    private Image cooldownOverlay;
    private Text cooldownText;

    private static readonly Color BorderIdle = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    private static readonly Color BorderHealing = new Color(0.3f, 0.9f, 0.4f, 1f);

    private void Awake()
    {
        if (potion == null)
            potion = FindObjectOfType<PlayerPotion>();
        Build();
    }

    private void Build()
    {
        var canvasGo = new GameObject("Potion Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 테두리 (회복 중 색 변화)
        border = CreateImage(canvasGo.transform, "Potion Slot", null, BorderIdle);
        var slot = border.rectTransform;
        slot.anchorMin = slot.anchorMax = Vector2.zero; // 왼쪽 아래
        slot.pivot = Vector2.zero;
        slot.anchoredPosition = margin;
        slot.sizeDelta = new Vector2(slotSize, slotSize);

        // 배경
        Image background = CreateImage(slot, "Background", null, new Color(0.05f, 0.05f, 0.08f, 0.9f));
        Stretch(background.rectTransform, 3f);

        // 포션 아이콘
        Image icon = CreateImage(slot, "Icon", CreatePotionSprite(), Color.white);
        Stretch(icon.rectTransform, 10f);
        icon.preserveAspect = true;

        // 쿨타임 덮개 : 남은 비율만큼 어둡게, 12시 방향부터 시계 방향으로 걷힘
        cooldownOverlay = CreateImage(slot, "Cooldown", WhiteSprite(), new Color(0f, 0f, 0f, 0.7f));
        Stretch(cooldownOverlay.rectTransform, 3f);
        cooldownOverlay.type = Image.Type.Filled;
        cooldownOverlay.fillMethod = Image.FillMethod.Radial360;
        cooldownOverlay.fillOrigin = (int)Image.Origin360.Top;
        cooldownOverlay.fillClockwise = false;
        cooldownOverlay.fillAmount = 0f;

        // 남은 쿨타임 (초)
        var cdRect = new GameObject("Cooldown Text", typeof(RectTransform)).GetComponent<RectTransform>();
        cdRect.SetParent(slot, false);
        Stretch(cdRect, 0f);
        cooldownText = UIBar.CreateText(cdRect, "Text", 26);
        cooldownText.fontStyle = FontStyle.Bold;

        // 키 표시 (왼쪽 위)
        var keyRect = new GameObject("Key", typeof(RectTransform)).GetComponent<RectTransform>();
        keyRect.SetParent(slot, false);
        keyRect.anchorMin = keyRect.anchorMax = new Vector2(0f, 1f);
        keyRect.pivot = new Vector2(0f, 1f);
        keyRect.anchoredPosition = new Vector2(5f, -3f);
        keyRect.sizeDelta = new Vector2(20f, 20f);
        Text key = UIBar.CreateText(keyRect, "Text", 14);
        key.alignment = TextAnchor.UpperLeft;
        key.text = keyLabel;
    }

    private void Update()
    {
        if (potion == null) return;

        float remaining = potion.CooldownRemaining;
        cooldownOverlay.fillAmount = potion.Cooldown > 0f ? remaining / potion.Cooldown : 0f;
        cooldownText.text = remaining > 0f ? Mathf.CeilToInt(remaining).ToString() : "";

        if (potion.IsHealing)
        {
            // 회복 중 : 테두리가 초록색으로 맥동
            float pulse = 0.6f + 0.4f * Mathf.Sin(Time.time * 6f);
            border.color = Color.Lerp(BorderIdle, BorderHealing, pulse);
        }
        else
        {
            border.color = BorderIdle;
        }
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    // Filled 타입 Image는 스프라이트가 있어야 동작하므로 흰색 스프라이트 생성
    private static Sprite WhiteSprite()
    {
        Texture2D tex = Texture2D.whiteTexture;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    // 별도 이미지 에셋 없이 코드로 그린 붉은 물약 아이콘 (둥근 병 + 목 + 코르크)
    private static Sprite CreatePotionSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var clear = new Color(0f, 0f, 0f, 0f);
        var glass = new Color(0.85f, 0.9f, 1f, 1f);
        var liquid = new Color(0.85f, 0.1f, 0.15f, 1f);
        var shine = new Color(1f, 0.6f, 0.6f, 1f);
        var cork = new Color(0.55f, 0.35f, 0.2f, 1f);

        Vector2 bodyCenter = new Vector2(15.5f, 11f);
        const float bodyRadius = 10f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color c = clear;
                float d = Vector2.Distance(new Vector2(x, y), bodyCenter);
                bool inNeck = x >= 12 && x <= 19 && y >= 19 && y <= 26;
                bool inCork = x >= 11 && x <= 20 && y >= 27 && y <= 30;

                if (d <= bodyRadius)
                {
                    c = d >= bodyRadius - 1.2f ? glass : (y <= 15 ? liquid : glass * new Color(1f, 1f, 1f, 0.35f));
                    if (y <= 15 && x >= 9 && x <= 11 && y >= 7 && y <= 12) c = shine; // 반사광
                }
                else if (inNeck)
                {
                    c = (x == 12 || x == 19) ? glass : glass * new Color(1f, 1f, 1f, 0.35f);
                }
                else if (inCork)
                {
                    c = cork;
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
