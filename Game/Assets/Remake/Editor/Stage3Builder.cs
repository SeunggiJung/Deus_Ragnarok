using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// 3스테이지 리메이크 변환.
// 원작 third_stage를 Assets/Remake/Scenes/Stage3.unity로 복사(원작 씬은 그대로)한 뒤 리메이크 시스템으로 교체
// - 플레이어 / HUD : Player.prefab, PlayerHUD.prefab (1~2스테이지와 같은 조작 / UI / 성장 이어받기), 카메라 대상 연결, 레거시 Canvas 비활성화
// - 졸개 4마리(Skeleton x2, Goblin x2) : 레거시 Enemy / EnemyAI → Monster FSM
// - 하데스 : 레거시 Hades / HadesAI → Behavior Tree 보스 (HadesBossBuilder), 2형태(부활) 지원
// - 전투 흐름 : 제단(Spawner) → 졸개 → 하데스 → 레버 → 부활 → 레버 → 출구 문 (HadesEncounter / HadesLever)
// - 출구 문 너머 트리거 : 엔딩 씬 (EndingTrigger)
// 메뉴 : Tools/Deus/Build Stage 3  (어느 씬에서 실행해도 Stage3 씬을 열고 구성, 여러 번 실행해도 결과 동일)
public static class Stage3Builder
{
    private const string LegacyScenePath = "Assets/Legacy/Scenes/third_stage.unity";
    private const string ScenePath = "Assets/Remake/Scenes/Stage3.unity";
    private const string DataFolder = "Assets/Remake/Data";

