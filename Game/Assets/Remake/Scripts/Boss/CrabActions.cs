using UnityEngine;

// Crab 보스 전용 패턴 노드

// 바닥 찍기. 내려찍는 동작을 느리게 재생(붉게 점멸 = 예고) → 진행률 poundHitTime에 발밑 광역 피해 + 좌우로 충격파
// 회피 : 충격파는 낮아서 점프로 넘을 수 있고, 구르기 무적으로도 통과. 2페이즈에서는 충격파가 한 번 더 이어짐
public class GroundPoundAction : BossAnimAction
{
    private const float BlinkInterval = 0.1f;
    private bool landed;
    private float secondWaveTime;

    private CrabBossStats CS => (CrabBossStats)S;

    public GroundPoundAction(BossController boss) : base(boss, BossController.AnimParams.Slam, "Slam") { }

    protected override void OnBegin()
    {
        landed = false;
        secondWaveTime = -1f;
        boss.Anim.speed = boss.AnimSpeed * CS.poundAnimSpeed;
    }

    protected override void OnAnimUpdate(float t)
    {
        if (!landed)
        {
            bool on = Mathf.FloorToInt(Time.time / BlinkInterval) % 2 == 0;
            boss.SetTint(on ? CS.poundTelegraphTint : Color.white);

            if (t >= CS.poundHitTime)
            {
                landed = true;
                boss.RestoreTint();
                boss.DamageArea(boss.FeetPosition + Vector2.up * CS.poundArea.y * 0.5f, CS.poundArea, CS.poundDamage);
                SpawnWaves();
                if (boss.IsPhase2) secondWaveTime = Time.time + 0.6f;
            }
        }
        else if (secondWaveTime > 0f && Time.time >= secondWaveTime)
        {
            secondWaveTime = -1f;
            SpawnWaves();
        }
    }

    private void SpawnWaves()
    {
        Vector2 origin = boss.FeetPosition;
        float halfWidth = boss.Body.bounds.extents.x;
        foreach (float dir in new[] { -1f, 1f })
        {
            GroundWave.Spawn(origin + new Vector2(dir * halfWidth, 0f), dir, CS.waveSpeed, CS.waveDistance,
                CS.waveHeight, CS.waveDamage, CS.waveColor, boss.whatIsEnemy);
        }
    }

    protected override void OnEnd()
    {
        boss.Anim.speed = boss.AnimSpeed;
        boss.RestoreTint();
    }
}

// 바닥을 따라 퍼지는 충격파. 플레이어와 겹치면 한 번 피해 (구르기 무적이면 통과), 벽이나 최대 거리에서 사라짐
public class GroundWave : MonoBehaviour
{
    private static Sprite square;

    private float dir, speed, maxDistance, damage;
    private Vector2 size;
    private LayerMask targetMask;
    private float traveled;

    public static void Spawn(Vector2 feet, float dir, float speed, float distance, float height, float damage,
                             Color color, LayerMask targetMask)
    {
        var go = new GameObject("GroundWave");
        var wave = go.AddComponent<GroundWave>();
        wave.dir = dir; wave.speed = speed; wave.maxDistance = distance; wave.damage = damage;
        wave.targetMask = targetMask;
        wave.size = new Vector2(1.2f, height);

        go.transform.position = feet + Vector2.up * height * 0.5f;
        go.transform.localScale = new Vector3(wave.size.x, wave.size.y, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Square();
        sr.color = color;
        sr.sortingOrder = 20;
    }

    private static Sprite Square()
    {
        if (square != null) return square;
        var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        square = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return square;
    }

    private void FixedUpdate()
    {
        float step = speed * Time.fixedDeltaTime;
        transform.position += new Vector3(dir * step, 0f, 0f);
        traveled += step;

        if (traveled >= maxDistance || Physics2D.OverlapBox(transform.position, size * 0.8f, 0f, LayerMask.GetMask("Ground")) != null)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D hit = Physics2D.OverlapBox(transform.position, size, 0f, targetMask);
        if (hit == null) return;

        BaseCharacter target = hit.GetComponentInParent<BaseCharacter>();
        if (target == null || target.IsInvincible) return; // 구르기 무적이면 통과
        target.TakeDamage(damage);
        Destroy(gameObject);
    }
}

// 잡기. 예고(노랗게 점멸, 구르기 준비) → 앞으로 돌진 → 닿으면 잡혀서 제압 → 큰 피해 후 풀려남
// 점프로는 피할 수 없고 구르기(무적)로만 회피. 빗나가면 보스가 오래 무방비 (구르기 성공 보상)
public class GrabAction : BossAction
{
    private enum Phase { Telegraph, Lunge, Hold, Recover }
    private const float BlinkInterval = 0.08f;
    private const float RecoverTime = 0.4f;

