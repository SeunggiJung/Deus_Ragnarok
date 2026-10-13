// 드래곤 보스 행동 트리.
//
// Root (Selector : 위에서부터 우선)
// ├─ [체력 50% 이하 & 1페이즈]        → 포효 (무적, 2페이즈 진입)
// ├─ [짧은 시간에 여러 번 맞음]        → (쿨다운) 방어 자세
// ├─ RandomSelector (모든 공격 뒤에는 숨 고르기)
// │   ├─ (쿨다운) [근접]               → 발톱 (Hit 이벤트)
// │   ├─ (쿨다운) [중거리]             → 화염 브레스 (후반 40%에 점프 공격 → 긴 그로기, 2페이즈는 2연속)
// │   ├─ (쿨다운)                      → 화염구 비 (플레이어 주변에 하나씩 떨어짐)
// │   └─ (쿨다운) [가까움]             → 붙잡아 내려찍기 (제한 시간 안에 범위 밖으로 못 나가면 잡혀서 내려찍힘)
// └─ 추적
public static class DragonBossBehaviorTree
{
    public static BTNode Build(DragonBoss boss)
    {
        DragonBossStats s = boss.DragonStats;

        return new Selector(
            new Sequence(
                new Condition(() => boss.NeedsPhase2),
                new RoarAction(boss)),

            new Cooldown(() => s.guardCooldown * boss.CooldownScale,
                new Sequence(
                    new Condition(boss.IsPressured),
                    new GuardAction(boss))),

            new RandomSelector(
                new Cooldown(() => s.meleeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.meleeRange),
                        new MeleeComboAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.breathCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.breathMinRange && boss.DistanceToTarget <= s.breathMaxRange),
                        new BreathAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.rainCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.rainMinRange && boss.DistanceToTarget <= s.rainMaxRange),
                        new FireRainAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.seizeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.seizeRange),
                        new SeizeSlamAction(boss),
                        Recovery(boss)))),

            new ChaseAction(boss));
    }

    private static BTNode Recovery(DragonBoss boss) =>
        new Wait(() => boss.Stats.attackRecovery * boss.CooldownScale);
}