    [MenuItem("Tools/Deus/Build Stage 3")]
    public static void Build()
    {
        if (!OpenStageScene()) return;

        Tilemap ground = Stage2Builder.FindInScene("Tilemap")?.GetComponent<Tilemap>();

        GameObject player = Stage2Builder.SetupPlayer(ground);
        Stage2Builder.SetupCamera(player);
        Stage2Builder.SetupHud();

        Monster[] minions = SetupMinions();
        HadesBoss hades = HadesBossBuilder.Convert();
        SetupEncounter(minions, hades);
        SetupEnding();
        SetupCheckpoints(ground);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AddToBuildSettings();
        Debug.Log("3스테이지 리메이크 구성 완료 : " + ScenePath);
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
    #endregion

    #region Minions
    private static Monster[] SetupMinions()
    {
        EnemyStats skeleton = Stage2Builder.LoadOrCreate<EnemyStats>("SkeletonStats", s =>
        {
            // 원작 Skeleton : 체력 300, 데미지 8, 쿨다운 1.0, 이동 1.5, 공격 범위 4, 시야 7
            s.maxHp = 130f; s.invincibleTime = 0.2f; s.attackDamage = 10f; s.attackRadius = 1.8f;
            s.moveSpeed = 1.8f; s.detectRange = 14f; s.loseRange = 24f; s.attackRange = 2.6f;
            s.attackCooldown = 1.4f; s.expReward = 60f; s.hitStunTime = 0.15f; s.deathDelay = 1.2f;
        });
        EnemyStats goblin = Stage2Builder.LoadOrCreate<EnemyStats>("GoblinStats", s =>
        {
            // 원작 Goblin : 체력 250, 데미지 10, 쿨다운 0.7, 이동 1.5, 공격 범위 3.5, 시야 7
            s.maxHp = 110f; s.invincibleTime = 0.2f; s.attackDamage = 12f; s.attackRadius = 1.6f;
            s.moveSpeed = 2.0f; s.detectRange = 14f; s.loseRange = 24f; s.attackRange = 2.4f;
            s.attackCooldown = 1.1f; s.expReward = 50f; s.hitStunTime = 0.15f; s.deathDelay = 1.2f;
        });

        // 웨이브 전투 : 거리와 상관없이 플레이어를 끝까지 추적 (방 안 어디에 있든 따라옴)
        foreach (EnemyStats s in new[] { skeleton, goblin })
        {
            s.detectRange = 200f;
            s.loseRange = 400f;
            EditorUtility.SetDirty(s);
        }
        AssetDatabase.SaveAssets();

        // 걷기 반복 등을 손본 리메이크용 애니메이터 컨트롤러
        RuntimeAnimatorController skeletonAnim = MinionAnimationBuilder.Prepare(MinionAnimationBuilder.SkeletonSource, "Skeleton");
        RuntimeAnimatorController goblinAnim = MinionAnimationBuilder.Prepare(MinionAnimationBuilder.GoblinSource, "Goblin");

        // 4마리 모두 애니메이터의 공격 상태 이름은 "Attack"
        var minions = new[]
        {
            Stage2Builder.ConvertMonster("Goblin1", "Goblin1", "Attack", goblin),
            Stage2Builder.ConvertMonster("Skeleton1", "Skeleton1", "Attack", skeleton),
            Stage2Builder.ConvertMonster("Skeleton2", "Skeleton2", "Attack", skeleton),
            Stage2Builder.ConvertMonster("Goblin2", "Goblin2", "Attack", goblin),
        }.Where(m => m != null).ToArray();

        foreach (Monster m in minions)
        {
            bool isSkeleton = m.name.StartsWith("Skeleton");
            var animator = m.GetComponent<Animator>();
            if (animator != null)
                animator.runtimeAnimatorController = isSkeleton ? skeletonAnim : goblinAnim;

            // 개체별로 속도 / 공격 거리 / 공격 간격을 조금씩 다르게 (여러 마리가 겹쳐 같은 타이밍에 움직이지 않도록)
            var so = new SerializedObject(m);
            so.FindProperty("varyMotion").boolValue = true;
            so.ApplyModifiedProperties();
        }
        return minions;
    }
    #endregion

    #region Encounter
    private static void SetupEncounter(Monster[] minions, HadesBoss hades)
    {
        GameObject altar = Stage2Builder.FindInScene("Spawner") ?? Stage2Builder.FindInScene("HadesAltar");
        GameObject lever = Stage2Builder.FindInScene("Lever") ?? Stage2Builder.FindInScene("HadesLever");
        GameObject door = Stage2Builder.FindInScene("Door");
        GameObject trap = Stage2Builder.FindInScene("Trap");
        if (altar == null || hades == null)
        {
            Debug.LogError("제단(Spawner) 또는 하데스를 찾을 수 없습니다.");
            return;
        }

        altar.name = "HadesAltar";

        // 출구 문 / 왼쪽 벽 : 바닥으로 인식되도록 Ground 레이어, 플레이어가 밀 수 없도록 Kinematic
        foreach (GameObject wall in new[] { door, trap })
        {
            if (wall == null) continue;
            wall.layer = LayerMask.NameToLayer("Ground");
            var rb = wall.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
            }
        }

        var encounter = Stage2Builder.GetOrAdd<HadesEncounter>(altar);
        var so = new SerializedObject(encounter);
        SerializedProperty array = so.FindProperty("minions");
        array.arraySize = minions.Length;
        for (int i = 0; i < minions.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = minions[i];
        // 웨이브별 몬스터 수 (4웨이브, 뒤로 갈수록 많아짐)
        SerializedProperty waves = so.FindProperty("waveSizes");
        int[] sizes = { 4, 6, 9, 12 };
        waves.arraySize = sizes.Length;
        for (int i = 0; i < sizes.Length; i++)
            waves.GetArrayElementAtIndex(i).intValue = sizes[i];
        so.FindProperty("hades").objectReferenceValue = hades;
        so.FindProperty("lever").objectReferenceValue = lever;
        so.FindProperty("door").objectReferenceValue = door != null ? door.transform : null;
        so.ApplyModifiedProperties();

        if (lever != null)
        {
            lever.name = "HadesLever";
            var hadesLever = Stage2Builder.GetOrAdd<HadesLever>(lever);
            var lso = new SerializedObject(hadesLever);
            lso.FindProperty("encounter").objectReferenceValue = encounter;
            lso.ApplyModifiedProperties();
            lever.SetActive(false);   // 하데스 1형태 처치 후 등장
        }

        // 졸개 / 하데스는 제단을 사용하기 전까지 숨김 (HadesEncounter.Awake에서도 처리하지만 씬에도 저장)
        foreach (var m in minions) m.gameObject.SetActive(false);
        hades.gameObject.SetActive(false);
    }

    private static void SetupEnding()
    {
        GameObject trigger = Stage2Builder.FindInScene("trigger") ?? Stage2Builder.FindInScene("EndingTrigger");
        if (trigger == null) return;
        trigger.name = "EndingTrigger";
        var box = Stage2Builder.GetOrAdd<BoxCollider2D>(trigger);
        box.isTrigger = true;
        box.size = new Vector2(1f, 6f);   // 점프해서 지나가도 닿도록 세로로 길게
        Stage2Builder.GetOrAdd<EndingTrigger>(trigger);
    }
    #endregion

    #region Checkpoints
    private static void SetupCheckpoints(Tilemap ground)
    {
        float[] xs = { -8f, 6f };   // 시작 / 제단 앞
        GameObject parent = Stage2Builder.FindInScene("Checkpoints") ?? new GameObject("Checkpoints");
        for (int i = 0; i < xs.Length; i++)
        {
            string name = $"Checkpoint_{i + 1}";
            Transform t = parent.transform.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.position = new Vector3(xs[i], Stage2Builder.GroundY(ground, xs[i]) + 1.5f, 0f);
            var box = Stage2Builder.GetOrAdd<BoxCollider2D>(go);
            box.isTrigger = true;
            box.size = new Vector2(1f, 3f);
            Stage2Builder.GetOrAdd<Checkpoint>(go);
        }
    }
    #endregion
}
