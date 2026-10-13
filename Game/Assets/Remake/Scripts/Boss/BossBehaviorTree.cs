using UnityEngine;

// 보스 행동 트리 구성.
//
// Root (Selector : 위에서부터 우선)
// ├─ [체력 50% 이하 & 1페이즈]        → 포효 (무적, 2페이즈 진입)
// ├─ [짧은 시간에 여러 번 맞음]        → (쿨다운) 방어 자세 → 가까우면 반격
// ├─ RandomSelector (매번 순서를 섞어 패턴 선택)
// │   (모든 공격 뒤에는 attackRecovery만큼 숨 고르기)
// │   ├─ (쿨다운) [근접 거리]          → 연타 (Attack_2)
// │   ├─ (쿨다운) [가까움]             → 지면 가시 (Attack_1, 주변 광역)
// │   ├─ (쿨다운) [중거리]             → 예고 후 돌진
// │   └─ (쿨다운) [원거리]             → 에너지탄 (Attack_3, 2페이즈 3갈래)
// └─ 추적
public static class BossBehaviorTree
{
    public static BTNode Build(BossController boss)
    {
        BossStats s = boss.Stats;

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
                            new MeleeComboAction(boss)),   // 반격
                        new ActionNode(() => NodeState.Success)))),

            new RandomSelector(
                new Cooldown(() => s.meleeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.meleeRange),
                        new MeleeComboAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.slamCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget <= s.slamRange),
                        new SlamAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.chargeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.chargeMinRange && boss.DistanceToTarget <= s.chargeMaxRange),
                        new ChargeAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.throwCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.throwMinRange && boss.DistanceToTarget <= s.throwMaxRange),
                        new ThrowAction(boss),
                        Recovery(boss)))),

            new ChaseAction(boss));
    }

    // 공격 후 숨 고르기 : 이 동안 플레이어가 반격할 수 있음 (2페이즈에서는 짧아짐)
    private static BTNode Recovery(BossController boss) =>
        new Wait(() => boss.Stats.attackRecovery * boss.CooldownScale);
}
