using UnityEngine;

// 새 스테이지에 들어왔을 때 이전 스테이지의 레벨 / 스탯 / 무기를 이어받음
[RequireComponent(typeof(PlayerController))]
[DefaultExecutionOrder(50)] // PlayerController / PlayerProgression 초기화 이후
public class PlayerSessionSync : MonoBehaviour
{
    private const string WeaponPath = "body/Weapon/Weapon-Sword";
    private const float WeaponWorldLength = 1.28f; // 기본 검의 월드 길이(스프라이트 높이 0.32 x 스케일 4) : 새 검도 이 길이에 맞춤

    private void Start()
    {
        PlayerSession.Restore(GetComponent<PlayerController>());
    }

    public static SpriteRenderer GetWeaponRenderer(PlayerController player)
    {
        Transform weapon = player.transform.Find(WeaponPath);
        return weapon != null ? weapon.GetComponent<SpriteRenderer>() : null;
    }

    // 손에 든 무기 교체. 획득한 검 스프라이트는 기본 검보다 훨씬 커서(2배) 몸보다 커 보이므로 기본 검과 같은 길이로 크기를 맞춤
    public static void EquipWeapon(SpriteRenderer weapon, Sprite sprite)
    {
        if (weapon == null || sprite == null) return;
        weapon.sprite = sprite;

        float length = sprite.bounds.size.y;
        Transform parent = weapon.transform.parent;
        float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.y) : 1f;
        if (length <= 0.0001f || parentScale <= 0.0001f) return;

        float scale = WeaponWorldLength / (length * parentScale);
        Vector3 local = weapon.transform.localScale;
        weapon.transform.localScale = new Vector3(Mathf.Sign(local.x) * scale, Mathf.Sign(local.y) * scale, local.z);
    }
}
