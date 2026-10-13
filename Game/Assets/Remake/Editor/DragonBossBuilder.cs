using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 2스테이지 드래곤을 일반 몬스터(Monster FSM)에서 Behavior Tree 보스(DragonBoss)로 변환하는 도구.
// - 에셋 팩의 프레임(Dragon Warrior PNG)으로 필요한 모션(피격 / 어지러움 / 기 모으기 / 브레스 / 덮치기 / 들어올리기)을 클립으로 생성
// - DragonBoss.controller : 트리거로 모션 재생, 그로기 동안 어지러운 모션 유지
// - DragonBossStats 에셋 : 체력 900. 스프라이트(불줄기 / 화염구 / 폭발)도 연결
// - 씬의 Dragon : Monster / WorldHealthBar 제거 → DragonBoss 추가, 보스 체력바 연결
// 메뉴 : Tools/Deus/Build Dragon Boss (Stage2 씬에서 실행. Stage2Builder.Build에도 포함되어 여러 번 실행해도 결과가 같음)
public static class DragonBossBuilder
{
    private const string PngFolder = "Assets/External/Dragon Warrior Files/Dragon Warrior PNG";
    private const string FxFolder = "Assets/External/Dragon Warrior Files/Effects";
    private const string ExistingClipFolder = "Assets/External/animations";
    private const string ClipFolder = "Assets/Remake/Animations/Boss/Dragon";
    private const string ControllerPath = "Assets/Remake/Animations/Boss/DragonBoss.controller";
    private const string StatsPath = "Assets/Remake/Data/DragonBossStats.asset";
    private const string SceneName = "Dragon";

    [MenuItem("Tools/Deus/Build Dragon Boss")]
    public static void Build()
    {
        if (Convert() == null) return;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    public static DragonBoss Convert()
    {
        GameObject go = GameObject.Find(SceneName);
        if (go == null)
        {
            // 비활성 상태면 Find가 못 찾음
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.name == SceneName && t.gameObject.scene.IsValid()) { go = t.gameObject; break; }
        }
        if (go == null)
        {
            Debug.LogError($"씬에서 {SceneName}을(를) 찾을 수 없습니다. Stage2 씬에서 실행하세요.");
            return null;
        }

        AnimatorController controller = BuildController();
        DragonBossStats stats = LoadOrCreateStats();

        // WorldHealthBar가 Monster에 의존하므로 먼저 제거
        foreach (string typeName in new[] { "WorldHealthBar", "Monster", "Enemy", "EnemyAI" })
        {
            foreach (var mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == typeName)
                    Object.DestroyImmediate(mb);
            }
        }

        var boss = go.GetComponent<DragonBoss>();
        if (boss == null) boss = go.AddComponent<DragonBoss>();

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

        var animator = go.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;

        go.layer = LayerMask.NameToLayer("Enemy");
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        SetupHealthBars(boss);

