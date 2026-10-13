using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// 보스전 전에 레벨업할 수 있도록 시작 지점 왼쪽으로 사냥 구간을 확장.
// - 기존 지면 타일(왼쪽 끝 기둥 / 채움 기둥)을 그대로 복사해 FieldLength칸 늘림
// - 왼쪽 끝에 보이지 않는 벽, 카메라 왼쪽 한계 조정, 플레이어 시작 위치 이동
// - 구간에 몬스터 스포너 배치 (Golem / 마녀 번갈아, 중간에 Golem 2마리 무리)
// 메뉴 : Tools/Deus/Build Hunting Field  (RemakeTest 씬에서 실행, 여러 번 실행해도 결과 동일)
public static class HuntingFieldBuilder
{
    private const int OldLeftEdgeX = -11;  // 기존 지면 왼쪽 끝 타일
    private const int FillTemplateX = -10; // 채움용으로 복사할 기둥
    private const int FieldLength = 60;
    private const int TileMinY = -4;
    private const int TileMaxY = -2;       // 지면 윗면 타일 (표면 높이 = -1)
    private const float SurfaceY = -1f;

    private const string GolemPrefab = "Assets/Remake/Prefabs/Golem.prefab";
    private const string WitchPrefab = "Assets/Remake/Prefabs/Witch.prefab";

    [MenuItem("Tools/Deus/Build Hunting Field")]
    public static void Build()
    {
        int newLeftX = OldLeftEdgeX - FieldLength;

        ExtendGround(newLeftX);
        CreateLeftBoundary(newLeftX);
        SetCameraLimit(newLeftX);
        MovePlayerStart(newLeftX + 5f);
        CreateSpawners();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"사냥 구간 생성 완료 : x {newLeftX} ~ {OldLeftEdgeX}");
    }

    private static void ExtendGround(int newLeftX)
    {
        Tilemap tilemap = GameObject.Find("Grid/Tilemap")?.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("Grid/Tilemap을 찾을 수 없습니다.");
            return;
        }
        if (tilemap.HasTile(new Vector3Int(newLeftX, TileMaxY, 0)))
            return; // 이미 확장됨

        int height = TileMaxY - TileMinY + 1;
        var edge = new TileBase[height];
        var fill = new TileBase[height];
        for (int i = 0; i < height; i++)
        {
            edge[i] = tilemap.GetTile(new Vector3Int(OldLeftEdgeX, TileMinY + i, 0));
            fill[i] = tilemap.GetTile(new Vector3Int(FillTemplateX, TileMinY + i, 0));
        }

        Undo.RecordObject(tilemap, "Build Hunting Field");
        // 기존 왼쪽 끝 기둥은 이제 중간이 되므로 채움 기둥으로 교체, 새 왼쪽 끝에 끝 기둥 배치
        for (int x = newLeftX; x <= OldLeftEdgeX; x++)
        {
            TileBase[] column = x == newLeftX ? edge : fill;
            for (int i = 0; i < height; i++)
                tilemap.SetTile(new Vector3Int(x, TileMinY + i, 0), column[i]);
        }
    }

    private static void CreateLeftBoundary(int newLeftX)
    {
        GameObject wall = GameObject.Find("LeftBoundary");
        if (wall == null) wall = new GameObject("LeftBoundary");
        wall.layer = LayerMask.NameToLayer("Ground");
        wall.transform.position = new Vector3(newLeftX - 0.5f, 5f, 0f);
        var box = GetOrAdd<BoxCollider2D>(wall);
        box.size = new Vector2(1f, 20f);
    }

    private static void SetCameraLimit(int newLeftX)
    {
        GameObject cam = GameObject.Find("Main Camera");
        if (cam == null) return;
        // 레거시 카메라 스크립트(camera)의 limitMinX
        foreach (var mb in cam.GetComponents<MonoBehaviour>())
        {
            var so = new SerializedObject(mb);
            SerializedProperty limit = so.FindProperty("limitMinX");
            if (limit == null) continue;
            limit.floatValue = newLeftX - 0.5f;
            so.ApplyModifiedProperties();
        }
    }

    private static void MovePlayerStart(float x)
    {
        GameObject player = GameObject.Find("sword_man");
        if (player == null) return;
        Undo.RecordObject(player.transform, "Build Hunting Field");
        player.transform.position = new Vector3(x, SurfaceY + 1f, 0f);
    }

    private static void CreateSpawners()
    {
        // (이름, 프리팹, x, 동시 수, 분산 반경, 리스폰)
        CreateSpawner("Spawner_Field_1", GolemPrefab, -60f, 1, 0f, 8f);
        CreateSpawner("Spawner_Field_2", WitchPrefab, -50f, 1, 0f, 10f);
        CreateSpawner("Spawner_Field_3", GolemPrefab, -40f, 2, 3f, 10f); // Golem 2마리 무리
        CreateSpawner("Spawner_Field_4", WitchPrefab, -30f, 1, 0f, 10f);
        CreateSpawner("Spawner_Field_5", GolemPrefab, -20f, 1, 0f, 8f);
    }

    private static void CreateSpawner(string name, string prefabPath, float x, int maxAlive, float radius, float respawn)
    {
        GameObject parent = GameObject.Find("Spawners");
        if (parent == null) parent = new GameObject("Spawners");
        Transform existing = parent.transform.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        // 프리팹 원점 기준 지면 위 높이 (Golem은 몸 중심, 마녀는 스프라이트 중심)
        float heightAboveGround = prefabPath == GolemPrefab ? 2f : 1.7f;
        go.transform.position = new Vector3(x, SurfaceY + heightAboveGround, 0f);

        var spawner = GetOrAdd<MonsterSpawner>(go);
        var so = new SerializedObject(spawner);
        so.FindProperty("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Monster>(prefabPath);
        so.FindProperty("maxAlive").intValue = maxAlive;
        so.FindProperty("spawnRadius").floatValue = radius;
        so.FindProperty("respawnDelay").floatValue = respawn;
        so.ApplyModifiedProperties();
    }

    // Unity 오브젝트는 없을 때 가짜 null을 반환할 수 있어 ?? 연산자 대신 명시적으로 검사
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
