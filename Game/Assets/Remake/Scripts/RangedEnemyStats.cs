using UnityEngine;

// 원거리 몬스터 전용 수치 (투사체)
[CreateAssetMenu(fileName = "NewRangedEnemyStats", menuName = "Deus/Ranged Enemy Stats")]
public class RangedEnemyStats : EnemyStats
{
    [Header("Projectile")]
    public float projectileSpeed = 7f;
    public float projectileLifetime = 2.5f;    // 이 시간이 지나면 사라짐 (사거리 = 속도 x 시간)
    public float projectileRadius = 0.25f;
    [Tooltip("비워두면 코드로 만든 빛나는 구체 사용")]
    public Sprite projectileSprite;
    public Color projectileColor = new Color(0.75f, 0.35f, 1f, 1f);
}
