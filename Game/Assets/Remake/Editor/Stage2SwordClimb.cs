using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 2스테이지 보스 처치 후 검(SwordPickup)까지 가는 길 재구성.
// 원작은 흩어진 1칸 돌 + 탑을 텔레포트(4칸 이동, 도착 지점이 벽이면 위로 이동)로 오르는 구조였는데,
// 리메이크 플레이어는 텔레포트 대신 점프 + 구르기라서 같은 지형으로는 오를 수 없다 → 점프 / 공중 구르기로 오르는 계단 발판으로 교체
//
// 플레이어 기준(moveSpeed 3.5, 점프 높이 1.4, 중력 1.7배, 구르기 3칸):
//  - 점프만으로 한 칸(1) 오르며 건널 수 있는 틈 : 약 2 이하  → 쉬운 도약은 틈 1
//  - 점프 + 공중 구르기(정점에서)로 한 칸 오르며 건널 수 있는 틈 : 약 4 이하 → 구르기 도약은 틈 3 (점프만으로는 못 건넘)
//  - 공중 구르기는 체공 중 1회 + 스태미나 15 → 구르기 도약 사이에는 반드시 쉬운 도약을 끼움
// 마지막 발판(검 발판)에서 오른쪽으로 걸어 내려가면 원래 지면(x 134~)에 착지
// 메뉴 : Tools/Deus/Stage 2 - Build Sword Climb  (Stage2 씬에서 실행, 여러 번 실행해도 결과 동일. Build Stage 2에서도 호출)
public static class Stage2SwordClimb
{
    private const string PlatformPrefix = "SwordClimb_";
    private const int ClearFromX = 89;        // 문 뒤 지면 끝(x 88) 다음부터
    private const int ClearToX = 133;         // 다음 지면(x 134) 직전까지
    private const int FloorTop = 0;           // 문 앞 지면 끝(x 86~88)의 윗면 높이
    private const int SurfaceTileY = -2;      // 발판 모양으로 쓸 지면 윗줄 타일의 셀 y

    private const int FirstGap = 1;                                  // 지면 끝 → 첫 발판 (쉬운 도약)
    // 발판 사이 틈. 구르기 도약(3)과 쉬운 도약(1)을 번갈아 한 칸씩 오름
    private static readonly int[] HopGaps = { 3, 1, 3, 1, 3, 1, 3, 1, 3 };
    private const int PlatformWidth = 2;
    private const int SwordPlatformWidth = 7;  // 마지막(검) 발판
    private const int CheckpointPlatform = 5;  // 이 번호(1부터)의 발판에 체크포인트

    [MenuItem("Tools/Deus/Stage 2 - Build Sword Climb")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        Transform grid = FindByName("Grid");
        Tilemap ground = FindByName("Tilemap") != null ? FindByName("Tilemap").GetComponent<Tilemap>() : null;
        if (grid == null || ground == null)
        {
            Debug.LogError("Stage2 씬의 Grid / Tilemap을 찾을 수 없습니다. Stage2 씬을 연 뒤 실행하세요.");
            return;
        }

        // 발판 모양 : 문 앞 지면의 윗줄 타일
        TileBase tile = ground.GetTile(new Vector3Int(60, SurfaceTileY, 0));
        var groundRenderer = ground.GetComponent<TilemapRenderer>();

        // 원작 돌 / 탑 제거
        BoundsInt bounds = ground.cellBounds;
        for (int y = bounds.yMin; y < bounds.yMax; y++)
            for (int x = ClearFromX; x <= ClearToX; x++)
                ground.SetTile(new Vector3Int(x, y, 0), null);

        // 이전에 만든 발판 제거
        for (int i = grid.childCount - 1; i >= 0; i--)
        {
            Transform child = grid.GetChild(i);
            if (child.name.StartsWith(PlatformPrefix))
                Object.DestroyImmediate(child.gameObject);
        }

        int left = ClearFromX + FirstGap;
        int top = FloorTop + 1;
        float checkpointX = 0f, checkpointTop = 0f;
        float swordX = 0f, swordTop = 0f;
        int count = HopGaps.Length + 1;

        for (int i = 0; i < count; i++)
        {
            bool last = i == count - 1;
            int width = last ? SwordPlatformWidth : PlatformWidth;
            if (last && left + width - 1 != ClearToX)
                Debug.LogWarning($"검 발판 끝(x {left + width - 1})이 다음 지면 직전(x {ClearToX})과 맞지 않습니다. HopGaps를 조정하세요.");

            AddPlatform(grid, groundRenderer, tile, i + 1, left, top, width);

            if (i + 1 == CheckpointPlatform)
            {
                checkpointX = left + width * 0.5f;
                checkpointTop = top;
            }
            if (last)
            {
                swordX = left + width * 0.5f;
                swordTop = top;
                break;
            }

            left += width + HopGaps[i];
            top += 1;
        }

        MoveCheckpoint(checkpointX, checkpointTop + 1.5f);
        MoveSword(swordX, swordTop);
        MoveSpawners();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"검 가는 길 구성 완료 : 발판 {count}개, 검 발판 윗면 y {swordTop}");
    }

    private static void AddPlatform(Transform grid, TilemapRenderer template, TileBase tile, int index, int left, int top, int width)
    {
        var go = new GameObject($"{PlatformPrefix}{index}");
        go.transform.SetParent(grid, false);
        go.layer = LayerMask.NameToLayer("Ground");

        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        if (template != null)
        {
            renderer.sortingLayerID = template.sortingLayerID;
            renderer.sortingOrder = template.sortingOrder;
        }
        for (int i = 0; i < width; i++)
            tilemap.SetTile(new Vector3Int(left + i, top - 1, 0), tile);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        go.AddComponent<TilemapCollider2D>();
    }

    private static void MoveCheckpoint(float x, float y)
    {
        Transform cp = FindByName("Checkpoint_3");
        if (cp != null) cp.position = new Vector3(x, y, 0f);
    }

    // 검은 마지막 발판 위 가운데
    private static void MoveSword(float x, float top)
    {
        Transform sword = FindByName("SwordPickup");
        if (sword == null) return;
        // 검 콜라이더(높이 약 3)의 아래가 발판 윗면에 닿도록 원래 높이 차(탑 윗면 9 → 중심 10.45)를 유지
        sword.position = new Vector3(x, top + 1.45f, sword.position.z);
    }

    // 사냥 스포너는 돌 위에 있었으므로 새 구조에서는 허공 → 다음 지면(x 134~)으로 이동
    private static void MoveSpawners()
    {
        Place("Spawner_S2_1", 138f, 4f);
        Place("Spawner_S2_2", 158f, 3.7f);
        Place("Spawner_S2_3", 178f, 4f);
    }

    private static void Place(string name, float x, float y)
    {
        Transform t = FindByName(name);
        if (t == null) return;
        t.position = new Vector3(x, y, 0f);   // 지면 윗면(2) 위
    }

    private static Transform FindByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
