using UnityEngine;

// 보스 수치. 기본 체력 / 근접 공격(attackDamage, attackRadius)은 CharacterStats 사용
[CreateAssetMenu(fileName = "NewBossStats", menuName = "Deus/Boss Stats")]
public class BossStats : CharacterStats
{
    [Header("Info")]
    public string bossName = "Reinforced Golem";
    public float expReward = 200f;

    [Header("Movement")]
    public float moveSpeed = 1.5f;
    public float baseAnimSpeed = 0.9f;       // 1페이즈 애니메이션(공격 동작) 속도 배율
    public float detectRange = 10f;          // 이 거리 안에 들어오면 전투 시작 (이후 계속 추적)

    [Header("Pacing")]
    public float attackRecovery = 0.8f;      // 공격 패턴이 끝난 뒤 다음 행동까지 쉬는 시간 (플레이어 반격 타이밍)

    [Header("Melee Combo (Attack_2)")]
    public float meleeRange = 3f;
    public float meleeCooldown = 1.2f;

    [Header("Ground Spikes (Attack_1)")]
    public float slamRange = 4f;             // 이 거리 안이면 사용
    public float slamCooldown = 6f;
    public float slamDamage = 20f;
    public Vector2 slamArea = new Vector2(8f, 3f);   // 보스 발밑 중심 판정 범위 (가로 전체, 세로)
    [Range(0f, 1f)] public float slamHitTime = 0.5f; // 애니메이션 진행률이 이 값일 때 가시가 솟음

    [Header("Charge")]
    public float chargeMinRange = 5f;
    public float chargeMaxRange = 12f;
    public float chargeCooldown = 7f;
    public float chargeTelegraph = 0.6f;     // 돌진 전 예고 시간
    public float chargeSpeed = 11f;
    public float chargeDistance = 10f;
    public float chargeDamage = 18f;
    public Color chargeTelegraphTint = new Color(1f, 0.6f, 0.2f);

    [Header("Energy Bolt (Attack_3)")]
    public float throwMinRange = 5f;
    public float throwMaxRange = 14f;
    public float throwCooldown = 4f;
    public float throwDamage = 12f;
    public float throwSpeed = 9f;
    public float throwLifetime = 2f;
    public float throwRadius = 0.35f;
    [Range(0f, 1f)] public float throwReleaseTime = 0.55f;
    public float phase2SpreadAngle = 12f;    // 2페이즈에서 3갈래로 발사할 때 각도
    public Sprite projectileSprite;
    public Color projectileColor = Color.white;

    [Header("Guard (Ability)")]
    public int guardHitCount = 3;            // guardWindow초 안에 이만큼 맞으면 방어 자세
    public float guardWindow = 2f;
    public float guardDuration = 1.5f;
    [Range(0f, 1f)] public float guardDamageMultiplier = 0.2f;
    public float guardCooldown = 10f;
    public Color guardTint = new Color(0.6f, 0.8f, 1f);

    [Header("Poise / Stagger")]
    public float maxPoise = 80f;             // 누적 피해가 이 값을 넘으면 그로기
    public float poiseRegenDelay = 3f;       // 마지막 피격 후 이 시간이 지나면 강인도 회복
    public float staggerTime = 1.5f;
    public Color staggerTint = new Color(0.6f, 0.6f, 0.6f);

    [Header("Phase 2")]
    [Range(0f, 1f)] public float phase2Threshold = 0.5f;
    public float roarTime = 1.3f;
    public float phase2CooldownMultiplier = 0.7f;
    public float phase2MoveMultiplier = 1.3f;
    public float phase2AnimSpeed = 1.2f;
    public Color roarTint = new Color(1f, 0.3f, 0.3f);
    public Color phase2Tint = new Color(1f, 0.8f, 0.8f);

    [Header("Death")]
    public float deathSlowMotionScale = 0.3f;
    public float deathSlowMotionDuration = 0.6f; // 실제 시간 기준
    public float shatterDelay = 0.35f;           // 사망 모션 시작 후 부서지기까지
    public int shatterGrid = 8;                  // 가로 / 세로 조각 수
    public float shatterForce = 6f;
    public float shardLifetime = 1.5f;
}
