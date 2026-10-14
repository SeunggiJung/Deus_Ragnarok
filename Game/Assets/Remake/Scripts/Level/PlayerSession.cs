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

    // 마지막으로 밟은 체크포인트 (사망 후 같은 씬을 다시 불러와도 그 위치에서 시작하기 위해 보관)
    private static string checkpointScene;
    private static Vector2 checkpointPosition;

    public static void SetCheckpoint(string scene, Vector2 position)
    {
        checkpointScene = scene;
        checkpointPosition = position;
    }

    public static bool TryGetCheckpoint(string scene, out Vector2 position)
    {
        position = checkpointPosition;
        return checkpointScene == scene;
    }

    // 홈으로 나가면 성장 / 체크포인트 모두 초기화
    public static void Reset()
    {
        HasData = false;
        checkpointScene = null;
        level = 0; exp = 0f; statPoints = 0;
        bonusHp = bonusAttack = bonusStamina = 0f;
        weaponSprite = null;
    }

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
