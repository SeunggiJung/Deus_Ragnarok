using UnityEngine;

// 색 조합 퍼즐의 돌. 맞는 조합색을 G키로 넣으면 연결된 불빛(Light 타일맵)과 돌의 Glow가 켜짐
[RequireComponent(typeof(Collider2D))]
public class ColorStone : MonoBehaviour, IInteractable
{
    [SerializeField] private PuzzleColor required = PuzzleColor.White;
    [SerializeField] private ColorPuzzle puzzle;
    [SerializeField] private GameObject lightObject;   // 위쪽 표시등 (Light1 ~ 3)
    [SerializeField] private GameObject glow;          // 돌의 빛 (자식 Glow)
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 1.5f);

    public PuzzleColor Required => required;
    public bool IsLit { get; private set; }

    public string InteractionPrompt => "Offer Color";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private SpriteRenderer hintOrb;
    private TextMesh hintLabel;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (puzzle == null) puzzle = FindObjectOfType<ColorPuzzle>();
        if (lightObject != null) lightObject.SetActive(false);
        if (glow != null) glow.SetActive(false);
    }

    private void Start() => CreateHint();

    // 필요한 색 힌트 : 돌 위에 해당 색 마름모 + 색 이름. 켜지면 흐려짐
    private void CreateHint()
    {
        Color c = ColorPuzzle.ToColor(required);

        var orb = new GameObject("Required Color Hint");
        orb.transform.SetParent(transform, false);
        orb.transform.position = transform.position + Vector3.up * 3.2f;
        orb.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        orb.transform.localScale = Vector3.one * 0.5f;
        hintOrb = orb.AddComponent<SpriteRenderer>();
        Texture2D tex = Texture2D.whiteTexture;
        hintOrb.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        hintOrb.sortingOrder = 55;
        hintOrb.color = c;

        hintLabel = new GameObject("Label").AddComponent<TextMesh>();
        hintLabel.transform.SetParent(transform, false);
        hintLabel.transform.position = orb.transform.position + Vector3.up * 0.9f;
        hintLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = hintLabel.GetComponent<MeshRenderer>();
        mr.sharedMaterial = hintLabel.font.material;
        mr.sortingOrder = 56;
        hintLabel.text = required.ToString();
        hintLabel.fontSize = 48;
        hintLabel.characterSize = 0.12f;
        hintLabel.anchor = TextAnchor.MiddleCenter;
        hintLabel.color = c;
    }

    // 조합색을 들고 있을 때만 상호작용 가능
    public bool CanInteract(PlayerController player) => !IsLit && puzzle != null && puzzle.HasMixedColor && !player.IsDead;

    public void Interact(PlayerController player)
    {
        puzzle.TryOffer(this);
    }

    public void Light()
    {
        IsLit = true;
        if (lightObject != null) lightObject.SetActive(true);
        if (glow != null) glow.SetActive(true);
        if (hintOrb != null) hintOrb.color = ColorPuzzle.ToColor(required) * new Color(1f, 1f, 1f, 0.35f);
        if (hintLabel != null) hintLabel.color = ColorPuzzle.ToColor(required) * new Color(1f, 1f, 1f, 0.35f);
    }
}
