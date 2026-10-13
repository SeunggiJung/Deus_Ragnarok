using UnityEngine;

// 색 조합 퍼즐의 불. G키로 기본색(빨강 / 파랑 / 초록 / 노랑)을 담음
[RequireComponent(typeof(Collider2D))]
public class ColorFire : MonoBehaviour, IInteractable
{
    [SerializeField] private PuzzleColor color = PuzzleColor.Red;
    [SerializeField] private ColorPuzzle puzzle;
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 1.5f);

    public string InteractionPrompt => $"Take {color}";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (puzzle == null) puzzle = FindObjectOfType<ColorPuzzle>();
    }

    public bool CanInteract(PlayerController player) => puzzle != null && !player.IsDead;

    public void Interact(PlayerController player)
    {
        puzzle.AddPrimary(color);
    }
}
