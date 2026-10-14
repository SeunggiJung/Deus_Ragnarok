using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

// 3스테이지 끝 연출 (원작 camera.EndingScene 계승) :
//   출구 문 너머 트리거에 닿으면 조작 불가 → 카메라가 오른쪽(Target)을 비추고 → 돌 기둥(Stair1~3)이 솟아올라 계단이 됨
//   → 카메라가 플레이어에게 돌아오고 → 플레이어가 앞으로 걸어가(계단 턱은 자동 점프) → 화면이 어두워지며 엔딩 씬으로 이동
// 원작의 기둥은 충돌체가 없는 장식이었지만, 여기서는 충돌체를 붙여 실제로 걸어 올라감.
// 기둥 / 카메라 목표는 비워 두면 씬에서 이름(Stair1~3, Target)으로 자동으로 찾음
[RequireComponent(typeof(Collider2D))]
public class EndingTrigger : MonoBehaviour
{
    [SerializeField] private string endingSceneName = "Ending";
    [SerializeField] private float fadeDuration = 1.2f;

    [Header("Ending Sequence")]
    [SerializeField] private Transform[] pillars;
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float pillarRestY = 0f;        // 기둥이 멈추는 높이 (Grid 기준, 원작 값)
    [SerializeField] private float pillarSpeed = 1.3f;      // 기둥이 솟는 속도 (원작 0.8)
    [SerializeField] private float cameraMoveTime = 2f;
    [SerializeField] private float holdAfterRise = 1.2f;    // 계단이 완성된 뒤 보여 주는 시간
    [SerializeField] private float returnDelay = 1.0f;      // 카메라가 플레이어에게 돌아오는 시간
    [SerializeField] private float walkTimeout = 15f;
    [SerializeField] private float shakeAmount = 0.05f;

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
        StartCoroutine(EndingRoutine(player));
    }

    private IEnumerator EndingRoutine(PlayerController player)
    {
        // 1. 조작 불가 : 멈추고 무적 (시퀀스 도중 피해 / 낙사 방지)
        player.InputLocked = true;
        player.ForceInvincible = true;
        player.SetVelocity(0f, player.Rb.velocity.y);
        PlayerSession.Save(player);
        Debug.Log("엔딩 연출 시작");

        BGMManager_Hades bgm = BGMManager_Hades.Instance;
        if (bgm != null && bgm.rs != null) bgm.PlaySound(bgm.rs);   // 원작 : 엔딩 BGM

        ResolveReferences();
        Camera cam = Camera.main;
        var follow = cam != null ? cam.GetComponent<camera>() : null;

        // 2. 카메라가 오른쪽(계단이 생길 곳)을 비춤
        Vector3 focus = cam != null ? FocusPosition(cam, follow) : Vector3.zero;
        if (cam != null && follow != null && cameraTarget != null)
        {
            follow.isFocusing = true;
            Vector3 from = cam.transform.position;
            float elapsed = 0f;
            while (elapsed < cameraMoveTime)
            {
                elapsed += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(from, focus, Mathf.SmoothStep(0f, 1f, elapsed / cameraMoveTime));
                yield return null;
            }
            cam.transform.position = focus;
        }

        // 3. 돌 기둥이 솟아올라 계단이 됨
        yield return RisePillars(cam, focus, follow != null && cameraTarget != null);
        yield return new WaitForSeconds(holdAfterRise);

        // 4. 카메라가 플레이어에게 돌아옴 (camera 스크립트가 부드럽게 따라감)
        if (follow != null) follow.isFocusing = false;
        yield return new WaitForSeconds(returnDelay);

        // 5. 플레이어가 앞으로 걸어감 : 계단 턱 앞에서 자동 점프. 마지막 기둥 위에서는 끝까지 걸어가 가장자리에서 멈춤
        //    (끝 너머에는 바닥이 없어서 떨어지지 않도록). 가장자리 가까이 오면 어두워지기 시작해 엔딩으로 전환
        float edgeX = StairEdgeX(player);
        float fadeStartX = edgeX - 1.2f;
        player.AutoMoveX = 1f;
        float walked = 0f;
        while (walked < walkTimeout && player.transform.position.x < fadeStartX)
        {
            walked += Time.deltaTime;
            player.RefillStamina();   // 점프 스태미너가 모자라 멈추는 일이 없도록
            if (NeedsJump(player)) player.AutoJump = true;
            yield return null;
        }

        ScreenFader.FadeAndLoad(endingSceneName, fadeDuration);
        float fade = 0f;
        while (fade < fadeDuration)   // 어두워지는 동안 가장자리까지 계속 걸어감
        {
            fade += Time.deltaTime;
            if (player.transform.position.x >= edgeX) player.AutoMoveX = 0f;
            if (NeedsJump(player)) player.AutoJump = true;
            yield return null;
        }
        player.AutoMoveX = 0f;
    }

    // 걸어갈 끝 지점 : 가장 오른쪽 기둥의 오른쪽 끝에서 조금 안쪽 (기둥이 없으면 카메라 목표 앞)
    private float StairEdgeX(PlayerController player)
    {
        float edge = float.NegativeInfinity;
        foreach (Transform p in pillars)
        {
            if (p == null) continue;
            var renderer = p.GetComponent<Renderer>();
            if (renderer != null) edge = Mathf.Max(edge, renderer.bounds.max.x);
        }
        if (edge > float.NegativeInfinity) return edge - 0.5f;
        return cameraTarget != null ? cameraTarget.position.x - 0.5f : player.transform.position.x + 12f;
    }

    #region Pillars
    private IEnumerator RisePillars(Camera cam, Vector3 focus, bool shake)
    {
        GameAudio.Play(Sfx.Rumble, 0.9f, 0f);   // 기둥이 솟는 땅울림
        var rbs = new Rigidbody2D[pillars.Length];
        var done = new bool[pillars.Length];
        var nextDust = new float[pillars.Length];
        for (int i = 0; i < pillars.Length; i++)
        {
            if (pillars[i] == null) { done[i] = true; continue; }
            PreparePillar(pillars[i].gameObject, out rbs[i]);
            if (rbs[i] != null) rbs[i].velocity = Vector2.up * pillarSpeed;
        }

        while (true)
        {
            bool all = true;
            for (int i = 0; i < pillars.Length; i++)
            {
                if (done[i]) continue;
                all = false;

                Transform p = pillars[i];
                if (p.position.y >= pillarRestY)
                {
                    // 정확한 높이에 멈춤
                    p.position = new Vector3(p.position.x, pillarRestY, p.position.z);
                    if (rbs[i] != null) { rbs[i].velocity = Vector2.zero; }
                    done[i] = true;
                    EmitDust(p, 14);
                }
                else if (Time.time >= nextDust[i])
                {
                    nextDust[i] = Time.time + 0.2f;
                    EmitDust(p, 4);
                }
            }
            if (all) break;

            if (shake && cam != null)
                cam.transform.position = focus + (Vector3)(Random.insideUnitCircle * shakeAmount);
            yield return null;
        }

        if (shake && cam != null) cam.transform.position = focus;
    }

    // 기둥을 밟을 수 있게 : 코드로만 움직이는 Kinematic + Ground 레이어 + 충돌체 (원작에는 충돌체가 없었음)
    private static void PreparePillar(GameObject pillar, out Rigidbody2D rb)
    {
        rb = pillar.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }

        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) pillar.layer = ground;

        if (pillar.GetComponent<TilemapCollider2D>() == null)
            pillar.AddComponent<TilemapCollider2D>();
    }

    // 기둥 윗면에서 먼지가 튐
    private static void EmitDust(Transform pillar, int count)
    {
        var renderer = pillar.GetComponent<Renderer>();
        if (renderer == null) return;
        Bounds b = renderer.bounds;
        Vector2 top = new Vector2(b.center.x, b.max.y);
        Fx.Burst(top, Vector2.up, 140f, new Color(0.7f, 0.65f, 0.55f, 0.8f), count, 0.6f, 2.2f, 0.08f, 0.22f, 0.8f, 0.15f);
    }
    #endregion

    #region Walk
    // 앞에 낮은 벽(계단 턱)이 있으면 점프. 발 높이에서 앞쪽으로 짧게 Raycast
    private static bool NeedsJump(PlayerController player)
    {
        if (!player.IsGrounded()) return false;

        Bounds b = player.BodyCollider.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.25f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right, b.extents.x + 0.5f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }
    #endregion

    #region Setup
    private void ResolveReferences()
    {
        if (cameraTarget == null)
        {
            GameObject target = GameObject.Find("Target");
            if (target != null) cameraTarget = target.transform;
        }

        bool missing = pillars == null || pillars.Length == 0;
        if (!missing)
            foreach (Transform p in pillars)
                if (p == null) missing = true;

        if (missing)
        {
            var found = new System.Collections.Generic.List<Transform>();
            for (int i = 1; i <= 3; i++)
            {
                GameObject go = GameObject.Find("Stair" + i);
                if (go != null) found.Add(go.transform);
            }
            pillars = found.ToArray();
        }

        if (cameraTarget == null) Debug.LogWarning("엔딩 : 카메라 목표(Target)를 찾지 못해 카메라 연출을 생략합니다.");
        if (pillars.Length == 0) Debug.LogWarning("엔딩 : 돌 기둥(Stair1~3)을 찾지 못해 기둥 연출을 생략합니다.");
    }

    // 원작과 같이 카메라 이동 한계 안으로 제한한 목표 위치
    private Vector3 FocusPosition(Camera cam, camera follow)
    {
        if (cameraTarget == null) return cam.transform.position;
        if (follow == null) return new Vector3(cameraTarget.position.x, cameraTarget.position.y, cam.transform.position.z);

        float halfWidth = cam.aspect * cam.orthographicSize;
        float halfHeight = cam.orthographicSize;
        return new Vector3(
            Mathf.Clamp(cameraTarget.position.x, follow.limitMinX + halfWidth, follow.limitMaxX - halfWidth),
            Mathf.Clamp(cameraTarget.position.y, follow.limitMinY + halfHeight, follow.limitMaxY - halfHeight),
            -10f);
    }
    #endregion
}
