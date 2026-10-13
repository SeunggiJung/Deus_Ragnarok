using System;

// 전투 관련 전역 이벤트. 적(몬스터, 보스)은 죽었다는 사실만 알리고, 보상 처리는 구독하는 쪽(PlayerProgression)이 담당
public static class CombatEvents
{
    // (죽은 적, 경험치)
    public static event Action<BaseCharacter, float> OnEnemyKilled;

    public static void RaiseEnemyKilled(BaseCharacter enemy, float expReward)
    {
        OnEnemyKilled?.Invoke(enemy, expReward);
    }
}
