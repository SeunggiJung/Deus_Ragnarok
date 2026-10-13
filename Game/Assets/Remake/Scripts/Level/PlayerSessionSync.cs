using UnityEngine;

// 새 스테이지에 들어왔을 때 이전 스테이지의 레벨 / 스탯 / 무기를 이어받음
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(50)] // PlayerController / PlayerProgression 초기화 이후
public class PlayerSessionSync : MonoBehaviour
{
    private const string WeaponPath = "body/Weapon/Weapon-Sword";

    private void Start()
    {
        PlayerSession.Restore(GetComponent<PlayerController>());
    }

    public static SpriteRenderer GetWeaponRenderer(PlayerController player)
    {
        Transform weapon = player.transform.Find(WeaponPath);
        return weapon != null ? weapon.GetComponent<SpriteRenderer>() : null;
    }
}
