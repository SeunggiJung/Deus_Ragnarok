using UnityEngine;

// 2스테이지 드래곤 보스 수치. 공통 보스 수치(BossStats)에 화염 브레스 / 화염구 비 / 붙잡아 내려찍기 수치를 더함
[CreateAssetMenu(fileName = "NewDragonBossStats", menuName = "Deus/Dragon Boss Stats")]
public class DragonBossStats : BossStats
{
    [Header("Fire Breath (화염 브레스) - 기를 모으는 후반에 점프 공격으로 카운터")]
    public float breathMinRange = 3f;
    public float breathMaxRange = 13f;
    public float breathCooldown = 9f;
    public float breathChargeTime = 2.0f;       // 기 모으는 시간 (끝 40%가 카운터 창)
    [Range(0.1f, 0.8f)] public float breathWindowRatio = 0.4f;
    public float breathDuration = 0.8f;         // 불줄기 유지 시간
    public float breathLength = 9f;
    public float breathHeight = 1.8f;
    public float breathTickDamage = 9f;         // 0.25초마다 한 번 (구르기 무적으로 통과 가능)
    public float breathTickInterval = 0.25f;
    public float breathRecovery = 0.8f;
    public float counterStaggerTime = 3f;       // 카운터 성공 시 그로기 시간

    [Header("Fire Rain (화염구 비)")]
    public float rainMinRange = 0f;
    public float rainMaxRange = 22f;
    public float rainCooldown = 12f;
    public int rainCount = 4;                   // 2페이즈에서는 +2
    public float rainInterval = 0.55f;          // 낙하 지점이 하나씩 생기는 간격
    public float rainWarnTime = 1.0f;           // 지점이 표시된 뒤 떨어지기까지 (이 동안 벗어나야 함)
    public float rainRadius = 1.7f;
    public float rainDamage = 22f;
    public float rainSpread = 3.5f;             // 플레이어 위치 기준 좌우로 흩어지는 폭

    [Header("Seize Slam (붙잡아 내려찍기)")]
    public float seizeRange = 7f;               // 이 거리 안이면 사용
    public float seizeCooldown = 13f;
    public float seizeRadius = 4.6f;            // 보스 주변 범위 (제한 시간 안에 이 밖으로 나가야 함)
    public float seizeTimeLimit = 1.5f;         // 제한 시간 (2페이즈에서는 0.8배)
    public float seizeLungeSpeed = 22f;
    public float seizeLiftHeight = 3.2f;
    public float seizeLiftTime = 0.45f;
    public float seizeSlamTime = 0.12f;
    public float seizeDamage = 38f;
    public float seizePlayerKnockback = 5f;     // 내려찍힌 뒤 플레이어가 날아가는 거리
    public float seizeBossKnockback = 2.5f;     // 보스가 반대쪽으로 밀려나는 거리
    public float seizeBossStagger = 1.5f;       // 성공한 뒤 보스의 그로기 시간
    public float seizeMissRecovery = 1.0f;      // 범위 밖으로 피했을 때 보스가 멈칫하는 시간

    [Header("Sprites (빌더가 연결)")]
    public Sprite[] breathFrames;               // 불줄기 (iblast)
    public Sprite[] fireballFrames;             // 떨어지는 화염구
    public Sprite[] explosionFrames;            // 착탄 폭발
}
