using UnityEngine;

// 테스트용 치트. 에디터 / 개발 빌드에서만 동작 (정식 빌드에서는 아무것도 하지 않음)
// F1 : 공격력 +50, 최대 체력 +100, 최대 스태미너 +100 (늘어난 만큼 현재 값도 회복)
// F2 : 부활 기회 +1
[RequireComponent(typeof(PlayerController))]
public class DebugCheats : MonoBehaviour
{
    [SerializeField] private KeyCode buffKey = KeyCode.F1;
    [SerializeField] private KeyCode reviveKey = KeyCode.F2;
    [SerializeField] private float attackBonus = 50f;
    [SerializeField] private float hpBonus = 100f;
    [SerializeField] private float staminaBonus = 100f;

    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Input.GetKeyDown(reviveKey))
        {
            player.AddReviveChance(1);   // 사망 중에도 사용 가능 (사망 화면의 타이머 중 확인용)
            Debug.Log($"[Cheat] 부활 기회 {player.ReviveCount}");
        }

        if (player.IsDead || !Input.GetKeyDown(buffKey)) return;

        player.AddAttack(attackBonus);
        player.AddMaxHp(hpBonus);
        player.AddMaxStamina(staminaBonus);
        Debug.Log($"[Cheat] 공격력 {player.AttackDamage}, 최대 체력 {player.MaxHp}, 최대 스태미너 {player.MaxStamina}");
    }
#endif
}
