using UnityEngine;

// Crab 보스 전용 패턴 노드
// 예고는 몸 색(붉은 계열이라 구분이 어려움) 대신 BossTelegraph(링 + 아이콘 + 패턴 색)로 표시한다.

// 바닥 찍기. 내려찍는 동작을 느리게 재생하며 예고 → 진행률 poundHitTime에 발밑 광역 피해 + 좌우로 충격파
// 회피 : 충격파는 낮아서 점프로 넘을 수 있고, 구르기 무적으로도 통과. 2페이즈에서는 충격파가 한 번 더 이어짐
public class GroundPoundAction : BossAnimAction
{
    private bool landed;
    private float secondWaveTime;

    private CrabBossStats CS => (CrabBossStats)S;

    public GroundPoundAction(BossController boss) : base(boss, BossController.AnimParams.Slam, "Slam") { }

    protected override void OnBegin()
    {
        landed = false;
        secondWaveTime = -1f;
        boss.Anim.speed = boss.AnimSpeed * CS.poundAnimSpeed;

        // 애니메이션 전체 길이 중 내려찍기까지 걸리는 시간만큼 예고
        float clipLength = 0.9167f / (boss.AnimSpeed * CS.poundAnimSpeed);
        boss.Telegraph.Show(TelegraphKind.Pound, clipLength * CS.poundHitTime);
    }

    protected override void OnAnimUpdate(float t)
    {
        if (!landed)
        {
            if (t < CS.poundHitTime) return;

            landed = true;
            boss.Telegraph.Hide();
            boss.DamageArea(boss.FeetPosition + Vector2.up * CS.poundArea.y * 0.5f, CS.poundArea, CS.poundDamage);
            SpawnWaves();
            Impact();
            if (boss.IsPhase2) secondWaveTime = Time.time + 0.6f;
        }
        else if (secondWaveTime > 0f && Time.time >= secondWaveTime)
        {
            secondWaveTime = -1f;
            SpawnWaves();
        }
    }

    private void Impact()
    {
        Vector2 feet = boss.FeetPosition;
        float width = boss.Body.bounds.size.x;
        Fx.Ring(feet, new Color(1f, 0.7f, 0.3f, 0.9f), width * 0.8f, width * 2.6f, 0.45f);
        Fx.Burst(feet, Vector2.up, 120f, new Color(0.55f, 0.4f, 0.3f), 22, 4f, 9f, 0.15f, 0.35f, 0.9f, 2.5f, hard: true);
        Fx.Burst(feet, Vector2.up, 160f, new Color(0.8f, 0.7f, 0.55f, 0.7f), 14, 1f, 4f, 0.5f, 0.9f, 0.7f, 0f, hard: false);
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
        boss.Telegraph.Hide();
    }
}

// 바닥을 따라 퍼지는 충격파. 솟아오르는 돌 가시 3개 + 먼지 꼬리.
// 플레이어와 겹치면 한 번 피해 (구르기 무적이면 통과), 벽이나 최대 거리에서 사라짐
public class GroundWave : MonoBehaviour
{
    private static readonly float[] SpikeOffsets = { -0.45f, 0f, 0.45f };
    private static readonly float[] SpikeHeights = { 0.85f, 1.25f, 0.9f };

    private float dir, speed, maxDistance, damage;
    private Vector2 size;
    private LayerMask targetMask;
    private float traveled;
    private SpriteRenderer[] spikes;
    private float seed;

    public static void Spawn(Vector2 feet, float dir, float speed, float distance, float height, float damage,
                             Color color, LayerMask targetMask)
    {
        var go = new GameObject("GroundWave");
        go.transform.position = feet + Vector2.up * height * 0.5f;
        var wave = go.AddComponent<GroundWave>();
        wave.dir = dir; wave.speed = speed; wave.maxDistance = distance; wave.damage = damage;
        wave.targetMask = targetMask;
        wave.size = new Vector2(1.4f, height);
        wave.seed = Random.value * 10f;

        // 돌 가시 (바닥 기준으로 솟아오름) : 어두운 외곽 + 밝은 속 두 겹
        wave.spikes = new SpriteRenderer[SpikeOffsets.Length * 2];
        for (int i = 0; i < SpikeOffsets.Length; i++)
        {
            wave.spikes[i * 2] = wave.NewSpike(SpikeOffsets[i], new Color(color.r * 0.45f, color.g * 0.35f, color.b * 0.3f, 1f), 20, height * 0.5f);
            wave.spikes[i * 2 + 1] = wave.NewSpike(SpikeOffsets[i], color, 21, height * 0.5f);
        }

        // 먼지 꼬리 + 출발 지점 파편
        Fx.Trail(go.transform, new Color(0.8f, 0.7f, 0.55f, 0.55f), 45f, 0.6f, 0.45f, new Vector2(-dir * 0.6f, 1.2f));
        Fx.Burst(feet, new Vector2(dir, 1f), 70f, new Color(0.6f, 0.45f, 0.3f), 10, 3f, 7f, 0.12f, 0.28f, 0.6f, 2.5f);
    }

