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

    [Header("Sprites (빌더가 연결)")]
    public Sprite[] fireballFrames;
    public Sprite[] explosionFrames;
}