    private Phase phase;
    private float phaseStart;
    private float dir;
    private float recoverTime;
    private PlayerController held;

    private CrabBossStats CS => (CrabBossStats)S;

    public GrabAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.StopMoving();
        boss.FaceTarget();
        dir = boss.FacingDir;
        held = null;
        recoverTime = RecoverTime;
        Enter(Phase.Telegraph);
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Telegraph:
                bool on = Mathf.FloorToInt(elapsed / BlinkInterval) % 2 == 0;
                boss.SetTint(on ? CS.grabTelegraphTint : Color.white);
                if (elapsed >= CS.grabTelegraph * (boss.IsPhase2 ? 0.8f : 1f))
                {
                    boss.RestoreTint();
                    boss.PlayAction(BossController.AnimParams.Grab);
                    Enter(Phase.Lunge);
                }
                return NodeState.Running;

            case Phase.Lunge:
                boss.SetVelocityX(dir * CS.grabLungeSpeed);
                if (TryGrab())
                {
                    boss.StopMoving();
                    boss.SetTint(CS.grabTelegraphTint);
                    Enter(Phase.Hold);
                }
                else if (elapsed >= CS.grabLungeTime)
                {
                    boss.StopMoving();
                    recoverTime = CS.grabMissRecovery;
                    Enter(Phase.Recover);
                }
                return NodeState.Running;

            case Phase.Hold:
                if (held == null || held.IsDead) return NodeState.Success;
                Pin();
                if (elapsed >= CS.grabHoldTime)
                {
                    PlayerController target = held;
                    Release();
                    target.TakeDamage(CS.grabDamage);
                    boss.RestoreTint();
                    Enter(Phase.Recover);
                }
                return NodeState.Running;

            default: // Recover
                return elapsed >= recoverTime ? NodeState.Success : NodeState.Running;
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    // 몸 앞쪽 범위에 플레이어가 있으면 잡기 성공 (구르기 무적이면 빗나감)
    private bool TryGrab()
    {
        Bounds b = boss.Body.bounds;
        Vector2 center = new Vector2(b.center.x + dir * b.extents.x * 0.9f, b.center.y);
        Vector2 size = new Vector2(2f, b.size.y);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f, boss.whatIsEnemy))
        {
            var player = hit.GetComponentInParent<PlayerController>();
            if (player == null || player.IsDead || player.IsInvincible) continue;
            held = player;
            held.InputLocked = true;
            return true;
        }
        return false;
    }

    // 잡힌 동안 보스 집게 앞에 고정
    private void Pin()
    {
        Bounds b = boss.Body.bounds;
        Rigidbody2D rb = held.Rb;
        rb.velocity = Vector2.zero;
        rb.position = new Vector2(b.center.x + dir * (b.extents.x + 0.4f), rb.position.y);
    }

    private void Release()
    {
        if (held != null) held.InputLocked = false;
        held = null;
    }

    protected override void OnStop()
    {
        Release();
        boss.StopMoving();
        boss.RestoreTint();
        boss.Anim.ResetTrigger(BossController.AnimParams.Grab);
    }
}