    private SpriteRenderer NewSpike(float offset, Color color, int order, float groundOffset)
    {
        var child = new GameObject("Spike");
        child.transform.SetParent(transform, false);
        child.transform.localPosition = new Vector3(offset, -groundOffset, 0f);
        var sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = Fx.Spike();
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    private void Update()
    {
        // 가시가 위아래로 출렁이며 달려가는 느낌
        for (int i = 0; i < spikes.Length; i++)
        {
            int index = i / 2;
            bool inner = i % 2 == 1;
            float wobble = 0.8f + 0.2f * Mathf.Sin(Time.time * 18f + seed + index * 2f);
            float h = SpikeHeights[index] * wobble * (inner ? 0.8f : 1f);
            spikes[i].transform.localScale = new Vector3(inner ? 1.4f : 1.9f, h, 1f);
        }
    }

    private void FixedUpdate()
    {
        float step = speed * Time.fixedDeltaTime;
        transform.position += new Vector3(dir * step, 0f, 0f);
        traveled += step;

        if (traveled >= maxDistance || Physics2D.OverlapBox(transform.position, size * 0.8f, 0f, LayerMask.GetMask("Ground")) != null)
        {
            Vanish();
            return;
        }

        Collider2D hit = Physics2D.OverlapBox(transform.position, size, 0f, targetMask);
        if (hit == null) return;

        BaseCharacter target = hit.GetComponentInParent<BaseCharacter>();
        if (target == null || target.IsInvincible) return; // 구르기 무적이면 통과
        target.TakeDamage(damage);
        Fx.Burst(transform.position, Vector2.up, 100f, new Color(1f, 0.6f, 0.2f), 12, 3f, 7f, 0.1f, 0.25f, 0.5f, 2f);
        Vanish();
    }

    private void Vanish()
    {
        // 먼지 꼬리가 자연스럽게 사라지도록 남김
        var trail = GetComponentInChildren<ParticleSystem>();
        if (trail != null)
        {
            trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trail.transform.SetParent(null);
            Destroy(trail.gameObject, 1f);
        }
        Destroy(gameObject);
    }
}

// 잡기. 예고(노란 느낌표 + 수축 링, 구르기 준비) → 앞으로 돌진 → 닿으면 잡혀서 제압 → 큰 피해 후 풀려남
// 점프로는 피할 수 없고 구르기(무적)로만 회피. 빗나가면 보스가 오래 무방비 (구르기 성공 보상)
public class GrabAction : BossAction
{
    private enum Phase { Telegraph, Lunge, Hold, Recover }
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
        boss.Telegraph.Show(TelegraphKind.Grab, TelegraphTime);
        Enter(Phase.Telegraph);
    }

    private float TelegraphTime => CS.grabTelegraph * (boss.IsPhase2 ? 0.8f : 1f);

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Telegraph:
                if (elapsed >= TelegraphTime)
                {
                    boss.Telegraph.Hide();
                    boss.PlayAction(BossController.AnimParams.Grab);
                    Fx.Burst(boss.FeetPosition, new Vector2(-dir, 1f), 60f, new Color(0.8f, 0.7f, 0.55f, 0.7f), 8, 2f, 5f, 0.4f, 0.8f, 0.5f, 0f, hard: false);
                    Enter(Phase.Lunge);
                }
                return NodeState.Running;

