// Crab 보스 행동 트리.
//
// Root (Selector : 위에서부터 우선)
// ├─ [체력 50% 이하 & 1페이즈]        → 포효 (무적, 2페이즈 진입)
// ├─ [짧은 시간에 여러 번 맞음]        → (쿨다운) 방어 자세 → 가까우면 반격
// ├─ RandomSelector (모든 공격 뒤에는 숨 고르기)
// │   ├─ (쿨다운) [근접]               → 집게 연타
// │   ├─ (쿨다운) [가까움]             → 바닥 찍기 (발밑 + 좌우 충격파, 점프 / 구르기로 회피)
// │   ├─ (쿨다운) [가까움]             → 잡기 (예고 → 돌진 → 잡히면 제압 + 큰 피해, 구르기로만 회피)
// │   ├─ (쿨다운) [중거리]             → 도약 내려찍기 (기 모음 → 높이 도약 → 바닥 장판 → 장판 위로 내려찍음)
// │   └─ (쿨다운) [원거리]             → 돌진
// └─ 추적
public static class CrabBossBehaviorTree
{
    public static BTNode Build(CrabBoss boss)
    {
        CrabBossStats s = boss.CrabStats;

        return new Selector(
            new Sequence(
                new Condition(() => boss.NeedsPhase2),
                new RoarAction(boss)),

            new Cooldown(() => s.guardCooldown * boss.CooldownScale,
                new Sequence(
                    new Condition(boss.IsPressured),
                    new GuardAction(boss),
                    new Selector(
                        new Sequence(
                            new Condition(() => boss.DistanceToTarget <= s.meleeRange),
                            new MeleeComboAction(boss)),
                        new ActionNode(() => NodeState.Success)))),

            new RandomSelector(
                new Cooldown(() => s.meleeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.meleeRange),
                        new MeleeComboAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.poundCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.poundRange),
                        new GroundPoundAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.grabCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.grabRange),
                        new GrabAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.leapCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.leapMinRange && boss.DistanceToTarget <= s.leapMaxRange),
                        new LeapSlamAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.chargeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.chargeMinRange && boss.DistanceToTarget <= s.chargeMaxRange),
                        new ChargeAction(boss),
                        Recovery(boss)))),

            new ChaseAction(boss));
    }

    private static BTNode Recovery(CrabBoss boss) =>
        new Wait(() => boss.Stats.attackRecovery * boss.CooldownScale);
}
