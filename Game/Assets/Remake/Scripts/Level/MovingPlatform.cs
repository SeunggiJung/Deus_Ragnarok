using UnityEngine;

// 두 지점 사이를 오가는 발판 (기본 : 위아래). 끝에서 잠깐 멈춤.
// Kinematic + MovePosition으로 움직여 위에 탄 플레이어가 물리적으로 함께 이동
// (원작 trap.cs의 위아래 왕복을 정리 : 시작 위치 기준 오프셋, 속도 / 대기 시간 조절 가능)
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 offsetA = new Vector2(0f, -1f);  // 시작 위치 기준
    [SerializeField] private Vector2 offsetB = new Vector2(0f, 2f);
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float pauseTime = 0.5f;                  // 양 끝에서 멈추는 시간

    private Rigidbody2D rb;
    private Vector2 pointA;
    private Vector2 pointB;
    private bool towardB = true;
    private float pausedUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.velocity = Vector2.zero;

        Vector2 start = rb.position;
        pointA = start + offsetA;
        pointB = start + offsetB;
    }

    private void FixedUpdate()
    {
        if (Time.time < pausedUntil) return;

        Vector2 target = towardB ? pointB : pointA;
        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f)
        {
            towardB = !towardB;
            pausedUntil = Time.time + pauseTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 start = Application.isPlaying ? (pointA - offsetA) : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start + offsetA, start + offsetB);
        Gizmos.DrawWireSphere(start + offsetA, 0.15f);
        Gizmos.DrawWireSphere(start + offsetB, 0.15f);
    }
}
