using System.Collections;
using UnityEngine;

// 보스방 문 / 기둥. 보스 이벤트에 맞춰 열리고 닫힘
// - 입구 기둥 (startsOpen = true) : 위에 떠 있다가 전투 시작 시 떨어져 막고, 처치 후 다시 올라감.
//   닫힌 위치는 기둥 아래 바닥을 찾아 자동 계산
// - 출구 문 (startsOpen = false) : 처음부터 막혀 있다가 처치 후 위로 올라가 열림
public class BossArenaGate : MonoBehaviour
{
    [SerializeField] private BossController boss;
    [Tooltip("BT 보스가 아닌 일반 몬스터가 보스방 주인일 때 (boss가 비어있을 때 사용)")]
    [SerializeField] private BaseCharacter guardian;
    [Tooltip("guardian 사용 시 플레이어가 이 거리 안에 들어오면 전투 시작으로 간주")]
    [SerializeField] private float engageRange = 12f;
    [SerializeField] private bool startsOpen = true;
    [SerializeField] private bool closeOnEngage = true;
    [SerializeField] private float closeDuration = 0.45f;  // 떨어지는 시간 (가속하며 낙하)
    [SerializeField] private bool openOnDefeat = true;
    [SerializeField] private float openDelay = 2f;         // 처치 후 열리기까지 대기
    [SerializeField] private float openDuration = 1.5f;

    private Rigidbody2D rb;
    private Collider2D gateCollider;
    private Vector2 openPosition;
    private Vector2 closedPosition;
    private Coroutine moving;

    private Transform player;
    private bool engaged;

    private void Awake()
    {
        if (boss == null && guardian == null)
            boss = FindObjectOfType<BossController>();

        // 레거시 Trap은 중력으로 떨어지는 구조였으므로 코드로 정확히 움직이도록 Kinematic으로 전환
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }
        gateCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        Vector2 current = transform.position;
        if (startsOpen)
        {
            openPosition = current;
            closedPosition = FindClosedPosition(current);
        }
        else
        {
            // 닫힌 상태에서 시작 : 문 높이만큼 위로 올라간 위치가 열린 위치
            closedPosition = current;
            float height = gateCollider != null ? gateCollider.bounds.size.y : 1f;
            openPosition = current + Vector2.up * (height + 0.5f);
        }
    }

    // 기둥 아래쪽 바닥까지의 거리만큼 내려간 위치
    private Vector2 FindClosedPosition(Vector2 from)
    {
        Bounds b = gateCollider != null ? gateCollider.bounds : new Bounds(transform.position, Vector3.one);
        Vector2 origin = new Vector2(b.center.x, b.min.y - 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 50f, LayerMask.GetMask("Ground"));
        float drop = hit.collider != null ? hit.distance + 0.05f : 0f;
        return from + Vector2.down * drop;
    }

    private void OnEnable()
    {
        if (boss != null)
        {
            boss.OnEngaged += Close;
            boss.OnDefeated += HandleBossDefeated;
        }
        else if (guardian != null)
        {
            guardian.OnDied += HandleBossDefeated;
        }
    }

    private void OnDisable()
    {
        if (boss != null)
        {
            boss.OnEngaged -= Close;
            boss.OnDefeated -= HandleBossDefeated;
        }
        else if (guardian != null)
        {
            guardian.OnDied -= HandleBossDefeated;
        }
    }

    // 일반 몬스터 보스 : 플레이어가 engageRange 안에 들어오면 전투 시작
    private void Update()
    {
        if (boss != null || guardian == null || engaged || guardian.IsDead) return;

        if (player == null)
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
        }

        if (Mathf.Abs(player.position.x - guardian.transform.position.x) <= engageRange)
        {
            engaged = true;
            Close();
        }
    }

    private void Close()
    {
        if (closeOnEngage)
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
