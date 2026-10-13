using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// 보스 처치 이후 진행 구간 구성.
// - 오른쪽 문(Door) : 처음엔 닫혀 있다가 보스 처치 후 열림 (BossArenaGate, startsOpen = false)
// - 검(key) : 닿으면 공격력 +3, 무기 스프라이트 교체 (WeaponPickup)
// - 낭떠러지(x 79 ~ 103) : 원작은 순간이동으로 건너뛰던 구간 → 떨어지는 / 움직이는 발판 6개로 점프 구간 구성
// - 원작 점프맵 발판 : Trap2 / Trap4 → 떨어지는 발판, Trap3 → 위아래 왕복 (레거시 trap.cs 비활성화)
// - 체크포인트 3곳 + 플레이어 낙사 복귀(PlayerRespawn), 플레이어의 레거시 Sword_Man 제거
// - 보상 상자 : 공격으로 부수면 LV+1 아이템 드롭 (BreakableBox)
// - 맨 오른쪽 포탈 : G키로 다음 스테이지 (StagePortal + 플레이어 PlayerInteractor)
// 메뉴 : Tools/Deus/Build Jump Section  (RemakeTest 씬에서 실행, 여러 번 실행해도 결과 동일)
public static class JumpSectionBuilder
{
    private const string PlatformPrefix = "JumpPlatform_";

    private enum Kind { Falling, Moving }

    // (왼쪽 타일 x, 윗면 높이, 종류) : 2칸 너비, 칸 사이 간격 2 (점프 1.4칸 / 이동 3.5 기준 최대 약 2.9칸)
    private static readonly (int x, int top, Kind kind)[] PitPlatforms =
    {
        (81, 3, Kind.Falling),
        (85, 3, Kind.Moving),
        (89, 4, Kind.Falling),
        (93, 3, Kind.Moving),
        (97, 4, Kind.Falling),
        (101, 3, Kind.Falling),
    };

    [MenuItem("Tools/Deus/Build Jump Section")]
    public static void Build()
    {
        GameObject gridObject = GameObject.Find("Grid");
        Transform grid = gridObject != null ? gridObject.transform : null;
        if (grid == null)
        {
            Debug.LogError("Grid를 찾을 수 없습니다.");
            return;
        }

        SetupExitDoor(grid);
        SetupSword();
        BuildPitPlatforms(grid);
        SetupLegacyPlatforms(grid);
        SetupCheckpoints();
        SetupPlayer();
        ReplaceLegacyEnemies();
        SetupRewardBox();
        SetupStagePortal();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("점프 구간 구성 완료");
    }

    private static void SetupExitDoor(Transform grid)
    {
        Transform door = grid.Find("Door");
        if (door == null) door = grid.Find("BossExitDoor");
        if (door == null) return;

        door.name = "BossExitDoor";
        door.gameObject.layer = LayerMask.NameToLayer("Ground"); // 구르기 벽 판정에도 걸리도록
        var gate = GetOrAdd<BossArenaGate>(door.gameObject);
        var so = new SerializedObject(gate);
        so.FindProperty("startsOpen").boolValue = false;
        so.FindProperty("closeOnEngage").boolValue = false;
        so.FindProperty("openOnDefeat").boolValue = true;
        so.FindProperty("openDelay").floatValue = 1.5f;
        so.ApplyModifiedProperties();
    }

    private static void SetupSword()
    {
        GameObject sword = GameObject.Find("key");
        if (sword == null) sword = GameObject.Find("SwordPickup");
        if (sword == null) return;

        sword.name = "SwordPickup";
        var pickup = GetOrAdd<WeaponPickup>(sword);
        var so = new SerializedObject(pickup);
        so.FindProperty("attackBonus").floatValue = 3f;
        so.ApplyModifiedProperties();
    }

