using System;
using UnityEngine;

public enum StatType { Hp, Attack, Stamina }

// 경험치 / 레벨 / 스탯 포인트. 적 처치 이벤트(CombatEvents.OnEnemyKilled)를 구독해 경험치를 얻고,
// 레벨업 시 받은 포인트를 체력 / 공격력 / 스태미너 중 원하는 곳에 투자
[RequireComponent(typeof(PlayerController))]
public class PlayerProgression : MonoBehaviour
{
    private PlayerController player;
    private PlayerStats Stats => player.Stats;

    public int Level { get; private set; } = 1;
    public float Exp { get; private set; }
    public int StatPoints { get; private set; }

    // 다음 레벨까지 필요한 경험치 : base * growth^(Level-1)
    public float ExpToNext => Stats.baseExpToNext * Mathf.Pow(Stats.expGrowth, Level - 1);

    public event Action<float, float> OnExpChanged;   // (현재 경험치, 필요 경험치)
    public event Action<int> OnLevelUp;               // 새 레벨
    public event Action<int> OnStatPointsChanged;     // 남은 포인트

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        CombatEvents.OnEnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        CombatEvents.OnEnemyKilled -= HandleEnemyKilled;
    }

    private void HandleEnemyKilled(BaseCharacter enemy, float expReward)
    {
        if (player.IsDead) return;
        AddExp(expReward);
    }

    public void AddExp(float amount)
    {
        Exp += amount;
        Debug.Log($"경험치 +{amount} ({Exp:F0} / {ExpToNext:F0})");

        // 한 번에 여러 레벨이 오를 수도 있으므로 while
        while (Exp >= ExpToNext)
        {
            Exp -= ExpToNext;
            Level++;
            StatPoints += Stats.pointsPerLevel;
            Debug.Log($"레벨 업! Lv {Level}, 스탯 포인트 {StatPoints}");
            OnLevelUp?.Invoke(Level);
            OnStatPointsChanged?.Invoke(StatPoints);
        }

        OnExpChanged?.Invoke(Exp, ExpToNext);
    }

    // 이전 스테이지에서 이어받은 진행 상황 적용 (PlayerSession)
    public void Restore(int level, float exp, int statPoints)
    {
        Level = Mathf.Max(1, level);
        Exp = exp;
        StatPoints = statPoints;
        OnLevelUp?.Invoke(Level);
        OnExpChanged?.Invoke(Exp, ExpToNext);
        OnStatPointsChanged?.Invoke(StatPoints);
    }

    // 남은 경험치를 한 번에 채워 즉시 레벨업 (레벨업 아이템)
    public void LevelUpInstantly()
    {
        if (player.IsDead) return;
        AddExp(ExpToNext - Exp);
    }

    public bool SpendPoint(StatType type)
    {
        if (StatPoints <= 0 || player.IsDead) return false;

        switch (type)
        {
            case StatType.Hp:      player.AddMaxHp(Stats.hpPerPoint); break;
            case StatType.Attack:  player.AddAttack(Stats.attackPerPoint); break;
            case StatType.Stamina: player.AddMaxStamina(Stats.staminaPerPoint); break;
        }

        StatPoints--;
        Debug.Log($"스탯 투자 : {type} (남은 포인트 {StatPoints}) / 최대 체력 {player.MaxHp}, 공격력 {player.AttackDamage}, 최대 스태미너 {player.MaxStamina}");
        OnStatPointsChanged?.Invoke(StatPoints);
        return true;
    }
}
