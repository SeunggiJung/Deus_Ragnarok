using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 2스테이지 리메이크 변환.
// 원작 second_stage를 Assets/Remake/Scenes/Stage2.unity로 복사(원작 씬은 그대로)한 뒤 리메이크 시스템으로 교체
// - 플레이어 / HUD : Player.prefab, PlayerHUD.prefab (1스테이지와 같은 조작 / UI), 카메라 대상 연결, 레거시 Canvas 비활성화
// - 내려찍는 장애물(Rockhead x7) : RockHeadController → Crusher (시작 시간을 엇갈리게 해 리듬 패턴)
// - 보스(Crab) : CrabBossBuilder (Behavior Tree 보스) / 드래곤 : 레거시 Enemy / EnemyAI → Monster FSM + Stats 에셋 + 머리 위 체력바
// - 보스방 입구 기둥(Trap) / 출구 문(Door) : Crab과 연동되는 BossArenaGate
// - 검(key) : WeaponPickup, 색 조합 퍼즐(불 3 + 돌 3 → Door2) : ColorPuzzle / ColorFire / ColorStone
// - 드래곤 등장(key2) : EncounterTrigger, 포탈 : 드래곤 처치 후 third_stage
// - 검 가는 길 : 텔레포트용 지형 → 점프 + 공중 구르기 계단 발판 (Stage2SwordClimb)
// - 체크포인트, 사냥용 스포너
// 메뉴 : Tools/Deus/Build Stage 2  (어느 씬에서 실행해도 Stage2 씬을 열고 구성, 여러 번 실행해도 결과 동일)
public static class Stage2Builder
{
    private const string LegacyScenePath = "Assets/Legacy/Scenes/second_stage.unity";
    private const string ScenePath = "Assets/Remake/Scenes/Stage2.unity";
    private const string DataFolder = "Assets/Remake/Data";
    private const float StartX = -3f;

    [MenuItem("Tools/Deus/Build Stage 2")]
    public static void Build()
    {
        if (!OpenStageScene()) return;

        Tilemap ground = FindInScene("Tilemap")?.GetComponent<Tilemap>();

        GameObject player = SetupPlayer(ground);
        SetupCamera(player);
        SetupHud();
        SetupCrushers();

        // Crab : 레거시 Boss → Behavior Tree 보스(CrabBoss)
        GameObject crabGo = FindInScene("Boss") ?? FindInScene("Crab");
        if (crabGo != null)
        {
            crabGo.name = "Crab";
            RemoveLegacyScripts(crabGo, "Enemy", "EnemyAI");
        }
        CrabBoss crab = CrabBossBuilder.Convert();
        Monster dragon = ConvertMonster("Dragon", "Dragon", "Dragon_attack", LoadOrCreateDragonStats());

        SetupArenaGates(crab);
        SetupSword();
        SetupPuzzle();
        SetupDragonEncounter(dragon);
        SetupPortal(dragon);
        SetupCheckpoints(ground);
        SetupSpawners(ground);
        Stage2SwordClimb.Build();   // 검 가는 길 (체크포인트 / 스포너 위치를 덮어씀)

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AddToBuildSettings();
        Debug.Log("2스테이지 리메이크 구성 완료 : " + ScenePath);
    }

    #region Scene
    private static bool OpenStageScene()
    {
        if (SceneManager.GetActiveScene().path == ScenePath) return true;

        EditorSceneManager.SaveOpenScenes();
        if (!File.Exists(ScenePath))
        {
            if (!AssetDatabase.CopyAsset(LegacyScenePath, ScenePath))
            {
                Debug.LogError($"{LegacyScenePath} 복사 실패");
                return false;
            }
            AssetDatabase.Refresh();
        }
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        return true;
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == ScenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // 비활성 오브젝트까지 포함해 이름으로 찾기
    private static GameObject FindInScene(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
        }
        return null;
    }

