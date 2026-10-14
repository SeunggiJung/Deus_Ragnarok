using UnityEngine;

// 하데스의 방 레버. 1형태를 쓰러뜨리면 나타나 2형태를 깨우고, 2형태를 쓰러뜨린 뒤에는 출구 문을 엶
// (원작 : 레버 G키 → 카메라 연출 후 부활 / 최종 처치 후 문 상승). 흐름은 HadesEncounter가 관리
[RequireComponent(typeof(Collider2D))]
public class HadesLever : MonoBehaviour, IInteractable
{
    [SerializeField] private HadesEncounter encounter;
    [SerializeField] private Vector2 promptOffset = new Vector2(0f, 2.2f);

    public string InteractionPrompt => encounter != null ? encounter.LeverPrompt : "Lever";
    public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    public bool CanInteract(PlayerController player) => encounter != null && encounter.CanUseLever(player);

    public void Interact(PlayerController player)
    {
        if (encounter != null) encounter.UseLever(player);
    }
}