        EditorUtility.SetDirty(go);
        Debug.Log("Dragon 보스 변환 완료 : DragonBoss + " + ControllerPath);
        return boss;
    }

    // 보스 체력바는 보스마다 하나씩 : HUD의 기존 바는 Crab에 고정하고, 드래곤용 바를 따로 만든다
    private static void SetupHealthBars(DragonBoss dragon)
    {
        CrabBoss crab = Object.FindObjectOfType<CrabBoss>();
        BossHealthBar dragonBar = null;
        foreach (var bar in Resources.FindObjectsOfTypeAll<BossHealthBar>())
        {
            if (!bar.gameObject.scene.IsValid()) continue;
            if (bar.gameObject.name == "DragonHealthBar") { dragonBar = bar; continue; }

            var so = new SerializedObject(bar);
            so.FindProperty("boss").objectReferenceValue = crab;
            so.ApplyModifiedProperties();
        }

        if (dragonBar == null)
        {
            var barGo = new GameObject("DragonHealthBar");
            dragonBar = barGo.AddComponent<BossHealthBar>();
        }
        var dso = new SerializedObject(dragonBar);
        dso.FindProperty("boss").objectReferenceValue = dragon;
        dso.ApplyModifiedProperties();
    }

    #region Stats
    // 에셋이 없을 때만 수치를 채움 (이후 수치 조정은 에셋에서). 스프라이트 연결은 매번 갱신
    private static DragonBossStats LoadOrCreateStats()
    {
        var stats = AssetDatabase.LoadAssetAtPath<DragonBossStats>(StatsPath);
        bool created = stats == null;
        if (created)
        {
            stats = ScriptableObject.CreateInstance<DragonBossStats>();
            stats.bossName = "Dragon";
            stats.expReward = 600f;

            stats.maxHp = 900f;                 // Crab(500)보다 높게
            stats.invincibleTime = 0.15f;
            stats.attackDamage = 24f;
            stats.attackRadius = 2.4f;

            stats.moveSpeed = 2.6f;
            stats.baseAnimSpeed = 1f;
            stats.detectRange = 40f;            // 등장 연출이 끝나면 바로 전투 시작 (DragonBoss.spawnDelay)
            stats.attackRecovery = 0.9f;

            stats.meleeRange = 3.4f;
            stats.meleeCooldown = 1.8f;

            stats.guardCooldown = 12f;
            stats.maxPoise = 150f;
            stats.staggerTime = 1.5f;
            stats.shatterForce = 8f;

            AssetDatabase.CreateAsset(stats, StatsPath);
        }

        stats.breathFrames = LoadSprites(FxFolder, "iblast_0", 1, 3);
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
    private static AnimationClip Existing(string name) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ExistingClipFolder}/Dragon_{name}.anim");

    // PNG 프레임으로 클립 생성. frames는 파일 이름(확장자 제외). loop가 아니면 마지막 프레임을 total까지 유지,
    // cycle이면 total까지 프레임을 반복 (끝나면 Idle로 돌아가는 비반복 모션을 길게 만들 때)
    private static AnimationClip MakeClip(string name, string[] frames, float fps, bool loop, float total = 0f, bool cycle = false)
    {
        Directory.CreateDirectory(ClipFolder);
        string path = $"{ClipFolder}/Dragon_{name}.anim";
        AssetDatabase.DeleteAsset(path);

        var sprites = new List<Sprite>();
        foreach (string f in frames)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{PngFolder}/{f}.png");
            if (s == null) Debug.LogWarning($"드래곤 프레임을 찾을 수 없습니다 : {f}");
            else sprites.Add(s);
        }

        var clip = new AnimationClip { frameRate = fps };
        float natural = sprites.Count / fps;
        float length = loop ? natural : Mathf.Max(natural, total);

        var keys = new List<ObjectReferenceKeyframe>();
        if (loop || cycle)
        {
            int count = Mathf.CeilToInt(length * fps);
            for (int i = 0; i < count; i++)
                keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = sprites[i % sprites.Count] });
        }
        else
        {
            for (int i = 0; i < sprites.Count; i++)
                keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] });
        }
        if (!loop)   // 마지막 프레임을 length까지 유지
            keys.Add(new ObjectReferenceKeyframe { time = length, value = keys[keys.Count - 1].value });

        AnimationUtility.SetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorController BuildController()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));

        // 클립은 컨트롤러를 지우기 전에 다시 만들어 연결이 끊기지 않게 함
        AnimationClip hurt = MakeClip("Hurt", new[] { "hurt_01", "hurt_02" }, 10f, false, 0.3f);
        AnimationClip dizzy = MakeClip("Dizzy", new[] { "dizzy_01", "dizzy_02", "dizzy_03" }, 8f, true);
        AnimationClip charge = MakeClip("Charge", new[] { "crouch_01", "crouch_02" }, 6f, false, 2.6f, cycle: true);
        AnimationClip breath = MakeClip("Breath", new[] { "crouch_ATK_01", "crouch_ATK_02", "crouch_ATK_03" }, 12f, false, 1.0f);
        AnimationClip lunge = MakeClip("Lunge", new[] { "flyKick_01", "flyKick_02", "flyKick_03", "flyKick_04", "flyKick_05", "flyKick_06" }, 16f, false, 0.6f);
        AnimationClip lift = MakeClip("Lift", new[] { "jump_01", "jump_02" }, 8f, false, 1.2f);

        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter(BossController.AnimParams.Moving, AnimatorControllerParameterType.Bool);
        controller.AddParameter(DragonBoss.DragonAnim.Staggered, AnimatorControllerParameterType.Bool);
        string[] triggers =
        {
            BossController.AnimParams.Melee, BossController.AnimParams.Throw, BossController.AnimParams.Grab,
            BossController.AnimParams.Ability, BossController.AnimParams.Hit, BossController.AnimParams.Die,
            DragonBoss.DragonAnim.Lift
        };
        foreach (string t in triggers)
            controller.AddParameter(t, AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idle = sm.AddState("Idle", new Vector3(300, 0));
        idle.motion = Existing("Idle");

        // 등장 모션 후 Idle (EncounterTrigger가 활성화하는 순간 재생)
        AnimatorState spawn = sm.AddState("Spawn", new Vector3(50, 0));
        spawn.motion = Existing("Spawn");
        sm.defaultState = spawn;
        var spawnToIdle = spawn.AddTransition(idle);
        spawnToIdle.hasExitTime = true;
        spawnToIdle.exitTime = 1f;
        spawnToIdle.duration = 0f;

        AnimatorState walk = sm.AddState("Walk", new Vector3(550, 0));
        walk.motion = Existing("walk");
        var toWalk = idle.AddTransition(walk);
        toWalk.hasExitTime = false;
        toWalk.duration = 0f;
        toWalk.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Moving);
        var toIdle = walk.AddTransition(idle);
        toIdle.hasExitTime = false;
        toIdle.duration = 0f;
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, BossController.AnimParams.Moving);

        // 행동 모션 : Any State에서 트리거로 진입, 끝나면 Idle로 복귀
        AddAction(sm, idle, "Melee", Existing("attack"), BossController.AnimParams.Melee, new Vector3(50, 150));      // 발톱 (Hit 이벤트)
        AddAction(sm, idle, "Breath", breath, BossController.AnimParams.Throw, new Vector3(250, 150));                 // 불 뿜기
        AddAction(sm, idle, "Lunge", lunge, BossController.AnimParams.Grab, new Vector3(450, 150));                    // 덮치기
        AddAction(sm, idle, "Charge", charge, BossController.AnimParams.Ability, new Vector3(650, 150));               // 기 모으기 / 포효 / 방어
        AddAction(sm, idle, "Lift", lift, DragonBoss.DragonAnim.Lift, new Vector3(850, 150));                          // 붙잡고 들어올리기

        // 피격(그로기 시작) → 그로기가 이어지는 동안 어지러운 모션
        AnimatorState hit = sm.AddState("Hit", new Vector3(50, 250));
        hit.motion = hurt;
        var anyToHit = sm.AddAnyStateTransition(hit);
        anyToHit.hasExitTime = false;
        anyToHit.duration = 0f;
        anyToHit.canTransitionToSelf = false;
        anyToHit.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Hit);

        AnimatorState dizzyState = sm.AddState("Dizzy", new Vector3(250, 250));
        dizzyState.motion = dizzy;
        var hitToDizzy = hit.AddTransition(dizzyState);
        hitToDizzy.hasExitTime = true;
        hitToDizzy.exitTime = 1f;
        hitToDizzy.duration = 0f;
        hitToDizzy.AddCondition(AnimatorConditionMode.If, 0, DragonBoss.DragonAnim.Staggered);
        var hitToIdle = hit.AddTransition(idle);
        hitToIdle.hasExitTime = true;
        hitToIdle.exitTime = 1f;
        hitToIdle.duration = 0f;
        hitToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, DragonBoss.DragonAnim.Staggered);
        var dizzyToIdle = dizzyState.AddTransition(idle);
        dizzyToIdle.hasExitTime = false;
        dizzyToIdle.duration = 0f;
        dizzyToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, DragonBoss.DragonAnim.Staggered);

        AnimatorState die = sm.AddState("Die", new Vector3(450, 250));
        die.motion = Existing("die");
        var toDie = sm.AddAnyStateTransition(die);
        toDie.hasExitTime = false;
        toDie.duration = 0f;
        toDie.canTransitionToSelf = false;
        toDie.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Die);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
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
    #endregion
}