    private static IEnumerable<GameObject> FindAllInScene(System.Func<string, bool> match)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (match(t.name)) yield return t.gameObject;
        }
    }

    // x 위치의 지면 윗면 높이 (해당 열의 가장 높은 타일 위)
    private static float GroundY(Tilemap ground, float worldX, float fallback = 0f)
    {
        if (ground == null) return fallback;
        Vector3Int cell = ground.WorldToCell(new Vector3(worldX, 0f, 0f));
        BoundsInt b = ground.cellBounds;
        for (int y = b.yMax; y >= b.yMin; y--)
        {
            if (ground.HasTile(new Vector3Int(cell.x, y, 0)))
                return ground.CellToWorld(new Vector3Int(cell.x, y + 1, 0)).y;
        }
        return fallback;
    }

    private static void RemoveLegacyScripts(GameObject go, params string[] typeNames)
    {
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (mb != null && typeNames.Contains(mb.GetType().Name))
                Object.DestroyImmediate(mb);
        }
    }
    #endregion

    #region Player / Camera / HUD
    private static GameObject SetupPlayer(Tilemap ground)
    {
        GameObject existing = GameObject.Find("sword_man");
        if (existing != null && existing.GetComponent<PlayerController>() != null)
            return existing; // 이미 리메이크 플레이어

        if (existing != null)
            Object.DestroyImmediate(existing); // 레거시 Sword_Man 플레이어 (복사본 씬에서만 제거)

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogError("Player.prefab이 없습니다. RemakeTest에서 Tools/Deus/Build Player Prefabs를 먼저 실행하세요.");
            return null;
        }
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        player.name = "sword_man";
        player.transform.position = new Vector3(StartX, GroundY(ground, StartX) + 1f, 0f);
        return player;
    }

    private static void SetupCamera(GameObject player)
    {
        if (player == null) return;
        GameObject cam = GameObject.Find("Main Camera");
        if (cam == null) return;
        foreach (var mb in cam.GetComponents<MonoBehaviour>())
        {
            var so = new SerializedObject(mb);
            var target = so.FindProperty("target");
            if (target == null) continue;
            target.objectReferenceValue = player.transform;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetupHud()
    {
        GameObject legacyCanvas = FindInScene("Canvas");
        if (legacyCanvas != null) legacyCanvas.SetActive(false); // 레거시 체력/마나 바 등

        if (GameObject.Find("PlayerHUD") != null) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.HudPrefabPath);
        if (prefab == null) return;
        var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        hud.name = "PlayerHUD";
    }
    #endregion

    #region Crushers
    private static void SetupCrushers()
    {
        var crushers = FindAllInScene(n => n.StartsWith("Rockhead"))
            .Where(go => go.GetComponent<Rigidbody2D>() != null)
            .OrderBy(go => go.transform.position.x)
            .ToList();

        for (int i = 0; i < crushers.Count; i++)
        {
            GameObject go = crushers[i];
            RemoveLegacyScripts(go, "RockHeadController");
            var crusher = GetOrAdd<Crusher>(go);
            var so = new SerializedObject(crusher);
            // 왼쪽부터 0.5초씩 늦게 시작 → 파도처럼 차례로 떨어지는 리듬
            so.FindProperty("startDelay").floatValue = (i % 4) * 0.5f;
            so.ApplyModifiedProperties();
        }
    }
    #endregion

    #region Enemies
    private static EnemyStats LoadOrCreateCrabStats()
    {
        return LoadOrCreate<EnemyStats>("CrabStats", s =>
        {
            // 원작 Boss(2스테이지) : 체력 600, 데미지 10, 쿨다운 3, 이동 3
            s.maxHp = 300f; s.invincibleTime = 0.2f; s.attackDamage = 14f; s.attackRadius = 2f;
            s.moveSpeed = 2f; s.detectRange = 10f; s.loseRange = 16f; s.attackRange = 3.5f;
            s.attackCooldown = 1.8f; s.expReward = 150f; s.hitStunTime = 0.15f; s.deathDelay = 1.5f;
        });
    }

    private static EnemyStats LoadOrCreateDragonStats()
    {
        return LoadOrCreate<EnemyStats>("DragonStats", s =>
        {
            // 원작 Dragon : 체력 800, 데미지 30, 쿨다운 1.5, 이동 3
            s.maxHp = 450f; s.invincibleTime = 0.2f; s.attackDamage = 20f; s.attackRadius = 1.5f;
            s.moveSpeed = 2.2f; s.detectRange = 12f; s.loseRange = 18f; s.attackRange = 2.2f;
            s.attackCooldown = 2f; s.expReward = 250f; s.hitStunTime = 0.15f; s.deathDelay = 2f;
        });
    }

    private static T LoadOrCreate<T>(string name, System.Action<T> init) where T : ScriptableObject
    {
        string path = $"{DataFolder}/{name}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        init(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    // 레거시 몬스터를 제자리에서 Monster FSM으로 변환 (같은 attack / die / moving 애니메이터 사용)
    private static Monster ConvertMonster(string legacyName, string newName, string attackState, EnemyStats stats)
    {
        GameObject go = FindInScene(legacyName) ?? FindInScene(newName);
        if (go == null) return null;

        go.name = newName;
        RemoveLegacyScripts(go, "Enemy", "EnemyAI");

        var monster = GetOrAdd<Monster>(go);
        var so = new SerializedObject(monster);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("whatIsEnemy").intValue = LayerMask.GetMask("Player");
        so.FindProperty("attackStateName").stringValue = attackState;
        so.ApplyModifiedProperties();

        GetOrAdd<WorldHealthBar>(go);
        go.layer = LayerMask.NameToLayer("Enemy");
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        if (go.transform.Find("AttackCheck") == null)
        {
            var col = go.GetComponent<CapsuleCollider2D>();
            var child = new GameObject("AttackCheck").transform;
            child.SetParent(go.transform, false);
            child.localPosition = col != null ? new Vector2(col.offset.x + col.size.x * 0.5f, col.offset.y) : Vector2.zero;
            child.gameObject.layer = go.layer;
        }
        return monster;
    }
    #endregion

    #region Arena / Pickups / Puzzle / Portal
    public static void SetupArenaGates(CrabBoss crab)
    {
        if (crab == null) return;
        SetupGate(FindInScene("Trap") ?? FindInScene("CrabGate"), "CrabGate", crab, startsOpen: true, closeOnEngage: true);
        LockDodgeBeforeBoss();
        SetupGate(FindInScene("Door") ?? FindInScene("CrabExitDoor"), "CrabExitDoor", crab, startsOpen: false, closeOnEngage: false);
    }

    // 보스 구역(입구 기둥) 이전에서는 구르기 금지
    [MenuItem("Tools/Deus/Stage 2 - Lock Dodge Before Boss")]
    public static void LockDodgeBeforeBoss()
    {
        GameObject gate = FindInScene("CrabGate") ?? FindInScene("Trap");
        if (gate == null) { Debug.LogError("보스방 입구 기둥(CrabGate)을 찾을 수 없습니다."); return; }
        GetOrAdd<DodgeLockZone>(gate);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        var col = gate.GetComponent<Collider2D>();
        Debug.Log($"구르기 잠금 구역 설정 : x < {(col != null ? col.bounds.center.x : gate.transform.position.x)}");
    }

    private static void SetupGate(GameObject go, string name, BossController boss, bool startsOpen, bool closeOnEngage)
    {
        if (go == null) return;
        go.name = name;
        go.layer = LayerMask.NameToLayer("Ground");
        var gate = GetOrAdd<BossArenaGate>(go);
        var so = new SerializedObject(gate);
        so.FindProperty("boss").objectReferenceValue = boss;
        so.FindProperty("guardian").objectReferenceValue = null;
        so.FindProperty("startsOpen").boolValue = startsOpen;
        so.FindProperty("closeOnEngage").boolValue = closeOnEngage;
        so.FindProperty("openOnDefeat").boolValue = true;
        so.ApplyModifiedProperties();
    }

    private static void SetupSword()
    {
        GameObject sword = FindInScene("key") ?? FindInScene("SwordPickup");
        if (sword == null) return;
        sword.name = "SwordPickup";
        GetOrAdd<WeaponPickup>(sword);
    }

    private static void SetupPuzzle()
    {
        GameObject puzzleGo = FindInScene("ColorPuzzle") ?? new GameObject("ColorPuzzle");
        var puzzle = GetOrAdd<ColorPuzzle>(puzzleGo);

        SetupFire("fire_red", PuzzleColor.Red, puzzle);
        SetupFire("fire_blue", PuzzleColor.Blue, puzzle);
        SetupFire("fire_green", PuzzleColor.Green, puzzle);

        // 원작 조합표 : Stone1 = 흰색, Stone2 = 자홍, Stone3 = 주황
        var stones = new List<ColorStone>
        {
            SetupStone("Stone1", PuzzleColor.White, "Light1", puzzle),
            SetupStone("Stone2", PuzzleColor.Magenta, "Light2", puzzle),
            SetupStone("Stone3", PuzzleColor.Orange, "Light3", puzzle),
        };

        var so = new SerializedObject(puzzle);
        SerializedProperty array = so.FindProperty("stones");
        var valid = stones.Where(s => s != null).ToList();
        array.arraySize = valid.Count;
        for (int i = 0; i < valid.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = valid[i];
        GameObject door2 = FindInScene("Door2");
        so.FindProperty("door").objectReferenceValue = door2 != null ? door2.transform : null;
        so.ApplyModifiedProperties();
    }

    private static void SetupFire(string name, PuzzleColor color, ColorPuzzle puzzle)
    {
        GameObject go = FindInScene(name);
        if (go == null) return;
        var fire = GetOrAdd<ColorFire>(go);
        var so = new SerializedObject(fire);
        so.FindProperty("color").enumValueIndex = (int)color;
        so.FindProperty("puzzle").objectReferenceValue = puzzle;
        so.ApplyModifiedProperties();
    }

    private static ColorStone SetupStone(string name, PuzzleColor required, string lightName, ColorPuzzle puzzle)
    {
        GameObject go = FindInScene(name);
        if (go == null) return null;
        var stone = GetOrAdd<ColorStone>(go);
        var so = new SerializedObject(stone);
        so.FindProperty("required").enumValueIndex = (int)required;
        so.FindProperty("puzzle").objectReferenceValue = puzzle;
        so.FindProperty("lightObject").objectReferenceValue = FindInScene(lightName);
        Transform glow = go.transform.Find("Glow");
        so.FindProperty("glow").objectReferenceValue = glow != null ? glow.gameObject : null;
        so.ApplyModifiedProperties();
        return stone;
    }

    private static void SetupDragonEncounter(Monster dragon)
    {
        GameObject trigger = FindInScene("key2") ?? FindInScene("DragonTrigger");
        if (trigger == null || dragon == null) return;
        trigger.name = "DragonTrigger";
        var encounter = GetOrAdd<EncounterTrigger>(trigger);
        var so = new SerializedObject(encounter);
        SerializedProperty array = so.FindProperty("activateOnTrigger");
        array.arraySize = 1;
        array.GetArrayElementAtIndex(0).objectReferenceValue = dragon.gameObject;
        so.FindProperty("focusPoint").objectReferenceValue = dragon.transform;
        so.ApplyModifiedProperties();
    }

    private static void SetupPortal(Monster dragon)
    {
        GameObject portal = FindInScene("Portal");
        if (portal == null) return;
        var stagePortal = GetOrAdd<StagePortal>(portal);
        var so = new SerializedObject(stagePortal);
        so.FindProperty("nextSceneName").stringValue = "third_stage";
        so.FindProperty("requiredDefeated").objectReferenceValue = dragon;
        so.FindProperty("lockedPrompt").stringValue = "Defeat the Dragon";
        so.ApplyModifiedProperties();
    }
    #endregion

    #region Checkpoints / Spawners
    private static void SetupCheckpoints(Tilemap ground)
    {
        // 장애물 구간 시작 / 보스방 / 검 이후 / 퍼즐 / 드래곤 앞
        float[] xs = { -2f, 56f, 110f, 142f, 185f };
        GameObject parent = FindInScene("Checkpoints") ?? new GameObject("Checkpoints");
        for (int i = 0; i < xs.Length; i++)
        {
            string name = $"Checkpoint_{i + 1}";
            Transform t = parent.transform.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = new Vector3(xs[i], GroundY(ground, xs[i]) + 1.5f, 0f);
            var box = GetOrAdd<BoxCollider2D>(go);
            box.isTrigger = true;
            box.size = new Vector2(1f, 3f);
            GetOrAdd<Checkpoint>(go);
        }
    }

    private static void SetupSpawners(Tilemap ground)
    {
        // 검 이후 퍼즐 앞까지만 사냥 구간. 퍼즐 ~ 드래곤 보스방 사이에는 몬스터를 두지 않음
        CreateSpawner(ground, "Spawner_S2_1", "Assets/Remake/Prefabs/Golem.prefab", 95f, 2f);
        foreach (string removed in new[] { "Spawner_S2_2", "Spawner_S2_3" })
        {
            GameObject old = FindInScene(removed);
            if (old != null) Object.DestroyImmediate(old);
        }
    }

    private static void CreateSpawner(Tilemap ground, string name, string prefabPath, float x, float heightAboveGround)
    {
        GameObject parent = FindInScene("Spawners") ?? new GameObject("Spawners");
        Transform t = parent.transform.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, GroundY(ground, x) + heightAboveGround, 0f);

        var spawner = GetOrAdd<MonsterSpawner>(go);
        var so = new SerializedObject(spawner);
        so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Monster>(prefabPath);
        so.FindProperty("maxAlive").intValue = 1;
        so.FindProperty("respawnDelay").floatValue = 10f;
        so.ApplyModifiedProperties();
    }
    #endregion

    // Unity 오브젝트는 없을 때 가짜 null을 반환할 수 있어 ?? 연산자 대신 명시적으로 검사
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
