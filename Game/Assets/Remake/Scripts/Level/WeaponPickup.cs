using UnityEngine;

// 바닥에 놓인 무기. 플레이어가 닿으면 공격력이 오르고 손에 든 무기 스프라이트가 이 무기로 교체됨
// (원작 Sword_Man의 key 태그 획득 → newweapon 교체 방식 계승)
[RequireComponent(typeof(Collider2D))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private float attackBonus = 3f;
    [SerializeField] private string weaponRendererPath = "body/Weapon/Weapon-Sword"; // 플레이어 기준 무기 스프라이트 위치
    [SerializeField] private float bobHeight = 0.15f;  // 둥실거리는 연출
    [SerializeField] private float bobSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private Vector3 basePosition;
    private bool collected;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;

        // 레거시 key 오브젝트는 중력이 있는 Rigidbody라 고정
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }
        basePosition = transform.position;
    }

    private void Update()
    {
        if (collected) return;
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead) return;

        collected = true;
        player.AddAttack(attackBonus);

        Transform weapon = player.transform.Find(weaponRendererPath);
        if (weapon != null && spriteRenderer != null)
            PlayerSessionSync.EquipWeapon(weapon.GetComponent<SpriteRenderer>(), spriteRenderer.sprite); // 몸 크기에 맞게 축소

        Debug.Log($"무기 획득 : 공격력 +{attackBonus} (현재 {player.AttackDamage})");
        gameObject.SetActive(false);
    }
}
