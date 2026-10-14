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
        ComputeForm2Hitbox(go, stats);
        EditorUtility.SetDirty(stats);
        AssetDatabase.SaveAssets();

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
            AssetDatabase.CreateAsset(stats, StatsPath);
        }

        // 수치는 에셋에 저장된 값이 기준 : 빌더의 TuningVersion이 올라갔을 때만 한 번 덮어씀 (이후 에셋에서 직접 조정)
        if (stats.tuningVersion < TuningVersion)
        {
            ApplyTuning(stats);
            stats.tuningVersion = TuningVersion;
        }

        stats.fireballFrames = LoadSprites(FxFolder, "fireball_0", 1, 3);
        stats.explosionFrames = LoadSprites(FxFolder, "explosion_0", 1, 6);
        EditorUtility.SetDirty(stats);
        AssetDatabase.SaveAssets();
        return stats;
    }

    // v1 : 최초 수치 / v2 : 패턴 6종으로 확장 + 공격 속도 / 이동 속도 / 쿨다운 단축 (쉽고 단조롭다는 피드백)
    private const int TuningVersion = 2;

    private static void ApplyTuning(HadesBossStats s)
    {
        s.bossName = "Hades";
        s.expReward = 400f;                 // 1형태 / 2형태 처치마다 지급

        s.maxHp = 1000f;                    // 원작 1000
        s.form2MaxHp = 1400f;
        s.invincibleTime = 0.12f;
        s.attackDamage = 30f;               // 원작 30
        s.form2AttackDamage = 38f;
        s.attackRadius = 3f;

        s.moveSpeed = 3.2f;                 // 원작 2.0 (더 압박감 있게)
        s.baseAnimSpeed = 1.3f;             // 모션이 빨라져 공격 템포가 올라감
        s.phase2AnimSpeed = 1.55f;
        s.phase2CooldownMultiplier = 0.65f;
        s.phase2MoveMultiplier = 1.25f;
        s.detectRange = 60f;                // 등장 연출이 끝나면 바로 전투 시작 (HadesBoss.spawnDelay)
        s.attackRecovery = 0.4f;            // 패턴 사이 숨 고르기 (기존 0.9)

        s.meleeRange = 6f;                  // 원작 공격 범위 6
        s.meleeCooldown = 0.9f;
        s.meleeDuration = 1.2f;

        s.guardCooldown = 999f;             // 방어 자세는 사용하지 않음
        s.maxPoise = 260f;
        s.staggerTime = 1.0f;
        s.shatterForce = 9f;
        s.phase2Threshold = 0f;             // 2페이즈는 체력이 아니라 부활(Revive)로 진입

        // 화염탄 비
        s.bombCooldown = 8f;
        s.bombCount = 4;
        s.form2BombCount = 6;
        s.bombInterval = 0.35f;
        s.bombWarnTime = 0.95f;

        // 돌진 (공통 노드)
        s.chargeMinRange = 6f;
        s.chargeMaxRange = 18f;
        s.chargeCooldown = 8f;
        s.chargeTelegraph = 0.55f;
        s.chargeSpeed = 13f;
        s.chargeDistance = 14f;
        s.chargeDamage = 26f;
        s.chargeTelegraphTint = new Color(1f, 0.5f, 0.9f);

        // 암흑탄 / 지면 폭발 / 순간이동 강타는 HadesBossStats 기본값 사용
        s.boltCooldown = 5f;
        s.eruptionCooldown = 7f;
        s.teleportCooldown = 9f;
    }

    #region Form 2 Hitbox
    // 2형태 스프라이트(Evil Wizard 2)는 1형태와 크기 / 발 위치가 달라서, 콜라이더를 그대로 두면 몸이 바닥 아래로 내려가 보임.
    // 두 형태 첫 프레임에서 "보이는 픽셀 영역"을 측정해 1형태 콜라이더와 같은 비율로 2형태 콜라이더를 계산해 에셋에 저장
    private static void ComputeForm2Hitbox(GameObject go, HadesBossStats stats)
    {
        var capsule = go.GetComponent<CapsuleCollider2D>();
        var motions = LoadLegacyMotions();
        if (capsule == null || motions == null) return;
        if (!motions.TryGetValue("Wizard_idle", out Motion m1) || !motions.TryGetValue("RHades_idle", out Motion m2)) return;

        if (!MeasureVisible(m1, out Rect art1) || !MeasureVisible(m2, out Rect art2)) return;

        float r = art2.height / art1.height;                   // 1형태 대비 2형태가 보이는 크기 비율
        Vector2 offset1 = capsule.offset, size1 = capsule.size;
        Vector2 d = offset1 - art1.center;                     // 1형태 : 보이는 영역 중심 → 콜라이더 중심

        stats.form2ColliderSize = size1 * r;
        stats.form2ColliderOffset = art2.center + d * r;
        stats.hasForm2Collider = true;
        Debug.Log($"Hades 2형태 콜라이더 계산 : 보이는 영역 1형태 {art1} / 2형태 {art2} (비율 {r:0.00}) → offset {stats.form2ColliderOffset}, size {stats.form2ColliderSize}");
    }

    // 모션 첫 프레임 스프라이트에서 투명하지 않은 픽셀 영역을 스프라이트 피벗 기준 로컬 단위로 반환
    private static bool MeasureVisible(Motion motion, out Rect area)
    {
        area = default;
        var clip = motion as AnimationClip;
        if (clip == null) return false;

        Sprite sprite = null;
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys.Length > 0) { sprite = keys[0].value as Sprite; break; }
        }
        if (sprite == null) return false;

        // 읽기 불가 텍스처도 읽을 수 있게 RenderTexture로 복사해서 픽셀 확인
        Texture2D tex = sprite.texture;
        RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(tex, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        readable.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Rect rect = sprite.rect;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        for (int y = (int)rect.yMin; y < (int)rect.yMax; y++)
        {
            for (int x = (int)rect.xMin; x < (int)rect.xMax; x++)
            {
                if (readable.GetPixel(x, y).a < 0.2f) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        }
        Object.DestroyImmediate(readable);
        if (maxX < minX) return false;

        float ppu = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;   // 스프라이트 사각형 기준 픽셀 좌표
        float x0 = (minX - rect.x - pivot.x) / ppu;
        float x1 = (maxX + 1 - rect.x - pivot.x) / ppu;
        float y0 = (minY - rect.y - pivot.y) / ppu;
        float y1 = (maxY + 1 - rect.y - pivot.y) / ppu;
        area = Rect.MinMaxRect(x0, y0, x1, y1);
        return true;
    }
    #endregion

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
