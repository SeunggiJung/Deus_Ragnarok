using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 3스테이지 하데스를 레거시(Hades / HadesAI)에서 Behavior Tree 보스(HadesBoss)로 변환하는 도구.
// - HadesBoss.controller : 원작 Hades.controller의 모션(1형태 Wizard_* / 2형태 Rades_*)을 그대로 재사용하고
//   트리거(melee / slam / throw / hit / die / spawn) + revived 불 값으로 두 형태의 모션 세트를 전환
// - HadesBossStats 에셋 : 1형태 체력 900 / 2형태 1200, 화염탄 비 수치. 화염구 / 폭발 스프라이트는 드래곤 이펙트 팩 사용
// - 씬의 Hades : Hades / HadesAI 제거 → HadesBoss 추가, 보스 체력바 연결
// 메뉴 : Tools/Deus/Build Hades Boss (Stage3 씬에서 실행. Stage3Builder.Build에도 포함되어 여러 번 실행해도 결과가 같음)
public static class HadesBossBuilder
{
    private const string LegacyControllerPath = "Assets/External/EVil Wizard/Animations/Hades.controller";
    private const string ControllerPath = "Assets/Remake/Animations/Boss/HadesBoss.controller";
    private const string StatsPath = "Assets/Remake/Data/HadesBossStats.asset";
    private const string FxFolder = "Assets/External/Dragon Warrior Files/Effects";
    private const string SceneName = "Hades";

    [MenuItem("Tools/Deus/Build Hades Boss")]
    public static void Build()
    {
        if (Convert() == null) return;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    public static HadesBoss Convert()
    {
        GameObject go = Stage2Builder.FindInScene(SceneName);
        if (go == null)
        {
            Debug.LogError($"씬에서 {SceneName}을(를) 찾을 수 없습니다. Stage3 씬에서 실행하세요.");
            return null;
        }

        AnimatorController controller = BuildController();
        if (controller == null) return null;
        HadesBossStats stats = LoadOrCreateStats();

        Stage2Builder.RemoveLegacyScripts(go, "Hades", "HadesAI");

        var boss = Stage2Builder.GetOrAdd<HadesBoss>(go);
        Transform attackCheck = go.transform.Find("AttackCheck");
        if (attackCheck == null)
        {
            var col = go.GetComponent<CapsuleCollider2D>();
            var child = new GameObject("AttackCheck").transform;
            child.SetParent(go.transform, false);
            child.localPosition = col != null ? new Vector2(col.offset.x + col.size.x * 0.5f, col.offset.y) : Vector2.zero;
            child.gameObject.layer = LayerMask.NameToLayer("Enemy");
            attackCheck = child;
        }

        var so = new SerializedObject(boss);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("whatIsEnemy").intValue = LayerMask.GetMask("Player");
        so.FindProperty("attackCheck").objectReferenceValue = attackCheck;
        so.ApplyModifiedProperties();

        go.GetComponent<Animator>().runtimeAnimatorController = controller;
        go.layer = LayerMask.NameToLayer("Enemy");
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        SetupHealthBar(boss);

        EditorUtility.SetDirty(go);
        Debug.Log("Hades 보스 변환 완료 : HadesBoss + " + ControllerPath);
        return boss;
    }

    private static void SetupHealthBar(HadesBoss hades)
    {
        BossHealthBar bar = null;
        foreach (var b in Resources.FindObjectsOfTypeAll<BossHealthBar>())
        {
            if (!b.gameObject.scene.IsValid()) continue;
            bar = b;
            break;
        }
        if (bar == null)
        {
            var barGo = new GameObject("HadesHealthBar");
            bar = barGo.AddComponent<BossHealthBar>();
        }
        var so = new SerializedObject(bar);
        so.FindProperty("boss").objectReferenceValue = hades;
        so.ApplyModifiedProperties();
    }

    #region Stats
    // 에셋이 없을 때만 수치를 채움 (이후 수치 조정은 에셋에서). 스프라이트 연결은 매번 갱신
    private static HadesBossStats LoadOrCreateStats()
    {
        var stats = AssetDatabase.LoadAssetAtPath<HadesBossStats>(StatsPath);
        if (stats == null)
        {
            stats = ScriptableObject.CreateInstance<HadesBossStats>();
            stats.bossName = "Hades";
            stats.expReward = 400f;                 // 1형태 / 2형태 처치마다 지급

            stats.maxHp = 900f;                     // 원작 1000
            stats.invincibleTime = 0.15f;
            stats.attackDamage = 26f;               // 원작 30
            stats.attackRadius = 3f;

            stats.moveSpeed = 2f;                   // 원작 2.0
            stats.baseAnimSpeed = 1f;
            stats.detectRange = 60f;                // 등장 연출이 끝나면 바로 전투 시작 (HadesBoss.spawnDelay)
            stats.attackRecovery = 0.9f;

            stats.meleeRange = 6f;                  // 원작 공격 범위 6
            stats.meleeCooldown = 1.8f;

            stats.guardCooldown = 999f;             // 방어 자세는 사용하지 않음
            stats.maxPoise = 220f;
            stats.staggerTime = 1.2f;
            stats.shatterForce = 9f;

            stats.phase2Threshold = 0f;             // 2페이즈는 체력이 아니라 부활(Revive)로 진입

            AssetDatabase.CreateAsset(stats, StatsPath);
        }

        stats.fireballFrames = LoadSprites(FxFolder, "fireball_0", 1, 3);
        stats.explosionFrames = LoadSprites(FxFolder, "explosion_0", 1, 6);
        EditorUtility.SetDirty(stats);
        AssetDatabase.SaveAssets();
        return stats;
    }

    private static Sprite[] LoadSprites(string folder, string prefix, int from, int to)
    {
        var list = new List<Sprite>();
        for (int i = from; i <= to; i++)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{prefix}{i}.png");
            if (sprite != null) list.Add(sprite);
        }
        return list.ToArray();
    }
    #endregion