            case Phase.Lunge:
                boss.SetVelocityX(dir * CS.grabLungeSpeed);
                if (TryGrab())
                {
                    boss.StopMoving();
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
                Pin(elapsed);
                if (elapsed >= CS.grabHoldTime)
                {
                    PlayerController target = held;
                    Release();
                    target.TakeDamage(CS.grabDamage);
                    Fx.Burst(target.transform.position, Vector2.up, 360f, new Color(1f, 0.9f, 0.3f), 16, 3f, 8f, 0.12f, 0.28f, 0.5f, 1f);
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
            Fx.Burst(player.transform.position, Vector2.up, 360f, new Color(1f, 0.95f, 0.4f), 12, 2f, 6f, 0.1f, 0.25f, 0.4f, 0.5f);
            return true;
        }
        return false;
    }

    // 잡힌 동안 보스 집게 앞에 고정 (발버둥치듯 미세하게 떨림)
    private void Pin(float elapsed)
    {
        Bounds b = boss.Body.bounds;
        Rigidbody2D rb = held.Rb;
        rb.velocity = Vector2.zero;
        float shake = Mathf.Sin(elapsed * 60f) * 0.08f;
        rb.position = new Vector2(b.center.x + dir * (b.extents.x + 0.4f) + shake, rb.position.y);
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
        boss.Telegraph.Hide();
        boss.Anim.ResetTrigger(BossController.AnimParams.Grab);
    }
}

// 도약 내려찍기. 기를 모음(보라 링 + ▲ + 모여드는 입자) → 화면 밖으로 높이 도약 → 바닥에 장판이 나타나 플레이어를 따라다님
// → 장판이 고정되고 안쪽이 차오름(이때 피해야 함) → 장판 위로 내려찍음
// 회피 : 장판 밖으로 벗어나거나, 내려찍는 순간 구르기 무적 / 점프. 공중에 있는 동안 보스는 무적이며 공격할 수 없음
// 2페이즈에서는 곧바로 한 번 더 도약
public class LeapSlamAction : BossAction
{
    private enum Phase { Gather, Rise, Track, Lock, Fall, Pause, Recover }

    private Phase phase;
    private float phaseStart;
    private int leapsLeft;
    private float groundY;           // 보스 몸 바닥(발) 높이
    private float restY;             // 보스 transform이 바닥에 서 있을 때의 y
    private float minX, maxX;        // 착지 가능한 범위 (벽 안쪽)
    private float startX, highY;
    private GroundZone zone;
    private bool airborne;

    private CrabBossStats CS => (CrabBossStats)S;

    public LeapSlamAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.StopMoving();
        boss.FaceTarget();
        restY = boss.transform.position.y;
        groundY = boss.FeetPosition.y;
        FindLandingLimits();
        leapsLeft = boss.IsPhase2 ? 2 : 1;

