using UnityEngine;

// 스테이지를 이동해도 플레이어 성장이 유지되도록 진행 상황을 보관 (메모리, 게임 실행 중에만 유지).
// - 포탈에서 Save → 새 씬의 플레이어(PlayerSessionSync)가 Start에서 Restore
// - 체크포인트를 밟을 때마다 그 시점의 성장을 스냅샷으로 따로 보관 → 사망 후 재시작하면 체크포인트 이후에 얻은
//   경험치 / 스탯 / 부활 기회는 모두 사라지고 스냅샷 상태로 돌아감 (체크포인트가 없으면 스테이지에 들어올 때의 상태)
public static class PlayerSession
{
    private struct Snapshot
    {
        public int level;
        public float exp;
        public int statPoints;
        public float bonusHp;
        public float bonusAttack;
        public float bonusStamina;
        public int revives;
        public Sprite weaponSprite;
    }

    public static bool HasData { get; private set; }

    private static Snapshot current;      // 스테이지 진입 시점 (포탈에서 저장)

    private static Snapshot checkpoint;   // 마지막으로 밟은 체크포인트 시점
    private static string checkpointScene;
    private static Vector2 checkpointPosition;

    private static Snapshot Capture(PlayerController player)
    {
        var s = new Snapshot();
        var progression = player.GetComponent<PlayerProgression>();
        if (progression != null)
        {
            s.level = progression.Level;
            s.exp = progression.Exp;
            s.statPoints = progression.StatPoints;
        }
        s.bonusHp = player.BonusMaxHp;
        s.bonusAttack = player.BonusAttack;
        s.bonusStamina = player.BonusMaxStamina;
        s.revives = player.ReviveCount;
        SpriteRenderer weapon = PlayerSessionSync.GetWeaponRenderer(player);
        s.weaponSprite = weapon != null ? weapon.sprite : null;
        return s;
    }

    public static void Save(PlayerController player)
    {
        current = Capture(player);
        HasData = true;
    }

    public static void Restore(PlayerController player)
    {
        if (!HasData) return;

        var progression = player.GetComponent<PlayerProgression>();
        if (progression != null)
            progression.Restore(current.level, current.exp, current.statPoints);
        player.AddMaxHp(current.bonusHp);
        player.AddAttack(current.bonusAttack);
        player.AddMaxStamina(current.bonusStamina);
        player.SetReviveCount(current.revives);

        var weapon = PlayerSessionSync.GetWeaponRenderer(player);
        PlayerSessionSync.EquipWeapon(weapon, current.weaponSprite);
    }

    #region Checkpoint
    public static void SetCheckpoint(string scene, Vector2 position, PlayerController player)
    {
        checkpointScene = scene;
        checkpointPosition = position;
        checkpoint = Capture(player);
    }

    public static bool TryGetCheckpoint(string scene, out Vector2 position)
    {
        position = checkpointPosition;
        return checkpointScene == scene;
    }

    // 사망 후 같은 씬을 다시 불러오기 전에 호출 : 체크포인트가 이 씬에 있으면 그 시점의 성장으로 되돌림.
    // 없으면 스테이지에 들어올 때 저장해 둔 상태(current)가 그대로 쓰임
    public static void PrepareRestart(string scene)
    {
        if (checkpointScene != scene) return;
        current = checkpoint;
        HasData = true;
    }

    // 홈으로 나가면 성장 / 체크포인트 모두 초기화
    public static void Reset()
    {
        HasData = false;
        checkpointScene = null;
        current = default;
        checkpoint = default;
    }
    #endregion
}
