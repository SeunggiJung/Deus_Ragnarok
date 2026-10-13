using UnityEngine;

// 2스테이지 드래곤 보스. 공통 보스(BossController)에 드래곤 전용 행동 트리를 적용
public class DragonBoss : BossController
{
    public static class DragonAnim
    {
        public const string Lift = "lift";             // 붙잡고 들어올리는 자세
        public const string Staggered = "staggered";   // 그로기 동안 어지러운 모션 유지
    }

    [Tooltip("등장 연출(카메라 이동 + Spawn 모션)이 끝날 때까지 전투를 시작하지 않는 시간")]
    [SerializeField] private float spawnDelay = 3f;

    private float enabledTime;

    public DragonBossStats DragonStats => (DragonBossStats)Stats;

    protected override float CounterStaggerTime => DragonStats.counterStaggerTime;
    protected override bool CanEngage => Time.time - enabledTime >= spawnDelay;

    protected override BTNode BuildTree() => DragonBossBehaviorTree.Build(this);
    protected override bool IsValidStats(CharacterStats s) => s is DragonBossStats;
    protected override CharacterStats CreateDefaultStats() => ScriptableObject.CreateInstance<DragonBossStats>();

    private void OnEnable() => enabledTime = Time.time;

    // 그로기 동안은 어지러운 모션(Dizzy)을 유지
    private void LateUpdate()
    {
        if (Anim != null) Anim.SetBool(DragonAnim.Staggered, IsStaggered && !IsDead);
    }
}
