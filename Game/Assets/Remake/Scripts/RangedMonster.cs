using UnityEngine;

// 원거리 몬스터 (마녀 등). AI(Idle/Chase/Attack/Hit/Dead)는 Monster와 동일하고,
// 공격 애니메이션의 Hit 이벤트 시점에 근접 판정 대신 바라보는 방향으로 투사체를 발사
public class RangedMonster : Monster
{
    public RangedEnemyStats RangedStats => (RangedEnemyStats)stats;

    protected override bool IsValidStats(CharacterStats s) => s is RangedEnemyStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<RangedEnemyStats>();

    protected override void PerformAttack()
    {
        // attackCheck를 발사 위치로 사용 (없으면 몸 중심)
        Vector2 firePoint = attackCheck != null ? (Vector2)attackCheck.position : (Vector2)transform.position;
        Vector2 direction = new Vector2(FacingDir, 0f);

        RangedEnemyStats s = RangedStats;
        Projectile projectile = ProjectilePool.Instance.Get();
        projectile.Launch(firePoint, direction, s.projectileSpeed, GetAttackDamage(), s.projectileLifetime,
                          whatIsEnemy, LayerMask.GetMask("Ground"),
                          s.projectileSprite, s.projectileColor, s.projectileRadius);
    }
}
