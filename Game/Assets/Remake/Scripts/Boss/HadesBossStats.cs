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
