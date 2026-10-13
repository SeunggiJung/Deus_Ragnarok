using System.Collections;
using UnityEngine;

// 보스방 입구 기둥. 보스가 플레이어를 처음 인식(전투 시작)하면 아래로 떨어져 입구를 막고,
// 보스를 처치하면 다시 올라가 열림. 닫힌 위치는 기둥 아래 바닥을 찾아 자동 계산
public class BossArenaGate : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [SerializeField] private float closeDuration = 0.45f;  // 떨어지는 시간 (가속하며 낙하)
    [SerializeField] private bool openOnDefeat = true;
    [SerializeField] private float openDelay = 2f;         // 처치 후 열리기까지 대기
    [SerializeField] private float openDuration = 1.5f;

    private Rigidbody2D rb;
    private Collider2D gateCollider;
    private Vector2 openPosition;
    private Vector2 closedPosition;
    private Coroutine moving;

    private void Awake()
    {
        if (boss == null)
            boss = FindObjectOfType<BossController>();

        // 레거시 Trap은 중력으로 떨어지는 구조였으므로 코드로 정확히 움직이도록 Kinematic으로 전환
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }
        gateCollider = GetComponent<Collider2D>();
        openPosition = transform.position;
    }

    private void Start()
    {
        closedPosition = FindClosedPosition();
    }

    // 기둥 아래쪽 바닥까지의 거리만큼 내려간 위치
    private Vector2 FindClosedPosition()
    {
        Bounds b = gateCollider != null ? gateCollider.bounds : new Bounds(transform.position, Vector3.one);
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 50f, LayerMask.GetMask("Ground"));
        float drop = hit.collider != null ? hit.distance + 0.05f : 0f;
        return openPosition + Vector2.down * drop;
    }

    private void OnEnable()
    {
        if (boss == null) return;
        boss.OnEngaged += Close;
        boss.OnDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        if (boss == null) return;
        boss.OnEngaged -= Close;
        boss.OnDefeated -= HandleBossDefeated;
    }

    private void Close()
    {
        Move(closedPosition, closeDuration, 0f, accelerate: true);
    }

    private void HandleBossDefeated()
    {
        if (openOnDefeat)
            Move(openPosition, openDuration, openDelay, accelerate: false);
    }

    private void Move(Vector2 target, float duration, float delay, bool accelerate)
    {
        if (moving != null) StopCoroutine(moving);
        moving = StartCoroutine(MoveRoutine(target, duration, delay, accelerate));
    }

    private IEnumerator MoveRoutine(Vector2 target, float duration, float delay, bool accelerate)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector2 start = rb != null ? rb.position : (Vector2)transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // 닫힐 때는 떨어지듯 가속(t^2), 열릴 때는 부드럽게(SmoothStep)
            float eased = accelerate ? t * t : Mathf.SmoothStep(0f, 1f, t);
            SetPosition(Vector2.Lerp(start, target, eased));
            yield return new WaitForFixedUpdate();
        }
        SetPosition(target);
        moving = null;
    }

    private void SetPosition(Vector2 position)
    {
        if (rb != null) rb.MovePosition(position);
        else transform.position = position;
    }
}