        boss.PlayAction(BossController.AnimParams.Ability);   // 집게를 치켜드는 모션
        boss.Telegraph.Show(TelegraphKind.Leap, CS.leapGatherTime);
        Enter(Phase.Gather);
    }

    // 좌우 벽까지 거리를 재서 보스가 벽에 끼지 않는 착지 범위를 계산
    private void FindLandingLimits()
    {
        Bounds b = boss.Body.bounds;
        int ground = LayerMask.GetMask("Ground");
        RaycastHit2D left = Physics2D.Raycast(b.center, Vector2.left, 80f, ground);
        RaycastHit2D right = Physics2D.Raycast(b.center, Vector2.right, 80f, ground);
        float margin = b.extents.x + 0.3f;
        minX = left.collider != null ? left.point.x + margin : b.center.x - 40f;
        maxX = right.collider != null ? right.point.x - margin : b.center.x + 40f;
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Gather:
                if (elapsed >= CS.leapGatherTime)
                {
                    boss.Telegraph.Hide();
                    BeginRise();
                }
                return NodeState.Running;

            case Phase.Rise:
            {
                float t = Mathf.Clamp01(elapsed / CS.leapRiseTime);
                float eased = 1f - (1f - t) * (1f - t);
                SetBossPosition(startX, Mathf.Lerp(restY, highY, eased));
                TrackPlayer();
                if (t >= 1f) Enter(Phase.Track);
                return NodeState.Running;
            }

            case Phase.Track:
                TrackPlayer();
                if (elapsed >= CS.leapTrackTime * (boss.IsPhase2 ? 0.75f : 1f))
                {
                    zone.Lock(CS.leapLockTime);
                    Enter(Phase.Lock);
                }
                return NodeState.Running;

            case Phase.Lock:
                if (elapsed >= CS.leapLockTime) Enter(Phase.Fall);
                return NodeState.Running;

            case Phase.Fall:
            {
                float t = Mathf.Clamp01(elapsed / CS.leapFallTime);
                SetBossPosition(zone.X, Mathf.Lerp(highY, restY, t * t));
                if (t >= 1f) Land();
                return NodeState.Running;
            }

            case Phase.Pause:
                if (elapsed >= 0.35f) BeginRise();
                return NodeState.Running;

            default: // Recover
                return elapsed >= 0.5f ? NodeState.Success : NodeState.Running;
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    private void BeginRise()
    {
        leapsLeft--;
        startX = boss.transform.position.x;
        highY = restY + CS.leapHeight;

        // 공중에서는 물리 / 피격 / 충돌 없이 코드로 이동
        boss.Rb.bodyType = RigidbodyType2D.Kinematic;
        boss.Rb.velocity = Vector2.zero;
        boss.Body.enabled = false;
        boss.ForceInvincible = true;
        airborne = true;

        // 도약 순간 먼지 + 링
        Vector2 feet = new Vector2(startX, groundY);
        Fx.Ring(feet, new Color(0.8f, 0.6f, 1f, 0.9f), 2f, 7f, 0.4f);
        Fx.Burst(feet, Vector2.up, 140f, new Color(0.75f, 0.65f, 0.5f, 0.8f), 18, 3f, 8f, 0.5f, 0.9f, 0.7f, 0.2f, hard: false);

        // 바닥 장판 : 처음에는 보스가 있던 자리에서 시작해 플레이어를 따라감
        if (zone != null) zone.Dismiss();
        zone = GroundZone.Create(new Vector2(Mathf.Clamp(startX, minX, maxX), groundY + 0.05f), CS.leapRadius);
        Enter(Phase.Rise);
    }

    // 장판(과 공중의 보스)이 플레이어의 x를 부드럽게 쫓아감. 착지 가능한 범위 안으로 제한
    private void TrackPlayer()
    {
        if (zone == null || boss.Target == null) return;
        float targetX = Mathf.Clamp(boss.Target.transform.position.x, minX, maxX);
        float x = Mathf.MoveTowards(zone.X, targetX, CS.leapTrackSpeed * Time.deltaTime);
        zone.SetX(x);
        SetBossPosition(x, boss.transform.position.y);
    }

    private void SetBossPosition(float x, float y)
    {
        boss.transform.position = new Vector3(x, y, boss.transform.position.z);
        boss.Rb.velocity = Vector2.zero;
    }

    private void Land()
    {
        SetBossPosition(zone.X, restY);
        Restore();

        // 장판 안(발이 바닥 근처)에 있는 대상에게 피해. 구르기 무적 / 점프로 띄우면 회피
        Vector2 center = new Vector2(zone.X, groundY + 0.5f);
        Vector2 size = new Vector2(zone.Radius * 2f, 1f);
        var damaged = new System.Collections.Generic.HashSet<BaseCharacter>();
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f, boss.whatIsEnemy))
        {
            BaseCharacter target = hit.GetComponentInParent<BaseCharacter>();
            if (target == null || target.IsInvincible || !damaged.Add(target)) continue;
            target.TakeDamage(CS.leapDamage);
        }

        // 착지 연출 : 충격 링 2겹, 돌 파편, 먼지
        Vector2 feet = new Vector2(zone.X, groundY);
        Fx.Ring(feet, new Color(1f, 0.35f, 0.15f, 1f), zone.Radius, zone.Radius * 3.2f, 0.5f);
        Fx.Ring(feet, new Color(1f, 0.8f, 0.4f, 0.9f), zone.Radius * 0.6f, zone.Radius * 2.2f, 0.35f);
        Fx.Burst(feet, Vector2.up, 150f, new Color(0.55f, 0.4f, 0.3f), 34, 5f, 12f, 0.15f, 0.4f, 1.1f, 2.5f);
        Fx.Burst(feet, Vector2.up, 170f, new Color(0.8f, 0.7f, 0.55f, 0.7f), 20, 1.5f, 5f, 0.6f, 1.1f, 0.8f, 0f, hard: false);
        zone.Dismiss();

        Enter(leapsLeft > 0 ? Phase.Pause : Phase.Recover);
    }

    // 공중 상태 해제 : 물리 / 충돌 / 무적 복구
    private void Restore()
    {
        if (!airborne) return;
        airborne = false;
        boss.Body.enabled = true;
        boss.Rb.bodyType = RigidbodyType2D.Dynamic;
        boss.Rb.velocity = Vector2.zero;
        boss.ForceInvincible = false;
    }

    protected override void OnStop()
    {
        // 도중에 끊겨도(플레이어 사망 등) 공중에 갇히지 않도록 바닥으로 복귀
        if (airborne)
        {
            Vector3 p = boss.transform.position;
            boss.transform.position = new Vector3(p.x, restY, p.z);
            Restore();
        }
        if (zone != null) zone.Dismiss();
        zone = null;
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
    }
}

