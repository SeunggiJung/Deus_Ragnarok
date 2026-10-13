using System;
using System.Collections;
using UnityEngine;

public enum PuzzleColor { None, Red, Blue, Green, Magenta, Orange, White }

// 2스테이지 색 조합 퍼즐.
// 불(ColorFire)에서 G키로 기본색을 2개 담으면 섞인 색이 되고, 맞는 색을 돌(ColorStone)에 G키로 넣으면 불이 켜짐.
// 돌 3개가 모두 켜지면 문(Door2)이 열리고 체력/스태미너 회복.
//
// 원작 Sword_Man과의 차이
// - 원작 : 플레이어 코드 안에 태그(cup, stone) 판정 + 리스트 + switch로 구현, 담는 순서까지 맞아야 조합 성공
// - 변경 : 퍼즐 로직을 별도 컴포넌트로 분리, 불 / 돌은 IInteractable로 G키 상호작용, 순서와 상관없이 조합
//   (조합표는 원작 유지 : 빨강+파랑=자홍, 파랑+초록=주황, 초록+빨강=흰색)
public class ColorPuzzle : MonoBehaviour
{
    [SerializeField] private ColorStone[] stones;
    [SerializeField] private Transform door;
    [SerializeField] private float doorOpenTime = 2f;

    public PuzzleColor Held { get; private set; } = PuzzleColor.None;
    private PuzzleColor pendingPrimary = PuzzleColor.None;
    private bool solved;

    public event Action<PuzzleColor> OnHeldChanged;

    private TextMesh heldLabel;
    private SpriteRenderer heldOrb;
    private Transform player;

    public bool HasMixedColor => IsMixed(Held);

    private void Start()
    {
        var pc = FindObjectOfType<PlayerController>();
        if (pc != null) player = pc.transform;
        CreateIndicator();
        SetHeld(PuzzleColor.None);
    }

    // 불에서 기본색을 담음. 두 번째 색을 담는 순간 섞임
    public void AddPrimary(PuzzleColor color)
    {
        if (solved) return;

        if (pendingPrimary == PuzzleColor.None)
        {
            pendingPrimary = color;
            SetHeld(color);
            return;
        }

        PuzzleColor mixed = Mix(pendingPrimary, color);
        pendingPrimary = PuzzleColor.None;
        SetHeld(mixed);
        if (mixed == PuzzleColor.None)
            Debug.Log("[퍼즐] 조합 실패 (같은 색을 두 번 담음)");
    }

    // 돌에 색을 넣음. 맞으면 true
    public bool TryOffer(ColorStone stone)
    {
        if (solved || !HasMixedColor) return false;

        bool correct = Held == stone.Required;
        SetHeld(PuzzleColor.None); // 맞든 틀리든 들고 있던 색은 사용됨 (원작 동일)
        if (!correct)
        {
            Debug.Log($"[퍼즐] {stone.name}에 맞지 않는 색");
            return false;
        }

        stone.Light();
        CheckSolved();
        return true;
    }

    private void CheckSolved()
    {
        foreach (var s in stones)
            if (s != null && !s.IsLit) return;

        solved = true;
        Debug.Log("[퍼즐] 해결! 문이 열립니다");

        // 원작 : 퍼즐을 풀면 체력 / 스태미너 회복
        var pc = player != null ? player.GetComponent<PlayerController>() : null;
        if (pc != null)
        {
            pc.Heal(pc.MaxHp);
            pc.RefillStamina();
        }

        if (door != null) StartCoroutine(OpenDoor());
    }

    private IEnumerator OpenDoor()
    {
        var rb = door.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }
        var col = door.GetComponent<Collider2D>();
        float height = col != null ? col.bounds.size.y + 0.5f : 10f;

        Vector2 start = door.position;
        Vector2 end = start + Vector2.up * height;
        float elapsed = 0f;
        while (elapsed < doorOpenTime)
        {
            elapsed += Time.fixedDeltaTime;
            Vector2 p = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, elapsed / doorOpenTime));
            if (rb != null) rb.MovePosition(p); else door.position = p;
            yield return new WaitForFixedUpdate();
        }
    }

    public static PuzzleColor Mix(PuzzleColor a, PuzzleColor b)
    {
        if (Pair(a, b, PuzzleColor.Red, PuzzleColor.Blue)) return PuzzleColor.Magenta;
        if (Pair(a, b, PuzzleColor.Blue, PuzzleColor.Green)) return PuzzleColor.Orange;
        if (Pair(a, b, PuzzleColor.Green, PuzzleColor.Red)) return PuzzleColor.White;
        return PuzzleColor.None;
    }

    private static bool Pair(PuzzleColor a, PuzzleColor b, PuzzleColor x, PuzzleColor y) =>
        (a == x && b == y) || (a == y && b == x);

    public static bool IsMixed(PuzzleColor c) =>
        c == PuzzleColor.Magenta || c == PuzzleColor.Orange || c == PuzzleColor.White;

    public static Color ToColor(PuzzleColor c)
    {
        switch (c)
        {
            case PuzzleColor.Red: return new Color(1f, 0.25f, 0.2f);
            case PuzzleColor.Blue: return new Color(0.3f, 0.5f, 1f);
            case PuzzleColor.Green: return new Color(0.25f, 0.95f, 0.35f);
            case PuzzleColor.Magenta: return new Color(1f, 0.25f, 0.9f);
            case PuzzleColor.Orange: return new Color(1f, 0.64f, 0f);
            case PuzzleColor.White: return Color.white;
            default: return Color.clear;
        }
    }

    #region 들고 있는 색 표시 (플레이어 머리 위)
    private void SetHeld(PuzzleColor color)
    {
        Held = color;
        OnHeldChanged?.Invoke(color);
        if (heldOrb == null) return;

        bool show = color != PuzzleColor.None;
        heldOrb.gameObject.SetActive(show);
        heldOrb.color = ToColor(color);
        heldLabel.text = IsMixed(color) ? color.ToString() : $"{color} + ?";
        heldLabel.color = ToColor(color);
    }

    private void CreateIndicator()
    {
        var orb = new GameObject("Held Color");
        heldOrb = orb.AddComponent<SpriteRenderer>();
        Texture2D tex = Texture2D.whiteTexture;
        heldOrb.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        heldOrb.sortingOrder = 55;
        orb.transform.localScale = Vector3.one * 0.35f;

        heldLabel = new GameObject("Label").AddComponent<TextMesh>();
        heldLabel.transform.SetParent(orb.transform, false);
        heldLabel.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        heldLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var mr = heldLabel.GetComponent<MeshRenderer>();
        mr.sharedMaterial = heldLabel.font.material;
        mr.sortingOrder = 56;
        heldLabel.fontSize = 48;
        heldLabel.characterSize = 0.15f;
        heldLabel.anchor = TextAnchor.MiddleCenter;
    }

    private void LateUpdate()
    {
        if (heldOrb == null || player == null || !heldOrb.gameObject.activeSelf) return;
        heldOrb.transform.position = player.position + Vector3.up * 2.2f;
        heldOrb.transform.rotation = Quaternion.Euler(0f, 0f, 45f); // 마름모 모양
        heldLabel.transform.rotation = Quaternion.identity;
    }
    #endregion
}
