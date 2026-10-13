﻿using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 2스테이지 Crab을 일반 몬스터(Monster FSM)에서 Behavior Tree 보스(CrabBoss)로 변환하는 도구.
// - CrabBoss.controller : 에셋 팩의 Crab 모션을 트리거로 재생 (집게 연타 / 바닥 찍기 / 잡기 / 능력 / 피격 / 사망)
// - CrabBossStats 에셋 : 거리 / 수치는 Crab 스케일(x18) 기준
// - 씬의 Crab : Monster / WorldHealthBar 제거 → CrabBoss 추가, 보스방 기둥 / 문은 이 보스를 구독
// 메뉴 : Tools/Deus/Build Crab Boss (Stage2 씬에서 실행. Stage2Builder.Build에도 포함되어 여러 번 실행해도 결과가 같음)
public static class CrabBossBuilder
{
    private const string AnimFolder = "Assets/External/Enemy Galore 1 - Pixel Art/Animations";
    private const string ControllerPath = "Assets/Remake/Animations/Boss/CrabBoss.controller";
    private const string StatsPath = "Assets/Remake/Data/CrabBossStats.asset";
    private const string SceneName = "Crab";

    [MenuItem("Tools/Deus/Build Crab Boss")]
    public static void Build()
    {
        CrabBoss boss = Convert();
        if (boss == null) return;

        Stage2Builder.SetupArenaGates(boss);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    public static CrabBoss Convert()
    {
        GameObject go = GameObject.Find(SceneName);
        if (go == null)
        {
            Debug.LogError($"씬에서 {SceneName}을(를) 찾을 수 없습니다. Stage2 씬에서 실행하세요.");
            return null;
        }

        AnimatorController controller = BuildController();
        CrabBossStats stats = LoadOrCreateStats();

        // WorldHealthBar가 Monster에 의존하므로 먼저 제거
        foreach (string typeName in new[] { "WorldHealthBar", "Monster", "Enemy", "EnemyAI" })
        {
            foreach (var mb in go.GetComponents<MonoBehaviour>())
            {
                if (mb != null && mb.GetType().Name == typeName)
                    Object.DestroyImmediate(mb);
            }
        }

        var boss = go.GetComponent<CrabBoss>();
        if (boss == null) boss = go.AddComponent<CrabBoss>();

        Transform attackCheck = go.transform.Find("AttackCheck");
        var so = new SerializedObject(boss);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("whatIsEnemy").intValue = LayerMask.GetMask("Player");
        so.FindProperty("attackCheck").objectReferenceValue = attackCheck;
        so.ApplyModifiedProperties();

        go.GetComponent<Animator>().runtimeAnimatorController = controller;

        if (attackCheck == null)
        {
            var col = go.GetComponent<CapsuleCollider2D>();
            var child = new GameObject("AttackCheck").transform;
            child.SetParent(go.transform, false);
            child.localPosition = col != null ? new Vector2(col.offset.x + col.size.x * 0.5f, col.offset.y) : Vector2.zero;
            child.gameObject.layer = LayerMask.NameToLayer("Enemy");
            so.FindProperty("attackCheck").objectReferenceValue = child;
            so.ApplyModifiedProperties();
        }
        go.layer = LayerMask.NameToLayer("Enemy");
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;

        EditorUtility.SetDirty(go);
        Debug.Log("Crab 보스 변환 완료 : CrabBoss + " + ControllerPath);
        return boss;
    }

    private static AnimationClip Clip(string name) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>($"{AnimFolder}/Crab_{name}.anim");

    private static AnimatorController BuildController()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ControllerPath));
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        controller.AddParameter(BossController.AnimParams.Moving, AnimatorControllerParameterType.Bool);
        string[] triggers =
        {
            BossController.AnimParams.Melee, BossController.AnimParams.Slam, BossController.AnimParams.Grab,
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
        AddAction(sm, idle, "Melee", Clip("Attack_C"), BossController.AnimParams.Melee, new Vector3(50, 150));   // 집게 연타 (Hit 이벤트)
        AddAction(sm, idle, "Slam", Clip("Attack_A"), BossController.AnimParams.Slam, new Vector3(250, 150));    // 바닥 찍기
        AddAction(sm, idle, "Grab", Clip("Attack_B"), BossController.AnimParams.Grab, new Vector3(450, 150));    // 잡기 돌진
        AddAction(sm, idle, "Ability", Clip("Ability"), BossController.AnimParams.Ability, new Vector3(650, 150)); // 방어 / 포효
        AddAction(sm, idle, "Hit", Clip("Hit"), BossController.AnimParams.Hit, new Vector3(50, 250));

        AnimatorState die = sm.AddState("Die", new Vector3(250, 250));
        die.motion = Clip("Death");
        var toDie = sm.AddAnyStateTransition(die);
        toDie.hasExitTime = false;
        toDie.duration = 0f;
        toDie.canTransitionToSelf = false;
        toDie.AddCondition(AnimatorConditionMode.If, 0, BossController.AnimParams.Die);

        EditorUtility.SetDirty(controller);
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

    // 에셋이 없을 때만 생성 (이후 수치 조정은 에셋에서)
    private static CrabBossStats LoadOrCreateStats()
    {
        var stats = AssetDatabase.LoadAssetAtPath<CrabBossStats>(StatsPath);
        if (stats != null) return stats;

        stats = ScriptableObject.CreateInstance<CrabBossStats>();
        stats.bossName = "Giant Crab";
        stats.expReward = 400f;

        // 기본 (원작 Crab : 체력 600 → Remake 공격력 기준으로 조정)
        stats.maxHp = 500f;
        stats.invincibleTime = 0.15f;
        stats.attackDamage = 16f;
        stats.attackRadius = 2.2f;

        stats.moveSpeed = 2.2f;
        stats.baseAnimSpeed = 1f;
        stats.detectRange = 22f;                // 보스방 기둥을 지나 안쪽으로 들어서면 전투 시작
        stats.attackRecovery = 0.9f;

        stats.meleeRange = 4.5f;
        stats.meleeCooldown = 1.6f;

        stats.chargeMinRange = 6f;
        stats.chargeMaxRange = 16f;
        stats.chargeCooldown = 8f;
        stats.chargeTelegraph = 0.8f;
        stats.chargeSpeed = 10f;
        stats.chargeDistance = 12f;
        stats.chargeDamage = 20f;

        stats.guardCooldown = 12f;
        stats.maxPoise = 120f;
        stats.staggerTime = 1.5f;

        // 사망 연출 : 커진 몸집에 맞춰 조각 수 / 힘 증가
        stats.shatterForce = 8f;

        AssetDatabase.CreateAsset(stats, StatsPath);
        AssetDatabase.SaveAssets();
        return stats;
    }
}
