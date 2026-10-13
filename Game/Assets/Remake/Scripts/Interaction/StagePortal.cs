using UnityEngine;

// 스테이지 끝 포탈. G키로 상호작용하면 입력을 잠그고 화면을 어둡게 한 뒤 다음 씬으로 이동
// (원작 Sword_Man : portal 태그에 닿은 상태에서 G키 → second_stage 로드 방식 계승)
[RequireComponent(typeof(Collider2D))]
public class StagePortal : MonoBehaviour, IInteractable
{
    [SerializeField] private string nextSceneName = "second_stage";
    [SerializeField] private string prompt = "Next Stage";
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 1.6f);
    [SerializeField] private float fadeDuration = 0.8f;

    private bool used;

    public string InteractionPrompt => prompt;
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public bool CanInteract(PlayerController player) => !used && !player.IsDead;

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        used = true;

        player.InputLocked = true;
        player.SetVelocity(0f, player.Rb.velocity.y);
        Debug.Log($"포탈 : {nextSceneName}(으)로 이동");
        ScreenFader.FadeAndLoad(nextSceneName, fadeDuration);
    }
}
