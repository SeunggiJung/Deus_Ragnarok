using System.Collections;
using UnityEngine;

// 내려찍는 장애물 (2스테이지 RockHead).
// 대기 → 예고(화난 표정 + 떨림) → 바닥까지 급강하 → 잠깐 멈춤 → 천천히 상승, 일정 확률로 중간에 멈췄다 올라가는 페이크.
// 내려찍는 중에 플레이어와 겹치면 피해 + 마지막 체크포인트로 복귀 (구르기 무적이면 통과)
//
// 원작 RockHeadController와의 차이
// - 원작 : 매 프레임 droptiming += 0.05 → 프레임 수에 따라 주기가 달라짐 / 중력으로 낙하해 속도가 일정하지 않음 / 예고 없음
// - 변경 : 시간 기준 주기, 정해진 속도로 이동(Kinematic), 예고 시간 추가, 장애물마다 시작 시간을 달리해 리듬 패턴 형성
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Crusher : MonoBehaviour
{
    private static class AnimParams
    {
        public const string Drop = "drop";
        public const string Fake = "fake";
        public const string Up = "up";
    }

    [Header("Timing")]
    [SerializeField] private float idleTime = 1.6f;        // 위에서 대기
    [SerializeField] private float startDelay = 0f;        // 처음 시작 지연 (장애물마다 다르게 해 엇박자 패턴)
    [SerializeField] private float telegraphTime = 0.45f;  // 떨어지기 전 예고
    [SerializeField] private float groundedTime = 0.6f;    // 바닥에 닿은 뒤 대기

    [Header("Movement")]
    [SerializeField] private float slamSpeed = 18f;
    [SerializeField] private float riseSpeed = 3f;
    [Range(0f, 1f)]
    [SerializeField] private float fakeChance = 0.2f;      // 원작과 같은 20% 페이크
    [Range(0f, 1f)]
    [SerializeField] private float fakeDepth = 0.45f;      // 페이크일 때 떨어지는 비율 (전체 낙하 거리 대비)
    [SerializeField] private float shakeAmount = 0.06f;

    [Header("Damage")]
    [SerializeField] private float damage = 20f;           // 원작과 같은 20
    [SerializeField] private bool sendToCheckpoint = true; // 원작 : 맞으면 시작 지점으로

    private Rigidbody2D rb;
    private Collider2D body;
    private Animator anim;
    private Vector2 topPosition;
    private float groundY;   // 바닥에 닿았을 때의 transform y
    private bool slamming;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.zero;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        body = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        topPosition = rb.position;
    }

    private void Start()
    {
        // 아래 바닥 위치를 찾아 낙하 목표 계산 (자기 콜라이더 아래에서 시작해 자신은 맞지 않음)
        Bounds b = body.bounds;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(b.center.x, b.min.y - 0.02f), Vector2.down, 30f, LayerMask.GetMask("Ground"));
        float drop = hit.collider != null ? hit.distance : 4f;
        groundY = topPosition.y - drop;

        StartCoroutine(Loop());
    }

    private IEnumerator Loop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            yield return new WaitForSeconds(idleTime);

            bool fake = Random.value < fakeChance;

            // 예고 : 표정 변화 + 제자리 떨림
            if (anim != null) anim.SetTrigger(fake ? AnimParams.Fake : AnimParams.Drop);
            yield return Shake(telegraphTime);

            // 급강하 (페이크는 중간까지만)
            float targetY = fake ? Mathf.Lerp(topPosition.y, groundY, fakeDepth) : groundY;
            slamming = true;
            yield return MoveTo(targetY, slamSpeed);
            slamming = false;

            yield return new WaitForSeconds(fake ? 0.3f : groundedTime);

            // 상승
            if (anim != null) anim.SetTrigger(AnimParams.Up);
            yield return MoveTo(topPosition.y, riseSpeed);
        }
    }

    private IEnumerator Shake(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            rb.MovePosition(topPosition + new Vector2(Random.Range(-shakeAmount, shakeAmount), 0f));
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(topPosition);
        yield return new WaitForFixedUpdate();
    }

    private IEnumerator MoveTo(float y, float speed)
    {
        while (!Mathf.Approximately(rb.position.y, y))
        {
            float next = Mathf.MoveTowards(rb.position.y, y, speed * Time.fixedDeltaTime);
            rb.MovePosition(new Vector2(topPosition.x, next));
            if (slamming) CheckCrush();
            yield return new WaitForFixedUpdate();
        }
    }

    // 내려찍는 중 아래쪽 플레이어 판정
    private void CheckCrush()
    {
        Bounds b = body.bounds;
        // 물리 충돌로 밀려나 실제로는 겹치지 않을 수 있으므로 아래쪽으로 조금 늘린 범위로 검사
        Vector2 center = (Vector2)b.center + Vector2.down * 0.1f;
        Vector2 size = new Vector2(b.size.x * 0.9f, b.size.y + 0.2f);
        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, LayerMask.GetMask("Player"));
        if (hit == null) return;

        PlayerController player = hit.GetComponentInParent<PlayerController>();
        if (player == null || player.IsDead || player.IsInvincible) return; // 구르기 무적이면 통과

        player.TakeDamage(damage);
        if (sendToCheckpoint && !player.IsDead)
        {
            var respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null) respawn.ReturnToCheckpoint();
        }
    }

    // 레거시 애니메이션 이벤트 수신용 (RockHead 클립에 남아 있는 이벤트 : 수신자가 없으면 매번 오류 로그)
    public void toidle() { }
    public void fakeidle() { }
    public void nonmoving() { }
}
