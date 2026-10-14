// 하데스 보스 행동 트리 (1형태 / 2형태 공통, 2형태는 BossController의 Phase2 배율로 쿨다운 / 속도 강화 + 패턴 강화).
//
// Root (Selector : 위에서부터 우선)
// ├─ RandomSelector (모든 공격 뒤에는 짧은 숨 고르기)
// │   ├─ (쿨다운) [근접]        → 마법 타격 (앞쪽 범위 피해, 2형태는 모션 번갈아)
// │   ├─ (쿨다운) [중~원거리]   → 암흑탄 부채꼴 (2형태 5발 x 2연속)
// │   ├─ (쿨다운) [중거리]      → 지면 폭발 파도 (플레이어 쪽으로 번지는 기둥, 2형태는 양쪽)
// │   ├─ (쿨다운)               → 화염탄 비 (맵 전체, 2형태는 더 많이)
// │   ├─ (쿨다운) [중거리]      → 돌진 (공통 노드)
// │   ├─ (쿨다운) [근~중거리]   → 순간이동 강타 (무적으로 사라졌다가 플레이어 위치에 출현, 2형태는 3연속)
// │   ├─ (쿨다운) [2형태]       → 돌진 연타 (파고들며 3연속 타격)
// │   ├─ (쿨다운) [2형태]       → 암흑 낙뢰 (맵 곳곳에 하늘에서 기둥)
// │   └─ (쿨다운) [2형태]       → 암흑 광선 (바닥 높이 가로 광선을 양쪽으로)
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

                new Cooldown(() => s.boltCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.boltMinRange && boss.DistanceToTarget <= s.boltMaxRange),
                        new HadesBoltAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.eruptionCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.eruptionMinRange && boss.DistanceToTarget <= s.eruptionMaxRange),
                        new HadesEruptionAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.bombCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.bombMinRange && boss.DistanceToTarget <= s.bombMaxRange),
                        new HadesBombAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.chargeCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.chargeMinRange && boss.DistanceToTarget <= s.chargeMaxRange),
                        new ChargeAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.teleportCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.DistanceToTarget >= s.teleportMinRange),
                        new HadesShadowStrikeAction(boss),
                        Recovery(boss))),

                // ---- 2형태 전용 (되살아난 뒤에만 사용) ----
                new Cooldown(() => s.comboCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.Revived && boss.DistanceToTarget <= s.comboRange),
                        new HadesComboAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.skyCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.Revived),
                        new HadesSkyStrikeAction(boss),
                        Recovery(boss))),

                new Cooldown(() => s.beamCooldown * boss.CooldownScale,
                    new Sequence(
                        new Condition(() => boss.Revived && boss.DistanceToTarget >= s.beamMinRange),
                        new HadesBeamAction(boss),
                        Recovery(boss)))),

            new ChaseAction(boss));
    }

    private static BTNode Recovery(HadesBoss boss) =>
        new Wait(() => boss.Stats.attackRecovery * boss.CooldownScale);
}
