using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 보스 셋업 도구.
// 1. Boss.controller 생성 : 기존 Golem_Reinforced_Controller는 공격 모션 1개만 연결돼 있어,
//    에셋 팩의 모든 모션(근접 / 지면 가시 / 투척 / 능력 / 피격 / 사망)을 트리거로 재생하는 컨트롤러를 새로 만듦
// 2. BossStats 에셋 생성 (투사체 스프라이트는 에셋 팩의 Attack_3_Projectile 첫 프레임)
// 3. 씬의 레거시 Boss를 Boss.prefab으로 변환 (레거시 Enemy / EnemyAI 제거, BossController 추가)
// 메뉴 : Tools/Deus/Build Boss  (RemakeTest 씬에서 실행)
public static class BossBuilder
{
    private const string AnimFolder = "Assets/External/Enemy Galore 1 - Pixel Art/Animations";
    private const string SpriteSheet = "Assets/External/Enemy Galore 1 - Pixel Art/Sprites/Reinforced Golem/Golem_Armor_Full_FX.png";
    private const string ControllerPath = "Assets/Remake/Animations/Boss/Boss.controller";
    private const string StatsPath = "Assets/Remake/Data/BossStats.asset";
    private const string PrefabPath = "Assets/Remake/Prefabs/Boss.prefab";
    private const string SceneBossName = "Boss";

    [MenuItem("Tools/Deus/Build Boss")]
    public static void Build()
    {
        AnimatorController controller = BuildController();
        BossStats stats = LoadOrCreateStats();
        BuildPrefab(controller, stats);
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimFolder}/Golem_Reinforced_{name}.anim");

    private static AnimatorController BuildController()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
        AssetDatabase.DeleteAsset(ControllerPath); // 재실행 시 깨끗하게 다시 생성
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter(BossController.AnimParams.Moving, AnimatorControllerParameterType.Bool);
        string[] triggers =
        {
            BossController.AnimParams.Melee, BossController.AnimParams.Slam, BossController.AnimParams.Throw,
            BossController.AnimParams.Ability, BossController.AnimParams.Hit, BossController.AnimParams.Die
        };
        foreach (string t in triggers)
            controller.AddParameter(t, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idle = sm.AddState("Idle", new Vector3(300, 0));
        idle.motion = Clip("Idle");
        sm.defaultState = idle;

        AnimatorState run = sm.AddState("Run", new Vector3(550, 0));
        run.motion = Clip("Run");

        var toRun = idle.AddTransition(run);
        toRun.hasExitTime = false;
        toRun.duration = 0f;
        toRun.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Moving);
        var toIdle = run.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, BossController.AnimParams.Moving);

        // 행동 모션 : Any State에서 트리거로 진입, 끝나면 Idle로 복귀
        AddAction(sm, idle, "Melee", Clip("Attack_2"), BossController.AnimParams.Melee, new Vector3(50, 150));
        AddAction(sm, idle, "Slam", Clip("Attack_1"), BossController.AnimParams.Slam, new Vector3(250, 150));
        AddAction(sm, idle, "Throw", Clip("Attack_3"), BossController.AnimParams.Throw, new Vector3(450, 150));
        AddAction(sm, idle, "Ability", Clip("Ability"), BossController.AnimParams.Ability, new Vector3(650, 150));
        AddAction(sm, idle, "Hit", Clip("Hit"), BossController.AnimParams.Hit, new Vector3(50, 250));

        // 사망 : 복귀 없음
        AnimatorState die = sm.AddState("Die", new Vector3(250, 250));
        die.motion = Clip("Death");
        var toDie = sm.AddAnyStateTransition(die);
        toDie.hasExitTime = false;
        toDie.duration = 0f;
        toDie.canTransitionToSelf = false;
        toDie.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Die);

        EditorUtility.SetDirty(controller);
        Debug.Log($"보스 애니메이터 생성 : {ControllerPath}");
        return controller;
    }

    private static void AddAction(AnimatorStateMachine sm, AnimatorState idle, string name, AnimationClip clip,
                                  string trigger, Vector3 position)
    {
        AnimatorState state = sm.AddState(name, position);
        state.motion = clip;

        var enter = sm.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.duration = 0f;
        enter.canTransitionToSelf = false;
        enter.AddCondition(AnimatorConditionMode.If, 0, trigger);

        var exit = state.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.duration = 0f;
    }

    private static BossStats LoadOrCreateStats()
    {
        var stats = AssetDatabase.LoadAssetAtPath<BossStats>(StatsPath);
        if (stats != null) return stats;

        stats = ScriptableObject.CreateInstance<BossStats>();
        stats.maxHp = 400f;            // 원작 Boss는 600 (Remake 플레이어 공격력 기준으로 조정)
        stats.invincibleTime = 0.15f;
        stats.attackDamage = 15f;
        stats.attackRadius = 1.8f;

        // 에너지탄 스프라이트 : Golem_Reinforced_Attack_3_Projectile의 첫 프레임
        var projectileClip = Clip("Attack_3_Projectile");
        var binding = AnimationUtility.GetObjectReferenceCurveBindings(projectileClip).FirstOrDefault();
        var frames = AnimationUtility.GetObjectReferenceCurve(projectileClip, binding);
        if (frames != null && frames.Length > 0)
            stats.projectileSprite = frames[0].value as Sprite;

        AssetDatabase.CreateAsset(stats, StatsPath);
        return stats;
    }

    private static void BuildPrefab(AnimatorController controller, BossStats stats)
    {
        GameObject source = GameObject.Find(SceneBossName);
        if (source == null)
        {
            Debug.LogWarning($"씬에서 {SceneBossName}을(를) 찾을 수 없어 보스 프리팹 생성을 건너뜁니다.");
            return;
        }

        GameObject go = Object.Instantiate(source);
        go.name = "Boss";

        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            string typeName = mb.GetType().Name;
            if (typeName == "Enemy" || typeName == "EnemyAI" || typeName == "WorldHealthBar")
                Object.DestroyImmediate(mb);
        }

        var boss = GetOrAdd<BossController>(go);
        var so = new SerializedObject(boss);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("whatIsEnemy").intValue = LayerMask.GetMask("Player");
        so.ApplyModifiedPropertiesWithoutUndo();

        go.GetComponent<Animator>().runtimeAnimatorController = controller;

        if (go.transform.Find("AttackCheck") == null)
        {
            var col = go.GetComponent<CapsuleCollider2D>();
            var child = new GameObject("AttackCheck").transform;
            child.SetParent(go.transform, false);
            child.localPosition = col != null ? new Vector2(col.offset.x + col.size.x * 0.5f, col.offset.y) : Vector2.zero;
            child.gameObject.layer = LayerMask.NameToLayer("Enemy");
        }

        go.layer = LayerMask.NameToLayer("Enemy");
        var rb = go.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        go.transform.position = Vector3.zero;

        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"보스 프리팹 생성 : {PrefabPath}");
    }

    // Unity 오브젝트는 없을 때 가짜 null을 반환할 수 있어 ?? 연산자 대신 명시적으로 검사
    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
