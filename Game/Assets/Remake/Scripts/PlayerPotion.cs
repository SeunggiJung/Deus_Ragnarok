using System;
using UnityEngine;

// 회복 포션. 사용하면 potionHealDuration초에 걸쳐 potionHealAmount만큼 나눠서 회복 (지속 회복), 쿨타임 potionCooldown초.
// 회복 중 피격되면 potionHitPauseTime초 동안 회복이 멈춤 (남은 회복량은 유지되어 이후 이어서 회복)
// 어떤 상태(공격, 구르기, 공중 등)에서든 사용 가능
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(100)] // PlayerController.Update에서 입력을 읽은 뒤 실행되도록
public class PlayerPotion : MonoBehaviour
{
    private PlayerController player;
    private float lastUseTime = float.NegativeInfinity;
    private float healRemaining;   // 아직 회복하지 않은 양
    private float healPerSecond;
    private float healPausedUntil = float.NegativeInfinity;

    public float Cooldown => player.Stats.potionCooldown;
    public float CooldownRemaining => Mathf.Max(0f, lastUseTime + Cooldown - Time.time);
    public bool IsReady => CooldownRemaining <= 0f;
    public bool IsHealing => healRemaining > 0f;
    public bool IsHealPaused => IsHealing && Time.time < healPausedUntil;

    public event Action OnUsed;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnEnable() => player.DamageTaken += HandleDamageTaken;
    private void OnDisable() => player.DamageTaken -= HandleDamageTaken;

    private void HandleDamageTaken(float damage)
    {
        if (IsHealing)
            healPausedUntil = Time.time + player.Stats.potionHitPauseTime;
    }

    private void Update()
    {
        if (player.IsDead)
        {
            healRemaining = 0f;
            return;
        }

        if (player.Input.PotionPressed)
            TryUse();

        if (healRemaining > 0f && !IsHealPaused)
        {
            float amount = Mathf.Min(healRemaining, healPerSecond * Time.deltaTime);
            healRemaining -= amount;
            player.Heal(amount);
        }
    }

    public bool TryUse()
    {
        if (!IsReady || player.IsDead) return false;

        PlayerStats s = player.Stats;
        lastUseTime = Time.time;
        healRemaining = s.potionHealAmount;
        healPerSecond = s.potionHealAmount / Mathf.Max(0.01f, s.potionHealDuration);
        healPausedUntil = float.NegativeInfinity;
        OnUsed?.Invoke();
        Debug.Log($"포션 사용 : {s.potionHealDuration}초에 걸쳐 체력 {s.potionHealAmount} 회복");
        return true;
    }
}