// 기 모아 강타. 기를 모으는 동안(파랑 별 + 수축 링) 보스는 무방비 → 링이 몸에 닿기 직전 30% 구간(카운터 창)에
// 플레이어가 점프 공격을 맞추면 패턴이 끊기고 긴 그로기. 놓치면 앞쪽에 큰 한 방 (구르기 무적으로 회피)
public class HeavyStrikeAction : BossAction
{
    private enum Phase { Charge, Strike, Recover }
    private const float WindowRatio = 0.3f;   // BossTelegraph가 임박 점멸을 시작하는 시점(70%)과 맞춤

    private Phase phase;
    private float phaseStart;
    private float dir;
    private bool struck;
    private bool windowOpened;

    private CrabBossStats CS => (CrabBossStats)S;

    public HeavyStrikeAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.StopMoving();
        boss.FaceTarget();
        dir = boss.FacingDir;
        struck = false;
        windowOpened = false;

        boss.PlayAction(BossController.AnimParams.Ability);   // 집게를 치켜드는 모션
        boss.Telegraph.Show(TelegraphKind.Heavy, CS.heavyChargeTime);
        Enter(Phase.Charge);
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Charge:
                if (!windowOpened && elapsed >= CS.heavyChargeTime * (1f - WindowRatio))
                {
                    windowOpened = true;
                    boss.CounterWindowOpen = true;
                    // 카운터 창이 열렸다는 신호
                    Bounds b = boss.Body.bounds;
                    Fx.Ring(b.center, new Color(0.6f, 0.7f, 1f, 0.9f), b.size.x * 2f, b.size.x * 0.8f, 0.25f, 1f);
                }
                if (elapsed >= CS.heavyChargeTime)
                {
                    boss.CounterWindowOpen = false;
                    boss.Telegraph.Hide();
                    boss.HitEventHandler = Strike;
                    boss.PlayAction(BossController.AnimParams.Melee);
                    Enter(Phase.Strike);
                }
                return NodeState.Running;

            case Phase.Strike:
                // 공격 애니메이션 Hit 이벤트에서 판정. 이벤트가 없어도 갇히지 않도록 제한 시간
                if (struck || elapsed > 1.2f) Enter(Phase.Recover);
                return NodeState.Running;

            default: // Recover
                return elapsed >= CS.heavyRecovery ? NodeState.Success : NodeState.Running;
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    private void Strike()
    {
        if (struck) return;
        struck = true;

        Bounds b = boss.Body.bounds;
        Vector2 center = new Vector2(b.center.x + dir * (b.extents.x + CS.heavyArea.x * 0.35f), b.center.y);
        boss.DamageArea(center, CS.heavyArea, CS.heavyDamage);

        Vector2 feet = new Vector2(b.center.x + dir * (b.extents.x + 1f), b.min.y);
        Fx.Ring(feet, new Color(0.7f, 0.8f, 1f, 0.95f), 1.5f, 6f, 0.35f);
        Fx.Burst(feet, new Vector2(dir, 0.6f), 90f, new Color(0.6f, 0.7f, 1f), 18, 5f, 11f, 0.12f, 0.3f, 0.6f, 1.5f);
        Fx.Burst(feet, Vector2.up, 150f, new Color(0.75f, 0.65f, 0.5f, 0.7f), 12, 1.5f, 4f, 0.5f, 0.9f, 0.7f, 0f, hard: false);
    }

    protected override void OnStop()
    {
        boss.CounterWindowOpen = false;
        boss.HitEventHandler = null;
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
        boss.Anim.ResetTrigger(BossController.AnimParams.Melee);
    }
}
