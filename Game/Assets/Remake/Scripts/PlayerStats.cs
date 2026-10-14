using UnityEngine;

// 플레이어 전용 수치 (스태미너, 구르기, 피격 경직)
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Deus/Player Stats")]
public class PlayerStats : CharacterStats
{
    [Header("Stamina")]
    public float maxStamina = 70f;
    public float staminaRegen = 10f;        // 초당 기본 회복량
    public float staminaRegenDelay = 0.7f;  // 마지막 소모 후 회복 시작까지 대기 시간
    public float staminaFastRegenTime = 1.5f;   // 마지막 소모 후 이 시간이 지나면 빠른 회복
    public float staminaRegenMultiplier = 1.5f; // 대기 시간 ~ staminaFastRegenTime 구간 배율
    public float staminaFastRegenMultiplier = 3f; // staminaFastRegenTime 이후 배율
    public float attackCost = 6f;
    public float jumpCost = 3f;
    public float dodgeCost = 15f;

    [Header("Combo (지상 3단 콤보, 1~3타 순서)")]
    public float[] comboDamage = { 1f, 1f, 1.5f };         // 공격력 배율 (3타 찌르기는 마무리라 강하게)
    public float[] comboCancelTime = { 0.55f, 0.5f, 0.6f }; // 모션 진행률이 이 값을 넘으면 다음 공격 / 이동 / 점프로 캔슬 가능
    public float[] comboLungeSpeed = { 1.5f, 1.5f, 4.5f };  // 공격하며 앞으로 나가는 속도
    public float[] comboLungeTime = { 0.1f, 0.1f, 0.14f };  // 앞으로 나가는 시간

    [Header("Jump Attack")]
    public float criticalMultiplier = 2f;    // 점프 공격 데미지 배율 (원작 : 점프 중 공격 2배)

    [Header("Dodge")]
    public float dodgeDistance = 3f;
    public float dodgeDuration = 0.35f;
    public float dodgeInvincibleTime = 0.3f; // 구르기 시작부터 무적이 유지되는 시간

    [Header("Level")]
    public float baseExpToNext = 50f;   // Lv1 → Lv2에 필요한 경험치
    public float expGrowth = 1.5f;      // 레벨마다 필요 경험치 배율
    public int pointsPerLevel = 1;      // 레벨업 시 얻는 스탯 포인트
    public float hpPerPoint = 10f;
    public float attackPerPoint = 2f;
    public float staminaPerPoint = 10f;

    [Header("Potion")]
    public float potionHealAmount = 50f;   // 총 회복량
    public float potionHealDuration = 5f;  // 이 시간에 걸쳐 나눠서 회복
    public float potionCooldown = 10f;
    public float potionHitPauseTime = 1f;  // 피격 시 회복이 멈추는 시간 (남은 회복량은 유지)

    [Header("Hit")]
    public float hitStunTime = 0.3f;         // 피격 경직 시간
}
