using UnityEngine;
using UnityEngine.Pool;

// 투사체 오브젝트 풀. 씬에 배치하지 않아도 처음 사용할 때 자동 생성
public class ProjectilePool : MonoBehaviour
{
    private static ProjectilePool instance;
    public static ProjectilePool Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("ProjectilePool").AddComponent<ProjectilePool>();
            return instance;
        }
    }

    private ObjectPool<Projectile> pool;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        pool = new ObjectPool<Projectile>(
            createFunc: () =>
            {
                var go = new GameObject("Projectile");
                go.transform.SetParent(transform, false);
                return go.AddComponent<Projectile>();
            },
            actionOnGet: p => p.gameObject.SetActive(true),
            actionOnRelease: p => p.gameObject.SetActive(false),
            actionOnDestroy: p => Destroy(p.gameObject),
            collectionCheck: false,
            defaultCapacity: 16,
            maxSize: 128);
    }

    public Projectile Get() => pool.Get();
    public void Release(Projectile projectile) => pool.Release(projectile);

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
