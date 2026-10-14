using UnityEngine;

// 스테이지를 이동해도 플레이어 성장이 유지되도록 진행 상황을 보관 (메모리, 게임 실행 중에만 유지).
// 포탈에서 Save → 새 씬의 플레이어(PlayerSessionSync)가 Start에서 Restore
public static class PlayerSession
{
    public static bool HasData { get; private set; }

    private static int level;
    private static float exp;
    private static int statPoints;
    private static float bonusHp;
    private static float bonusAttack;
    private static float bonusStamina;
    private static Sprite weaponSprite;

    public static void Save(PlayerController player)
    {
        var progression = player.GetComponent<PlayerProgression>();
        if (progression != null)
        {
            level = progression.Level;
            exp = progression.Exp;
            statPoints = progression.StatPoints;
        }
        bonusHp = player.BonusMaxHp;
        bonusAttack = player.BonusAttack;
        bonusStamina = player.BonusMaxStamina;
        SpriteRenderer weapon = PlayerSessionSync.GetWeaponRenderer(player);
        weaponSprite = weapon != null ? weapon.sprite : null;
        HasData = true;
    }

    public static void Restore(PlayerController player)
    {
        if (!HasData) return;

        var progression = player.GetComponent<PlayerProgression>();
        if (progression != null)
            progression.Restore(level, exp, statPoints);
        player.AddMaxHp(bonusHp);
        player.AddAttack(bonusAttack);
        player.AddMaxStamina(bonusStamina);

        var weapon = PlayerSessionSync.GetWeaponRenderer(player);
        PlayerSessionSync.EquipWeapon(weapon, weaponSprite);
    }
}
