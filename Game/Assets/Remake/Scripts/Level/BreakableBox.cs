using UnityEngine;

// 공격으로 부술 수 있는 상자. 부서지면 조각이 흩어지고 안에서 레벨업 아이템이 튀어나옴
// (원작 : reward 태그 상자에 닿으면 레벨업 선택창 → 공격해서 부수고 아이템을 줍는 방식으로 변경)
public class BreakableBox : MonoBehaviour, IDamageable
{
    [SerializeField] private Sprite[] breakPieces;        // 부서질 때 흩어질 조각 (Pixel Adventure Box3 Break 시트)
    [SerializeField] private float debrisForce = 5f;
    [SerializeField] private float debrisLifetime = 1.2f;
    [SerializeField] private bool dropLevelUpItem = true;

    private bool broken;

    public void TakeDamage(float damage)
    {
        if (broken) return;
        broken = true;

        var sr = GetComponent<SpriteRenderer>();
        Vector3 scale = transform.lossyScale;
        Vector2 center = sr != null ? (Vector2)sr.bounds.center : (Vector2)transform.position;

        SpriteDebris.Spawn(breakPieces, center, scale, sr != null ? sr.sortingOrder : 0, debrisForce, debrisLifetime);

        if (dropLevelUpItem)
            LevelUpPickup.Spawn(center);

        Debug.Log("상자 파괴 : 레벨업 아이템 드롭");
        Destroy(gameObject);
    }
}
