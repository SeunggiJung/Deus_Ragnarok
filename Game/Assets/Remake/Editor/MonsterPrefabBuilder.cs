using System;
using UnityEditor;
using UnityEngine;

// 씬에 배치돼 있던 레거시 몬스터(Golem, 마녀)를 Remake용 프리팹으로 변환.
// - 레거시 Enemy / EnemyAI 컴포넌트 제거 (꺼져 있어도 애니메이션 이벤트 Hit()는 호출되어 오류 원인이 됨)
// - Monster / RangedMonster, Stats 에셋, 머리 위 체력바, AttackCheck(공격/발사 위치), 레이어 설정
// 메뉴 : Tools/Deus/Build Monster Prefabs From Scene  (RemakeTest 씬에서 실행)
public static class MonsterPrefabBuilder
{
    private const string PrefabFolder = "Assets/Remake/Prefabs";
    private const string DataFolder = "Assets/Remake/Data";

    [MenuItem("Tools/Deus/Build Monster Prefabs From Scene")]
    public static void BuildAll()
    {
        EnemyStats golemStats = AssetDatabase.LoadAssetAtPath<EnemyStats>($"{DataFolder}/GolemStats.asset");
        RangedEnemyStats witchStats = LoadOrCreateWitchStats();

        Build("Golem", "Golem", typeof(Monster), golemStats);
        Build("Enemy1", "Witch", typeof(RangedMonster), witchStats);

        AssetDatabase.SaveAssets();
    }

    private static RangedEnemyStats LoadOrCreateWitchStats()
    {
        string path = $"{DataFolder}/WitchStats.asset";
        var stats = AssetDatabase.LoadAssetAtPath<RangedEnemyStats>(path);
        if (stats != null) return stats;

        stats = ScriptableObject.CreateInstance<RangedEnemyStats>();
        stats.maxHp = 40f;
        stats.invincibleTime = 0.3f;
        stats.attackDamage = 10f;          // 원작 Enemy 기본값
        stats.moveSpeed = 2f;              // 원작 Enemy 기본값
        stats.detectRange = 8f;
        stats.loseRange = 11f;
        stats.attackRange = 6f;            // 원거리라 이 거리에서 멈춰서 발사
        stats.attackCooldown = 2f;
        stats.expReward = 20f;
        stats.projectileSpeed = 7f;
        stats.projectileLifetime = 1.2f;   // 사거리 약 8.4
        AssetDatabase.CreateAsset(stats, path);
        return stats;
    }

    private static void Build(string sceneObjectName, string prefabName, Type monsterType, CharacterStats stats)
    {
        GameObject source = GameObject.Find(sceneObjectName);
        if (source == null)
        {
            Debug.LogWarning($"씬에서 {sceneObjectName}을(를) 찾을 수 없어 {prefabName} 프리팹 생성을 건너뜁니다.");
            return;
        }

        GameObject go = UnityEngine.Object.Instantiate(source);
        go.name = prefabName;

        // 레거시 AI 제거 + 다른 타입의 Monster 제거
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            string typeName = mb.GetType().Name;
            if (typeName == "Enemy" || typeName == "EnemyAI" || (mb is Monster && mb.GetType() != monsterType))
                UnityEngine.Object.DestroyImmediate(mb);
        }

        // Unity 오브젝트는 없을 때 가짜 null을 반환할 수 있어 ?? 연산자 대신 명시적으로 검사
        Component existing = go.GetComponent(monsterType);
        var monster = (Monster)(existing != null ? existing : go.AddComponent(monsterType));
        var so = new SerializedObject(monster);
        so.FindProperty("stats").objectReferenceValue = stats;
        so.FindProperty("whatIsEnemy").intValue = LayerMask.GetMask("Player");
        so.ApplyModifiedPropertiesWithoutUndo();

        if (go.GetComponent<WorldHealthBar>() == null)
            go.AddComponent<WorldHealthBar>();

        EnsureAttackCheck(go);

        go.layer = LayerMask.NameToLayer("Enemy");
        var rb = go.GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        go.transform.position = Vector3.zero;

        string path = $"{PrefabFolder}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        Debug.Log($"몬스터 프리팹 생성 : {path}");
    }

    // 콜라이더 앞쪽 끝, 몸 중앙 높이에 AttackCheck 생성 (근접 판정 중심 / 투사체 발사 위치)
    private static void EnsureAttackCheck(GameObject go)
    {
        if (go.transform.Find("AttackCheck") != null) return;

        var col = go.GetComponent<CapsuleCollider2D>();
        Vector2 local = col != null
            ? new Vector2(col.offset.x + col.size.x * 0.5f, col.offset.y)
            : Vector2.zero;

        var child = new GameObject("AttackCheck").transform;
        child.SetParent(go.transform, false);
        child.localPosition = local;
        child.gameObject.layer = LayerMask.NameToLayer("Enemy");
    }
}
