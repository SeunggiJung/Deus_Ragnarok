using UnityEngine;

// G키로 상호작용할 수 있는 대상 (포탈, 이후 문 / 레버 / NPC 등).
// 트리거 콜라이더 범위 안에 플레이어가 들어오면 PlayerInteractor가 안내 문구를 띄우고 G키 입력을 전달
public interface IInteractable
{
    string InteractionPrompt { get; }          // 예 : "Next Stage"
    Vector3 PromptPosition { get; }            // 안내 문구를 띄울 월드 위치
    bool CanInteract(PlayerController player);
    void Interact(PlayerController player);
}
