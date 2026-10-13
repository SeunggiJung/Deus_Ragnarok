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

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (puzzle == null) puzzle = FindObjectOfType<ColorPuzzle>();
        if (lightObject != null) lightObject.SetActive(false);
        if (glow != null) glow.SetActive(false);
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
    }
}
