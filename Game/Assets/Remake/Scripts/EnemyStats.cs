using UnityEngine;

// 일반 몬스터 전용 수치 (AI 감지/추적/공격)
[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Deus/Enemy Stats")]
public class EnemyStats : CharacterStats
{
    [Header("AI")]
    public float moveSpeed = 1.5f;
    public float detectRange = 6f;      // 이 거리 안에 플레이어가 들어오면 추적 시작
    public float loseRange = 9f;        // 이 거리 밖으로 벗어나면 추적 중단
    public float attackRange = 2f;      // 이 거리 안이면 공격 시도 (수평 거리 기준)
    public float attackCooldown = 1.5f;

    [Header("Reward")]
    public float expReward = 50f;       // 처치 시 플레이어가 얻는 경험치

    [Header("Hit / Death")]
    public float hitStunTime = 0.2f;    // 공격 중이 아닐 때 피격 경직 시간 (공격 중에는 경직 없음)
    public float deathDelay = 1.5f;     // 사망 애니메이션 후 오브젝트 제거까지 시간
}
