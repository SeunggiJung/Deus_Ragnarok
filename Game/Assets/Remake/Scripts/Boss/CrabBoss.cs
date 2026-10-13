using UnityEngine;

// 2스테이지 Crab 보스. 공통 보스(BossController)에 Crab 전용 행동 트리를 적용
public class CrabBoss : BossController
{
    public CrabBossStats CrabStats => (CrabBossStats)Stats;

    protected override BTNode BuildTree() => CrabBossBehaviorTree.Build(this);
    protected override bool IsValidStats(CharacterStats s) => s is CrabBossStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<CrabBossStats>();
}
