using UnityEngine;

// 3스테이지 끝 : 출구 문 너머 트리거에 닿으면 성장 상태를 저장하고 엔딩 씬으로 이동 (원작 trigger.cs : Ending 씬 로드)
[RequireComponent(typeof(Collider2D))]
public class EndingTrigger : MonoBehaviour
{
    [SerializeField] private string endingSceneName = "Ending";
    [SerializeField] private float fadeDuration = 1.2f;

    private bool triggered;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead) return;

        triggered = true;
        player.InputLocked = true;
        player.SetVelocity(0f, player.Rb.velocity.y);
        PlayerSession.Save(player);
        Debug.Log($"엔딩 : {endingSceneName}(으)로 이동");
        ScreenFader.FadeAndLoad(endingSceneName, fadeDuration);
    }
}
