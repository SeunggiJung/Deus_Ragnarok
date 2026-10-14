using UnityEngine;

// 3스테이지 하데스 보스 수치. 공통 보스 수치(BossStats)에 2형태(되살아남) / 화염탄 낙하 / 근접 공격 수치를 더함
// 원작 : 1형태(Hades_alive) 체력 1000 / 공격 30, 레버를 당기면 거대해져 되살아남(Hades_revive) 체력 1500 / 공격 40
[CreateAssetMenu(fileName = "NewHadesBossStats", menuName = "Deus/Hades Boss Stats")]
public class HadesBossStats : BossStats
{
    [Header("Form 2 (되살아난 하데스)")]
    public float form2MaxHp = 1200f;
    public float form2AttackDamage = 34f;
    public float form2ScaleMultiplier = 0.85f;   // 원작 : 크기 10 → 8.5
    public float formSwitchDeathTime = 3f;       // 1형태가 쓰러진 뒤 모습이 사라지기까지

    [Header("Melee (근접 공격)")]
    public float meleeReach = 6.5f;              // 앞쪽으로 닿는 거리 (원작 공격 범위 6)
    public float meleeDuration = 1.3f;           // 공격 모션 길이 (1형태 / 2형태는 0.8배)
    [Range(0.1f, 0.9f)] public float meleeHitRatio = 0.55f;   // 모션 진행률이 이 값일 때 판정

    [Header("Fire Bomb (화염탄 비) - 원작 : 공격할 때마다 맵 전체에 3발 (2형태 5발)")]
    public float bombMinRange = 0f;
    public float bombMaxRange = 40f;
    public float bombCooldown = 7f;
    public int bombCount = 3;
    public int form2BombCount = 5;
    public float bombInterval = 0.45f;
    public float bombWarnTime = 1.1f;
    public float bombRadius = 1.8f;
    public float bombDamage = 16f;
    public float form2BombDamage = 20f;
    public float bombCastTime = 0.8f;
    public float arenaMinX = -9f;                // 원작 하데스 방 범위
    public float arenaMaxX = 28.4f;

    [Header("Dark Bolts (암흑탄 부채꼴) - 플레이어를 향해 여러 발")]
    public float boltCooldown = 5f;
    public float boltMinRange = 4f;
    public float boltMaxRange = 30f;
    public int boltCount = 3;
    public int form2BoltCount = 5;
    public int form2BoltVolleys = 2;
    public float boltSpreadAngle = 14f;
    public float boltSpeed = 8f;
    public float boltDamage = 14f;
    public float form2BoltDamage = 18f;
    public float boltRadius = 0.45f;
    public float boltCastTime = 0.55f;
    public float boltVolleyInterval = 0.45f;

    [Header("Eruption (지면 폭발 파도) - 보스에서 플레이어 쪽으로 번져가는 기둥 (점프 / 구르기로 회피, 2형태는 양쪽)")]
    public float eruptionCooldown = 7f;
    public float eruptionMinRange = 2f;
    public float eruptionMaxRange = 18f;
    public int eruptionColumns = 7;
    public int form2EruptionColumns = 9;
    public float eruptionSpacing = 2.2f;
    public float eruptionStartOffset = 3f;
    public float eruptionRadius = 1.3f;
    public float eruptionDamage = 20f;
    public float eruptionWarnTime = 0.7f;
    public float eruptionStep = 0.12f;          // 기둥이 하나씩 솟는 간격

    [Header("Shadow Strike (순간이동 강타) - 사라졌다가 플레이어 위치에 나타나 내려찍음")]
    public float teleportCooldown = 9f;
    public float teleportMinRange = 3f;
    public float teleportTrackTime = 1.0f;      // 장판이 플레이어를 따라오는 시간
    public float teleportLockTime = 0.55f;      // 장판 고정 후 나타나기까지
    public float teleportRadius = 3.4f;
    public float teleportDamage = 30f;
    public int form2TeleportChain = 3;          // 2형태는 연속으로 여러 번 (이후 강타는 장판 추적 / 고정 시간이 짧아짐)
    public float teleportChainTrackTime = 0.5f;
    public float teleportChainLockTime = 0.4f;

    [Header("Phase 2 : Combo (돌진 연타) - 파고들며 3연속 타격")]
    public float comboCooldown = 6f;
    public float comboRange = 12f;
    public int comboHits = 3;
    public float comboLungeSpeed = 14f;
    public float comboLungeTime = 0.22f;
    public float comboReach = 5.5f;
    public float comboHitDuration = 0.65f;
    public float comboDamageRatio = 0.8f;       // 공격력 대비 (연타라 한 발은 약함)

    [Header("Phase 2 : Sky Strike (암흑 낙뢰) - 맵 곳곳에 하늘에서 기둥")]
    public float skyCooldown = 12f;
    public int skyStrikes = 12;
    public int skyAimed = 4;                    // 처음 몇 발은 플레이어 위치를 노림
    public float skyInterval = 0.16f;
    public float skyWarnTime = 0.85f;
    public float skyRadius = 0.85f;
    public float skyDamage = 24f;

    [Header("Phase 2 : Beam (암흑 광선) - 바닥 높이 가로 광선을 양쪽으로")]
    public float beamCooldown = 10f;
    public float beamMinRange = 2f;
    public float beamWarnTime = 1.0f;
    public float beamDuration = 1.0f;
    public float beamTickInterval = 0.2f;
    public float beamDamage = 14f;
    public float beamHeight = 1.3f;
    public float beamHeightAboveGround = 0.65f;
    public float beamLength = 40f;

    [Header("Spiral Nova (회전 탄막) - 보스를 중심으로 소용돌이치며 뻗어나가는 탄막, 도중에 회전 방향이 뒤집힘")]
    public float novaCooldown = 11f;
    public float novaCastTime = 0.8f;
    public float novaDuration = 3.2f;
    public float novaInterval = 0.09f;
    public int novaArms = 2;
    public int form2NovaArms = 3;
    public float novaTurnSpeed = 130f;          // 초당 회전 각도 (중간에 반대로)
    public float novaBoltSpeed = 6f;
    public float novaDamage = 11f;
    public float novaBoltRadius = 0.32f;

    [Header("Void Vortex (공허의 소용돌이) - 플레이어를 끌어당긴 뒤 보스 주변을 폭발")]
    public float vortexCooldown = 13f;
    public float vortexChargeTime = 1.0f;
    public float vortexPullTime = 2.4f;
    public float vortexPullSpeed = 3.4f;        // 플레이어 이동 속도(3.5)와 비슷 : 제자리에 서 있으면 끌려옴
    public float form2VortexPullSpeed = 4.0f;
    public float vortexRadius = 5.5f;           // 폭발 범위 (경고 장판)
    public float vortexDamage = 42f;

    [Header("Judgement (심판) - 2형태 전용 : 안전지대 밖은 전부 폭발")]
    public float judgementCooldown = 17f;
    public float judgementWarnTime = 3.0f;
    public int judgementSafeZones = 2;
    public float judgementSafeRadius = 2.0f;
    public float judgementDamage = 55f;

    [Header("Form 2 Hitbox (빌더가 원작 스프라이트 크기 비율로 계산)")]
    public bool hasForm2Collider;
    public Vector2 form2ColliderOffset;
    public Vector2 form2ColliderSize;

    [Header("Meta")]
    [Tooltip("빌더가 수치 일괄 조정을 적용한 버전 (에셋 수치를 직접 고치면 올리지 말 것)")]
    public int tuningVersion;

    [Header("Sprites (빌더가 연결)")]
    public Sprite[] fireballFrames;
    public Sprite[] explosionFrames;
}