    private static void BuildPitPlatforms(Transform grid)
    {
        // 원작 Trap4 발판의 윗줄 타일을 모양 그대로 사용
        Transform trap4 = grid.Find("Trap4");
        Tilemap template = trap4 != null ? trap4.GetComponent<Tilemap>() : null;
        if (template == null)
        {
            Debug.LogError("발판 모양으로 쓸 Trap4를 찾을 수 없습니다.");
            return;
        }
        TileBase leftTile = template.GetTile(new Vector3Int(112, 3, 0));
        TileBase rightTile = template.GetTile(new Vector3Int(113, 3, 0));
        var templateRenderer = template.GetComponent<TilemapRenderer>();

        // 다시 실행할 때를 위해 이전에 만든 발판 제거
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Transform child = grid.GetChild(i);
            if (child.name.StartsWith(PlatformPrefix))
                Object.DestroyImmediate(child.gameObject);
        }

        for (int i = 0; i < PitPlatforms.Length; i++)
        {
            var (x, top, kind) = PitPlatforms[i];

            var go = new GameObject($"{PlatformPrefix}{i + 1}_{kind}");
            go.transform.SetParent(grid, false);
            go.layer = LayerMask.NameToLayer("Ground");

            var tilemap = go.AddComponent<Tilemap>();
            var renderer = go.AddComponent<TilemapRenderer>();
            renderer.sortingLayerID = templateRenderer.sortingLayerID;
            renderer.sortingOrder = templateRenderer.sortingOrder;
            tilemap.SetTile(new Vector3Int(x, top - 1, 0), leftTile);
            tilemap.SetTile(new Vector3Int(x + 1, top - 1, 0), rightTile);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            go.AddComponent<TilemapCollider2D>();

            if (kind == Kind.Falling)
            {
                go.AddComponent<FallingPlatform>();
            }
            else
            {
                var moving = go.AddComponent<MovingPlatform>();
                var so = new SerializedObject(moving);
                so.FindProperty("offsetA").vector2Value = new Vector2(0f, -1f);
                so.FindProperty("offsetB").vector2Value = new Vector2(0f, 2f);
                so.FindProperty("speed").floatValue = 1.5f;
                so.ApplyModifiedProperties();
            }
        }
    }

    private static void SetupLegacyPlatforms(Transform grid)
    {
        foreach (string name in new[] { "Trap2", "Trap4" })
        {
            Transform t = grid.Find(name);
            if (t != null) GetOrAdd<FallingPlatform>(t.gameObject);
        }

        Transform trap3 = grid.Find("Trap3");
        if (trap3 != null)
        {
            // 레거시 trap.cs(속도 4로 y -2 ~ 8 왕복)를 끄고 같은 범위를 MovingPlatform으로
            foreach (var mb in trap3.GetComponents<MonoBehaviour>())
            {
                if (mb.GetType().Name == "trap") mb.enabled = false;
            }
            var moving = GetOrAdd<MovingPlatform>(trap3.gameObject);
            float startY = trap3.position.y;
            var so = new SerializedObject(moving);
            so.FindProperty("offsetA").vector2Value = new Vector2(0f, -2f - startY);
            so.FindProperty("offsetB").vector2Value = new Vector2(0f, 8f - startY);
            so.FindProperty("speed").floatValue = 2.5f;
            so.FindProperty("pauseTime").floatValue = 0.6f;
            so.ApplyModifiedProperties();
        }
    }

    private static void SetupCheckpoints()
    {
        CreateCheckpoint("Checkpoint_AfterBoss", new Vector2(70f, 4.5f));
        CreateCheckpoint("Checkpoint_JumpMap", new Vector2(105f, 3.5f));
        CreateCheckpoint("Checkpoint_Upper", new Vector2(128f, 5.5f));
    }

    private static void CreateCheckpoint(string name, Vector2 position)
    {
        GameObject parent = GameObject.Find("Checkpoints");
        if (parent == null) parent = new GameObject("Checkpoints");

        Transform existing = parent.transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = position;

        var box = GetOrAdd<BoxCollider2D>(go);
        box.isTrigger = true;
        box.size = new Vector2(1f, 3f);
        GetOrAdd<Checkpoint>(go);
    }

    private static void SetupPlayer()
    {
        GameObject player = GameObject.Find("sword_man");
        if (player == null) return;

        GetOrAdd<PlayerRespawn>(player);

        // 레거시 Sword_Man은 꺼져 있어도 OnTriggerEnter2D 같은 물리 메시지를 계속 받아
        // 태그(key, reward, portal 등)에 닿을 때마다 원작 로직이 실행되며 오류를 냄 → 제거
        foreach (var mb in player.GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name == "Sword_Man")
                Undo.DestroyObjectImmediate(mb);
        }
    }

    // 점프맵 끝의 레거시 Golem2(EnemyAI가 플레이어의 Sword_Man을 매 프레임 참조)를 비활성화하고
    // 같은 자리에 Remake Golem 스포너 배치
    private static void ReplaceLegacyEnemies()
    {
        GameObject legacy = GameObject.Find("Golem2");
        if (legacy != null)
        {
            legacy.SetActive(false);
            legacy.name = "Golem2 (레거시, 스포너로 대체됨)";
        }

        GameObject parent = GameObject.Find("Spawners");
        if (parent == null) parent = new GameObject("Spawners");
        const string name = "Spawner_JumpMap_Golem";
        Transform existing = parent.transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(134f, 6f, 0f); // x 127 ~ 137 지면(표면 높이 4) 위

        var spawner = GetOrAdd<MonsterSpawner>(go);
        var so = new SerializedObject(spawner);
        so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Monster>("Assets/Remake/Prefabs/Golem.prefab");
        so.FindProperty("maxAlive").intValue = 1;
        so.FindProperty("respawnDelay").floatValue = 15f;
        so.ApplyModifiedProperties();
    }

    // 점프맵 중간의 보상 상자 : 원작은 닿으면 레벨업 선택창 → 공격해서 부수면 LV+1 아이템 드롭
    private static void SetupRewardBox()
    {
        GameObject box = GameObject.Find("box");
        if (box == null) box = GameObject.Find("RewardBox");
        if (box == null) return;

        box.name = "RewardBox";
        box.layer = LayerMask.NameToLayer("Enemy"); // 플레이어 공격 판정(whatIsEnemy = Enemy) 대상이 되도록

        // 레거시 trigger.cs : 매 프레임 Trap3을 밀어 올리고, trigger 태그에 닿으면 엔딩 씬을 불러오던 스크립트
        foreach (var mb in box.GetComponents<MonoBehaviour>())
        {
            if (mb != null && mb.GetType().Name == "trigger")
                Undo.DestroyObjectImmediate(mb);
        }

        var breakable = GetOrAdd<BreakableBox>(box);
        var pieces = AssetDatabase.LoadAllAssetsAtPath("Assets/External/Pixel Adventure 1/Assets/Items/Boxes/Box3/Break (28x24).png")
            .OfType<Sprite>().OrderBy(sp => sp.name).ToArray();
        var so = new SerializedObject(breakable);
        SerializedProperty array = so.FindProperty("breakPieces");
        array.arraySize = pieces.Length;
        for (int i = 0; i < pieces.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];
        so.ApplyModifiedProperties();
    }

    // 맨 오른쪽 포탈 : G키 상호작용으로 다음 스테이지(Stage2) 이동
    private static void SetupStagePortal()
    {
        GameObject portal = GameObject.Find("Portal");
        if (portal != null)
        {
            var stagePortal = GetOrAdd<StagePortal>(portal);
            var so = new SerializedObject(stagePortal);
            so.FindProperty("nextSceneName").stringValue = "Stage2"; // 리메이크 2스테이지 (Tools/Deus/Build Stage 2)
            so.ApplyModifiedProperties();
        }

        GameObject player = GameObject.Find("sword_man");
        if (player != null) GetOrAdd<PlayerInteractor>(player);
    }

    // Unity 오브젝트는 없을 때 가짜 null을 반환할 수 있어 ?? 연산자 대신 명시적으로 검사
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
