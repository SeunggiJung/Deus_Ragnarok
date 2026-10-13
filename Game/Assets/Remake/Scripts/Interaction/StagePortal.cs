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

    [Header("Lock")]
    [Tooltip("지정하면 이 적을 처치해야 포탈 사용 가능 (원작 : 드래곤 처치 후 이동 가능)")]
    [SerializeField] private BaseCharacter requiredDefeated;
    [SerializeField] private string lockedPrompt = "Defeat the guardian";

    private bool used;

    private bool IsLocked => requiredDefeated != null && !requiredDefeated.IsDead;
    public string InteractionPrompt => IsLocked ? lockedPrompt : prompt;
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public bool CanInteract(PlayerController player) => !used && !player.IsDead;

    public void Interact(PlayerController player)
    {
        if (!CanInteract(player)) return;
        if (IsLocked)
        {
            Debug.Log($"포탈 잠김 : {lockedPrompt}");
            return;
        }
        used = true;

        player.InputLocked = true;
        player.SetVelocity(0f, player.Rb.velocity.y);
        PlayerSession.Save(player); // 다음 스테이지로 레벨 / 스탯 / 무기 이어가기
        Debug.Log($"포탈 : {nextSceneName}(으)로 이동");
        ScreenFader.FadeAndLoad(nextSceneName, fadeDuration);
    }
}
