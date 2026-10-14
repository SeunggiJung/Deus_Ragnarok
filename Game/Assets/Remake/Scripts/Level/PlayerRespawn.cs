using UnityEngine;
using UnityEngine.SceneManagement;

// 낭떠러지 낙사 처리. 일정 높이 아래로 떨어지면 마지막 체크포인트로 돌아오고 최대 체력의 일부를 잃음
// (원작 Sword_Man : y <= -5면 체력 20% 감소 후 시작 지점으로 이동하던 규칙 계승)
[RequireComponent(typeof(PlayerController))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private float fallY = -8f;
    [Range(0f, 1f)]
    [SerializeField] private float fallDamageRatio = 0.2f; // 최대 체력 대비

    private PlayerController player;
    private Vector2 respawnPoint;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        respawnPoint = transform.position; // 체크포인트를 밟기 전에는 시작 지점
    }

    public void SetCheckpoint(Vector2 point)
    {
        respawnPoint = point;
        PlayerSession.SetCheckpoint(SceneManager.GetActiveScene().name, point, player); // 이 시점의 성장도 함께 스냅샷
    }

    // 사망 후 씬을 다시 불러왔을 때 저장된 체크포인트에서 시작
    public void ApplySavedCheckpoint()
    {
        if (!PlayerSession.TryGetCheckpoint(SceneManager.GetActiveScene().name, out Vector2 point)) return;
        respawnPoint = point;
        transform.position = point;
        player.SetVelocity(0f, 0f);
    }

    // 함정 등에 맞았을 때 마지막 체크포인트로 이동 (피해는 호출한 쪽에서 처리)
    public void ReturnToCheckpoint()
    {
        transform.position = respawnPoint;
        player.SetVelocity(0f, 0f);
    }

    private void Update()
    {
        if (player.IsDead || transform.position.y > fallY) return;

        ReturnToCheckpoint();
        player.ForceInvincible = false; // 공중 구르기 도중 떨어진 경우에도 피해를 받도록
        player.TakeDamage(player.MaxHp * fallDamageRatio);
        Debug.Log($"낙사 : 체크포인트 {respawnPoint}로 복귀, 체력 -{player.MaxHp * fallDamageRatio}");
    }
}
