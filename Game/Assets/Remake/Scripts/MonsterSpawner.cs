using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

// 스폰 포인트. 몬스터 프리팹을 오브젝트 풀로 관리하며, 죽은 몬스터는 파괴하지 않고 풀로 돌려받아
// respawnDelay 뒤 다시 같은 자리에서 꺼내 씀 (몬스터 정보는 프리팹 + Stats 에셋에만 존재 → 재입력 불필요)
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private Monster prefab;
    [SerializeField] private int maxAlive = 1;          // 이 스폰 포인트에서 동시에 살아있을 수 있는 수
    [SerializeField] private float respawnDelay = 8f;   // 사망 후 다시 나타나기까지 시간
    [SerializeField] private float spawnRadius = 0f;    // 0이면 정확히 이 위치, 아니면 좌우로 랜덤 분산
    [SerializeField] private bool spawnOnStart = true;

    private ObjectPool<Monster> pool;
    private int aliveCount;

    private void Awake()
    {
        pool = new ObjectPool<Monster>(
            createFunc: () => Instantiate(prefab, transform.position, prefab.transform.rotation, transform),
            actionOnGet: m => m.gameObject.SetActive(true),
            actionOnRelease: m => m.gameObject.SetActive(false),
            actionOnDestroy: m => Destroy(m.gameObject),
            collectionCheck: false,
            defaultCapacity: maxAlive,
            maxSize: Mathf.Max(maxAlive, 1) * 2);
    }

    private void Start()
    {
        if (!spawnOnStart) return;
        for (int i = 0; i < maxAlive; i++)
            SpawnOne();
    }

    private void SpawnOne()
    {
        if (prefab == null || aliveCount >= maxAlive) return;

        Vector3 pos = transform.position;
        if (spawnRadius > 0f)
            pos.x += Random.Range(-spawnRadius, spawnRadius);

        Monster monster = pool.Get();
        monster.Spawn(pos, HandleMonsterReleased);
        aliveCount++;
    }

    private void HandleMonsterReleased(Monster monster)
    {
        pool.Release(monster);
        aliveCount--;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnOne();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Max(spawnRadius * 2f, 0.5f), 0.5f, 0f));
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
    }
}
