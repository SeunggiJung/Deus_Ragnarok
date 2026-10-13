using UnityEngine;

// 2스테이지 Crab 보스 수치. 공통 보스 수치(BossStats)에 바닥 찍기 / 잡기 패턴 수치를 더함
[CreateAssetMenu(fileName = "NewCrabBossStats", menuName = "Deus/Crab Boss Stats")]
public class CrabBossStats : BossStats
{
    [Header("Ground Pound (바닥 찍기)")]
    public float poundRange = 9f;               // 이 거리 안이면 사용
    public float poundCooldown = 7f;
    public float poundAnimSpeed = 0.65f;        // 동작을 느리게 해 예고 시간 확보
    [Range(0f, 1f)] public float poundHitTime = 0.55f; // 애니메이션 진행률이 이 값일 때 내려찍음
    public float poundDamage = 22f;             // 발밑 직접 피해 (poundArea 범위)
    public Vector2 poundArea = new Vector2(6f, 2.5f);
    public float waveSpeed = 8f;                // 좌우로 퍼지는 충격파 (점프로 넘거나 구르기로 회피)
    public float waveDistance = 10f;
    public float waveHeight = 0.9f;
    public float waveDamage = 18f;
    public Color waveColor = new Color(1f, 0.55f, 0.2f, 0.9f);
    public Color poundTelegraphTint = new Color(1f, 0.4f, 0.4f);

    [Header("Heavy Strike (기 모아 강타) - 타이밍 맞춘 점프 공격으로 카운터")]
    public float heavyRange = 7f;
    public float heavyCooldown = 10f;
    public float heavyChargeTime = 2.0f;        // 기 모으는 시간 (끝 40%가 카운터 창)
    public float heavyDamage = 40f;
    public Vector2 heavyArea = new Vector2(4.5f, 3f);
    public float heavyRecovery = 0.9f;
    public float counterStaggerTime = 3f;       // 카운터 성공 시 그로기 시간

    [Header("Leap Slam (도약 내려찍기)")]
    public float leapMinRange = 2f;
    public float leapMaxRange = 18f;
    public float leapCooldown = 11f;
    public float leapGatherTime = 1.0f;         // 기 모으는 시간 (예고)
    public float leapHeight = 14f;              // 화면 밖으로 사라질 만큼 높이
    public float leapRiseTime = 0.45f;
    public float leapTrackTime = 1.6f;          // 장판이 플레이어를 따라다니는 시간
    public float leapTrackSpeed = 9f;
    public float leapLockTime = 0.8f;           // 장판 고정 후 내려찍기까지 (이 동안 피해야 함)
    public float leapFallTime = 0.1f;
    public float leapRadius = 3.4f;             // 장판 반지름
    public float leapDamage = 32f;

    [Header("Grab (잡기)")]
    public float grabRange = 7f;
    public float grabCooldown = 9f;
    public float grabTelegraph = 1.3f;          // 예고 : 이 동안 구르기 준비
    public float grabLungeSpeed = 12f;
    public float grabLungeTime = 0.35f;
    public float grabHoldTime = 0.9f;           // 잡힌 채 제압당하는 시간
    public float grabDamage = 30f;
    public float grabMissRecovery = 1.2f;       // 빗나가면 보스가 무방비인 시간 (구르기 성공 보상)
    public Color grabTelegraphTint = new Color(1f, 0.9f, 0.3f);
}
