using System.Collections;
using UnityEngine;

// 밟으면 잠깐 흔들린 뒤 떨어지는 발판. 일정 시간 뒤 원래 자리에 다시 나타남
[RequireComponent(typeof(Rigidbody2D))]
public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float shakeTime = 0.5f;     // 밟은 뒤 떨어지기까지 (흔들림 예고)
    [SerializeField] private float shakeAmount = 0.06f;
    [SerializeField] private float fallGravity = 3f;
    [SerializeField] private float respawnDelay = 3f;    // 떨어지기 시작한 뒤 복구까지

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private Renderer[] renderers;
    private Vector2 startPosition;
    private bool triggered;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();
        renderers = GetComponentsInChildren<Renderer>();
        startPosition = rb.position;
        ResetPlatform();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;

        PlayerController player = collision.collider.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // 옆이나 아래에서 부딪힌 경우는 무시, 위에 올라섰을 때만
        if (player.BodyCollider.bounds.min.y < platformCollider.bounds.max.y - 0.15f) return;

        StartCoroutine(FallRoutine());
    }

    private IEnumerator FallRoutine()
    {
        triggered = true;

        // 예고 : 제자리에서 좌우로 떨림
        float elapsed = 0f;
        while (elapsed < shakeTime)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(startPosition + new Vector2(Random.Range(-shakeAmount, shakeAmount), 0f));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(startPosition);

        // 낙하
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravity;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        yield return new WaitForSeconds(respawnDelay);

        // 복구 : 안 보이게 원위치로 옮긴 뒤 다시 표시
        SetVisible(false);
        ResetPlatform();
        yield return new WaitForSeconds(0.3f);
        SetVisible(true);
    }

    private void ResetPlatform()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        rb.position = startPosition;
        transform.position = startPosition;
        triggered = false;
    }

    private void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
        platformCollider.enabled = visible;
    }
}