    #region Animation
    // 원작 컨트롤러의 상태 이름으로 모션(클립) 찾기
    private static Dictionary<string, Motion> LoadLegacyMotions()
    {
        var legacy = AssetDatabase.LoadAssetAtPath<AnimatorController>(LegacyControllerPath);
        if (legacy == null)
        {
            Debug.LogError($"원작 하데스 컨트롤러를 찾을 수 없습니다 : {LegacyControllerPath}");
            return null;
        }
        var map = new Dictionary<string, Motion>();
        foreach (var s in legacy.layers[0].stateMachine.states)
            map[s.state.name] = s.state.motion;
        return map;
    }

    private static AnimatorController BuildController()
    {
        var motions = LoadLegacyMotions();
        if (motions == null) return null;

        Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter(BossController.AnimParams.Moving, AnimatorControllerParameterType.Bool);
        controller.AddParameter(HadesBoss.HadesAnim.Revived, AnimatorControllerParameterType.Bool);
        string[] triggers =
        {
            BossController.AnimParams.Melee, BossController.AnimParams.Slam, BossController.AnimParams.Throw,
            BossController.AnimParams.Hit, BossController.AnimParams.Die, HadesBoss.HadesAnim.Spawn
        };
        foreach (string t in triggers)
            controller.AddParameter(t, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // 1형태(Wizard_*) / 2형태(Rades_*) 모션 세트. revived 불 값으로 구분
        BuildSet(sm, motions, revived: false, yOffset: 0f,
            idle: "Wizard_idle", walk: "Wizard_walk", melee: "Wizard_attack", melee2: "Wizard_attack",
            spawn: "Wizard_spawn", die: "Wizard_die");
        BuildSet(sm, motions, revived: true, yOffset: 500f,
            idle: "RHades_idle", walk: "Rades_walk", melee: "Rades_attack", melee2: "Rades_attack2",
            spawn: "Rades_Spawn", die: "Rades_die");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void BuildSet(AnimatorStateMachine sm, Dictionary<string, Motion> motions, bool revived, float yOffset,
                                 string idle, string walk, string melee, string melee2, string spawn, string die)
    {
        string prefix = revived ? "R_" : "";
        Motion M(string name)
        {
            if (!motions.TryGetValue(name, out Motion m) || m == null)
                Debug.LogWarning($"하데스 모션을 찾을 수 없습니다 : {name}");
            return m;
        }

        AnimatorState idleState = sm.AddState(prefix + "Idle", new Vector3(300, yOffset));
        idleState.motion = M(idle);

        // 등장 / 되살아남 : 모션 후 Idle. 1형태는 씬이 활성화되는 순간부터(기본 상태), 2형태는 spawn 트리거로 진입
        AnimatorState spawnState = sm.AddState(prefix + "Spawn", new Vector3(50, yOffset));
        spawnState.motion = M(spawn);
        var spawnToIdle = spawnState.AddTransition(idleState);
        spawnToIdle.hasExitTime = true;
        spawnToIdle.exitTime = 1f;
        spawnToIdle.duration = 0f;
        if (!revived)
            sm.defaultState = spawnState;
        else
            AddAnyTransition(sm, spawnState, HadesBoss.HadesAnim.Spawn, true);

        AnimatorState walkState = sm.AddState(prefix + "Walk", new Vector3(550, yOffset));
        walkState.motion = M(walk);
        var toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Moving);
        var toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, BossController.AnimParams.Moving);

        // 행동 모션 : Any State에서 트리거로 진입, 끝나면 Idle로 복귀
        AddAction(sm, idleState, prefix + "Melee", M(melee), BossController.AnimParams.Melee, revived, new Vector3(50, 150 + yOffset));
        AddAction(sm, idleState, prefix + "Melee2", M(melee2), BossController.AnimParams.Slam, revived, new Vector3(250, 150 + yOffset));
        AddAction(sm, idleState, prefix + "Cast", M(melee2), BossController.AnimParams.Throw, revived, new Vector3(450, 150 + yOffset));

        // 그로기(피격) : 제자리 대기 모션 (하데스는 피격 모션이 없어 몸 색 변화로 표현)
        AddAction(sm, idleState, prefix + "Hit", M(idle), BossController.AnimParams.Hit, revived, new Vector3(650, 150 + yOffset));

        AnimatorState dieState = sm.AddState(prefix + "Die", new Vector3(850, 150 + yOffset));
        dieState.motion = M(die);
        AddAnyTransition(sm, dieState, BossController.AnimParams.Die, revived);
    }

    private static void AddAction(AnimatorStateMachine sm, AnimatorState idle, string name, Motion motion,
                                  string trigger, bool revived, Vector3 position)
    {
        AnimatorState state = sm.AddState(name, position);
        state.motion = motion;
        AddAnyTransition(sm, state, trigger, revived);

        var exit = state.AddTransition(idle);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.duration = 0f;
    }

    private static void AddAnyTransition(AnimatorStateMachine sm, AnimatorState state, string trigger, bool revived)
    {
        var t = sm.AddAnyStateTransition(state);
        t.hasExitTime = false;
        t.duration = 0f;
        t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.AddCondition(revived ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, HadesBoss.HadesAnim.Revived);
    }
    #endregion
}
