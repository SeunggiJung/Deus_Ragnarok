// 하데스 보스 행동 트리 (1형태 / 2형태 공통, 2형태는 BossController의 Phase2 배율로 강화).
//
// Root (Selector : 위에서부터 우선)
// ├─ RandomSelector (모든 공격 뒤에는 숨 고르기)
// │   ├─ (쿨다운) [근접]   → 마법 타격 (앞쪽 범위 피해, 2형태에서는 두 가지 모션 번갈아)
// │   └─ (쿨다운)          → 화염탄 비 (맵 곳곳에 낙하, 2형태는 5발)
// └─ 추적
public static class HadesBossBehaviorTree
{
    public static BTNode Build(HadesBoss boss)
    {
        HadesBossStats s = boss.HadesStats;

        return new Selector(
            new RandomSelector(
                new Cooldown(() => s.meleeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.meleeRange),
                        new HadesMeleeAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.bombCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.bombMinRange && boss.DistanceToTarget <= s.bombMaxRange),
                        new HadesBombAction(boss),
                        Recovery(boss)))),

            new ChaseAction(boss));
    }

    private static BTNode Recovery(HadesBoss boss) =>
        new Wait(() => boss.Stats.attackRecovery * boss.CooldownScale);
}
