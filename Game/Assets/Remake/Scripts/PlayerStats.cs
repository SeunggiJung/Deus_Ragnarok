using UnityEngine;

// 플레이어 전용 수치 (스태미너, 구르기, 피격 경직)
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Deus/Player Stats")]
public class PlayerStats : CharacterStats
{
    [Header("Stamina")]
    public float maxStamina = 70f;
    public float staminaRegen = 10f;        // 초당 회복량
    public float staminaRegenDelay = 0.7f;  // 마지막 소모 후 회복 시작까지 대기 시간
    public float attackCost = 6f;
    public float jumpCost = 3f;
    public float dodgeCost = 15f;

    [Header("Jump Attack")]
    public float criticalMultiplier = 2f;    // 점프 공격 데미지 배율 (원작 : 점프 중 공격 2배)

    [Header("Dodge")]
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.35f;
    public float dodgeInvincibleTime = 0.3f; // 구르기 시작부터 무적이 유지되는 시간

    [Header("Hit")]
    public float hitStunTime = 0.3f;         // 피격 경직 시간
}
