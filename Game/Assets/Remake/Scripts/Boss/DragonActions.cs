using System.Collections.Generic;
using UnityEngine;

// 드래곤 보스 전용 행동 노드

// 화염 브레스. 기를 모으는 동안(파랑 별 + 수축 링) 보스는 무방비 → 후반 구간(카운터 창)에 플레이어가 점프 공격을 맞추면
// 패턴이 끊기고 긴 그로기. 놓치면 앞쪽으로 불줄기를 뿜음 (구르기 무적으로 통과 가능). 2페이즈에서는 2연속
public class BreathAction : BossAction
{
    private enum Phase { Charge, Fire, Recover }

    private Phase phase;
    private float phaseStart;
    private float dir;
    private int breathsLeft;
    private int breathIndex;
    private bool windowOpened;
    private float nextTick;
    private GameObject beam;
    private readonly List<SpriteRenderer> beamParts = new List<SpriteRenderer>();

    private DragonBossStats DS => (DragonBossStats)S;

    public BreathAction(BossController boss) : base(boss) { }

    // 2페이즈 : 충전이 빨라지고 카운터 창도 좁아짐. 2연속의 두 번째는 더 짧게 모음
    private float ChargeTime => DS.breathChargeTime * (boss.IsPhase2 ? 0.8f : 1f) * (breathIndex > 0 ? 0.7f : 1f);
    private float WindowRatio => boss.IsPhase2 ? DS.breathWindowRatio * 0.85f : DS.breathWindowRatio;

    protected override void OnStart()
    {
        breathsLeft = boss.IsPhase2 ? 2 : 1;
        breathIndex = 0;
        BeginCharge();
    }

    private void BeginCharge()
    {
        breathsLeft--;
        boss.StopMoving();
        boss.FaceTarget();
        dir = boss.FacingDir;
        windowOpened = false;

        boss.PlayAction(BossController.AnimParams.Ability);   // 웅크려 기를 모으는 자세
        boss.Telegraph.Show(TelegraphKind.Heavy, ChargeTime);
        Enter(Phase.Charge);
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Charge:
                if (!windowOpened && elapsed >= ChargeTime * (1f - WindowRatio))
                {
                    windowOpened = true;
                    boss.CounterWindowOpen = true;
                    Bounds b = boss.Body.bounds;
                    Fx.Ring(b.center, new Color(0.6f, 0.7f, 1f, 0.9f), b.size.x * 2f, b.size.x * 0.8f, 0.25f, 1f);
                }
                if (elapsed >= ChargeTime)
                {
                    boss.CounterWindowOpen = false;
                    boss.Telegraph.Hide();
                    boss.PlayAction(BossController.AnimParams.Throw);   // 머리를 내밀고 불을 뿜는 자세
                    StartBeam();
                    nextTick = 0f;
                    Enter(Phase.Fire);
                }
                return NodeState.Running;

            case Phase.Fire:
                UpdateBeam(elapsed);
                if (elapsed >= DS.breathDuration)
                {
                    DestroyBeam();
                    breathIndex++;
                    if (breathsLeft > 0) BeginCharge();
                    else Enter(Phase.Recover);
                }
                return NodeState.Running;

            default: // Recover
                return elapsed >= DS.breathRecovery ? NodeState.Success : NodeState.Running;
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    private Vector2 Mouth
    {
        get
        {
            Bounds b = boss.Body.bounds;
            return new Vector2(b.center.x + dir * (b.extents.x + 0.5f), b.center.y + 0.35f);
        }
    }

    private Vector2 BeamCenter => Mouth + new Vector2(dir * DS.breathLength * 0.5f, 0f);

    private void StartBeam()
    {
        Vector2 mouth = Mouth;
        Fx.Burst(mouth, new Vector2(dir, 0.2f), 50f, new Color(1f, 0.7f, 0.2f), 18, 5f, 12f, 0.15f, 0.35f, 0.5f, 0f);

        beam = new GameObject("BreathBeam");
        beamParts.Clear();
        if (DS.breathFrames == null || DS.breathFrames.Length == 0 || DS.breathFrames[0] == null) return;

        // 불줄기 조각을 입에서부터 이어 붙임 (조각 하나가 길이의 1/3)
        int segments = 3;
        float segLength = DS.breathLength / segments;
        float spriteWidth = DS.breathFrames[0].bounds.size.x;
        float scale = spriteWidth > 0f ? segLength / spriteWidth * 1.15f : 1f;
        for (int i = 0; i < segments; i++)
        {
            var part = new GameObject("Part" + i);
            part.transform.SetParent(beam.transform, false);
            part.transform.position = new Vector3(Mouth.x + dir * segLength * (i + 0.5f), Mouth.y, 0f);
            part.transform.localScale = new Vector3(scale * dir, scale * (DS.breathHeight / 1.2f), 1f);
            var sr = part.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 30;
            sr.sprite = DS.breathFrames[i % DS.breathFrames.Length];
            beamParts.Add(sr);
        }
    }

    private void UpdateBeam(float elapsed)
    {
        // 불줄기 모양 : 프레임을 돌리며 끝에서 사라짐
        int frame = Mathf.FloorToInt(elapsed * 14f);
        float alpha = Mathf.Clamp01((DS.breathDuration - elapsed) / 0.15f);
        for (int i = 0; i < beamParts.Count; i++)
        {
            beamParts[i].sprite = DS.breathFrames[(frame + i) % DS.breathFrames.Length];
            beamParts[i].color = new Color(1f, 1f, 1f, alpha);
        }

        if (Time.time < nextTick) return;
        nextTick = Time.time + DS.breathTickInterval;
        boss.DamageArea(BeamCenter, new Vector2(DS.breathLength, DS.breathHeight), DS.breathTickDamage);
        Fx.Burst(Mouth + new Vector2(dir * Random.Range(1f, DS.breathLength), 0f), Vector2.up, 120f,
                 new Color(1f, 0.6f, 0.15f, 0.9f), 4, 1f, 3f, 0.1f, 0.25f, 0.4f, -0.3f, hard: false);
    }

    private void DestroyBeam()
    {
        if (beam != null) Object.Destroy(beam);
        beam = null;
        beamParts.Clear();
    }

    protected override void OnStop()
    {
        boss.CounterWindowOpen = false;
        boss.Telegraph.Hide();
        DestroyBeam();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}

// 화염구 비. 플레이어 주변에 낙하 지점이 하나씩 표시되고(붉은 장판), 위에서 화염구가 떨어짐 (장판 밖으로 벗어나면 회피)
public class FireRainAction : BossAction
{
    private class Drop
    {
        public GroundZone zone;
        public Transform ball;
        public SpriteRenderer ballRenderer;
        public float landTime;
        public float x;
    }

    private const float CastTime = 0.7f;   // 시전 모션
    private const float FallHeight = 12f;

    private readonly List<Drop> drops = new List<Drop>();
    private float startTime;
    private int spawned;
    private int total;
    private float nextSpawn;
    private float groundY;

    private DragonBossStats DS => (DragonBossStats)S;

    public FireRainAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.StopMoving();
        boss.FaceTarget();
        groundY = boss.FeetPosition.y;
        startTime = Time.time;
        spawned = 0;
        total = DS.rainCount + (boss.IsPhase2 ? 2 : 0);
        nextSpawn = startTime + CastTime;
        drops.Clear();

        boss.PlayAction(BossController.AnimParams.Throw);   // 하늘을 향해 불을 뿜는 자세
        boss.Telegraph.Show(TelegraphKind.Bolt, CastTime);
        Bounds b = boss.Body.bounds;
        Fx.Burst(new Vector2(b.center.x, b.max.y), Vector2.up, 40f, new Color(1f, 0.6f, 0.2f), 20, 6f, 14f, 0.15f, 0.35f, 0.8f, 0.4f);
    }

    protected override NodeState OnUpdate()
    {
        float interval = DS.rainInterval * (boss.IsPhase2 ? 0.8f : 1f);

        if (spawned < total && Time.time >= nextSpawn)
        {
            if (spawned == 0) boss.Telegraph.Hide();
            SpawnDrop(spawned == 0);
            spawned++;
            nextSpawn = Time.time + interval;
        }

        for (int i = drops.Count - 1; i >= 0; i--)
        {
            Drop d = drops[i];
            float remaining = d.landTime - Time.time;
            float t = 1f - Mathf.Clamp01(remaining / DS.rainWarnTime);
            // 화염구가 가속하며 낙하
            d.ball.position = new Vector3(d.x, groundY + 0.4f + FallHeight * (1f - t * t), 0f);
            if (remaining <= 0f)
            {
                Explode(d);
                drops.RemoveAt(i);
            }
        }

        return spawned >= total && drops.Count == 0 ? NodeState.Success : NodeState.Running;
    }

    private void SpawnDrop(bool atPlayer)
    {
        float playerX = boss.Target.transform.position.x;
        float x = atPlayer ? playerX : playerX + Random.Range(-DS.rainSpread, DS.rainSpread);

        // 바닥이 없는 곳에는 떨어뜨리지 않음 (낭떠러지 / 벽 밖)
        RaycastHit2D ground = Physics2D.Raycast(new Vector2(x, groundY + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
        if (ground.collider == null) return;
        float y = ground.point.y;

        var drop = new Drop { x = x, landTime = Time.time + DS.rainWarnTime };
        drop.zone = GroundZone.Create(new Vector2(x, y + 0.05f), DS.rainRadius);
        drop.zone.Lock(DS.rainWarnTime);

        var ball = new GameObject("FireBall");
        drop.ball = ball.transform;
        drop.ballRenderer = ball.AddComponent<SpriteRenderer>();
        drop.ballRenderer.sortingOrder = 32;
        if (DS.fireballFrames != null && DS.fireballFrames.Length > 0 && DS.fireballFrames[0] != null)
        {
            drop.ballRenderer.sprite = DS.fireballFrames[0];
            float w = DS.fireballFrames[0].bounds.size.x;
            float scale = w > 0f ? DS.rainRadius * 1.6f / w : 1f;
            ball.transform.localScale = new Vector3(scale, scale, 1f);
            ball.transform.rotation = Quaternion.Euler(0f, 0f, -90f);   // 아래를 향하게
        }
        else
        {
            drop.ballRenderer.sprite = Fx.Circle();
            ball.transform.localScale = Vector3.one * DS.rainRadius;
            drop.ballRenderer.color = new Color(1f, 0.5f, 0.1f);
        }
        drops.Add(drop);
    }

    private void Explode(Drop d)
    {
        float y = d.zone != null ? d.zone.transform.position.y : groundY;

        // 장판 안(발이 바닥 근처)에 있는 대상에게 피해. 구르기 무적 / 점프로 띄우면 회피
        Vector2 center = new Vector2(d.x, y + 0.7f);
        boss.DamageArea(center, new Vector2(DS.rainRadius * 2f, 1.6f), DS.rainDamage);

        Vector2 feet = new Vector2(d.x, y);
        Fx.Ring(feet, new Color(1f, 0.45f, 0.1f, 1f), DS.rainRadius, DS.rainRadius * 2.6f, 0.4f);
        Fx.Burst(feet, Vector2.up, 120f, new Color(1f, 0.6f, 0.15f), 16, 3f, 9f, 0.12f, 0.3f, 0.7f, 1.5f);
        FxSpriteAnim.Play(new Vector3(d.x, y + DS.rainRadius * 0.6f, 0f), DS.explosionFrames, 14f, DS.rainRadius * 2.4f, false, 0f, 33);

        if (d.zone != null) d.zone.Dismiss();
        if (d.ball != null) Object.Destroy(d.ball.gameObject);
    }

    protected override void OnStop()
    {
        foreach (Drop d in drops)
        {
            if (d.zone != null) d.zone.Dismiss();
            if (d.ball != null) Object.Destroy(d.ball.gameObject);
        }
        drops.Clear();
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}

// 붙잡아 내려찍기. 보스 주변에 범위(붉은 장판)가 표시되고 제한 시간이 줄어듦 → 시간 안에 범위 밖으로 나가야 함
// 못 나가면 보스가 덮쳐 붙잡고 → 위로 들어올렸다가 → 땅으로 내려찍음(큰 피해)
// 이후 플레이어와 보스는 서로 반대 방향으로 날아가고, 보스는 그로기, 플레이어는 스태미너가 즉시 0이 됨
// 회피 : 범위 밖으로 벗어나기 / 덮치는 순간 구르기 무적
public class SeizeSlamAction : BossAction
{
    private enum Phase { Warn, Lunge, Lift, Slam, Knock, Miss }

    private const float KnockTime = 0.4f;
    private const float KnockArc = 1.3f;

    private Phase phase;
    private float phaseStart;
    private float dir;
    private float restY, groundY;
    private GroundZone zone;
    private PlayerController held;
    private float savedGravity;
    private bool airborne;
    private float bossStartX, playerStartX, playerLandY;
    private float playerDir, bossKnockDist, playerKnockDist;

    private DragonBossStats DS => (DragonBossStats)S;

    public SeizeSlamAction(BossController boss) : base(boss) { }

    private float TimeLimit => DS.seizeTimeLimit * (boss.IsPhase2 ? 0.8f : 1f);

    protected override void OnStart()
    {
        boss.StopMoving();
        boss.FaceTarget();
        dir = boss.FacingDir;
        restY = boss.transform.position.y;
        groundY = boss.FeetPosition.y;
        held = null;
        airborne = false;

        zone = GroundZone.Create(new Vector2(boss.Body.bounds.center.x, groundY + 0.05f), DS.seizeRadius);
        zone.Lock(TimeLimit);
        boss.PlayAction(BossController.AnimParams.Ability);   // 웅크려 노려보는 자세
        boss.Telegraph.Show(TelegraphKind.Grab, TimeLimit);
        Enter(Phase.Warn);
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Warn:
                if (elapsed < TimeLimit) return NodeState.Running;

                boss.Telegraph.Hide();
                if (zone != null) zone.Dismiss();
                if (!IsTargetCaught())
                {
                    // 범위 밖으로 피함 : 잠깐 멈칫 (반격 기회)
                    boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
                    Enter(Phase.Miss);
                    return NodeState.Running;
                }
                boss.FaceTarget();
                dir = boss.FacingDir;
                boss.PlayAction(BossController.AnimParams.Grab);   // 덮치는 자세
                Fx.Burst(boss.FeetPosition, new Vector2(-dir, 1f), 60f, new Color(0.8f, 0.7f, 0.55f, 0.7f), 8, 2f, 5f, 0.4f, 0.8f, 0.5f, 0f, hard: false);
                Enter(Phase.Lunge);
                return NodeState.Running;

            case Phase.Lunge:
            {
                PlayerController target = boss.Target as PlayerController;
                if (target == null || target.IsDead) return NodeState.Success;

                boss.SetVelocityX(dir * DS.seizeLungeSpeed);
                float gap = Mathf.Abs(target.transform.position.x - boss.Body.bounds.center.x);
                if (gap <= boss.Body.bounds.extents.x + 0.9f)
                {
                    boss.StopMoving();
                    if (!target.IsInvincible) Seize(target);
                    else Enter(Phase.Miss);   // 덮치는 순간 구르기로 피함
                    return NodeState.Running;
                }
                if (elapsed > 0.6f)
                {
                    boss.StopMoving();
                    Enter(Phase.Miss);
                }
                return NodeState.Running;
            }

            case Phase.Lift:
            {
                if (held == null || held.IsDead) return NodeState.Success;
                float t = Mathf.Clamp01(elapsed / DS.seizeLiftTime);
                float eased = 1f - (1f - t) * (1f - t);
                SetBossY(restY + DS.seizeLiftHeight * eased);
                Pin(elapsed);
                if (t >= 1f) Enter(Phase.Slam);
                return NodeState.Running;
            }

            case Phase.Slam:
            {
                if (held == null || held.IsDead) return NodeState.Success;
                float t = Mathf.Clamp01(elapsed / DS.seizeSlamTime);
                SetBossY(Mathf.Lerp(restY + DS.seizeLiftHeight, restY, t * t));
                Pin(elapsed);
                if (t >= 1f) Impact();
                return NodeState.Running;
            }

            case Phase.Knock:
            {
                float t = Mathf.Clamp01(elapsed / KnockTime);
                float eased = 1f - (1f - t) * (1f - t);
                if (held != null)
                {
                    Rigidbody2D prb = held.Rb;
                    prb.velocity = Vector2.zero;
                    prb.position = new Vector2(playerStartX + playerDir * playerKnockDist * eased,
                                               playerLandY + KnockArc * Mathf.Sin(t * Mathf.PI));
                }
                boss.transform.position = new Vector3(bossStartX - playerDir * bossKnockDist * eased, restY, boss.transform.position.z);
                if (t >= 1f)
                {
                    RestoreAll();
                    boss.StaggerFor(DS.seizeBossStagger);   // 트리를 중단시킴 (이 노드의 OnStop 호출)
                    return NodeState.Success;
                }
                return NodeState.Running;
            }

            default: // Miss
                return elapsed >= DS.seizeMissRecovery ? NodeState.Success : NodeState.Running;
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    // 범위 안(x 거리 + 발 높이)에 있고 구르기 무적이 아니면 잡힘
    private bool IsTargetCaught()
    {
        PlayerController player = boss.Target as PlayerController;
        if (player == null || player.IsDead || player.IsInvincible) return false;

        float dx = Mathf.Abs(player.transform.position.x - zone.X);
        bool nearGround = player.BodyCollider.bounds.min.y < groundY + 2.5f;
        return dx <= DS.seizeRadius && nearGround;
    }

    private void Seize(PlayerController player)
    {
        held = player;
        held.InputLocked = true;
        savedGravity = held.Rb.gravityScale;
        held.Rb.gravityScale = 0f;
        held.Rb.velocity = Vector2.zero;

        // 공중에서는 코드로 이동
        boss.Rb.bodyType = RigidbodyType2D.Kinematic;
        boss.Rb.velocity = Vector2.zero;
        airborne = true;

        boss.PlayAction(DragonBoss.DragonAnim.Lift);
        Fx.Burst(player.transform.position, Vector2.up, 360f, new Color(1f, 0.6f, 0.2f), 14, 2f, 6f, 0.1f, 0.25f, 0.4f, 0.5f);
        Enter(Phase.Lift);
    }

    // 잡힌 동안 보스 앞쪽(발톱 위치)에 고정
    private void Pin(float elapsed)
    {
        Bounds b = boss.Body.bounds;
        float shake = Mathf.Sin(elapsed * 55f) * 0.07f;
        held.Rb.velocity = Vector2.zero;
        held.Rb.position = new Vector2(b.center.x + dir * (b.extents.x + 0.5f) + shake, b.min.y + 0.1f);
    }

    private void SetBossY(float y)
    {
        boss.transform.position = new Vector3(boss.transform.position.x, y, boss.transform.position.z);
        boss.Rb.velocity = Vector2.zero;
    }

    private void Impact()
    {
        SetBossY(restY);
        PlayerController target = held;
        playerLandY = target.Rb.position.y;
        playerStartX = target.Rb.position.x;
        bossStartX = boss.transform.position.x;

        target.Rb.gravityScale = savedGravity;
        target.TakeDamage(DS.seizeDamage);
        target.DrainStamina();   // 스태미너 즉시 0

        // 내려찍는 연출
        Vector2 feet = new Vector2(playerStartX, groundY);
        Fx.Ring(feet, new Color(1f, 0.4f, 0.15f, 1f), 2f, 9f, 0.45f);
        Fx.Ring(feet, new Color(1f, 0.85f, 0.4f, 0.9f), 1.2f, 6f, 0.3f);
        Fx.Burst(feet, Vector2.up, 150f, new Color(0.55f, 0.4f, 0.3f), 30, 5f, 12f, 0.15f, 0.4f, 1.1f, 2.5f);
        Fx.Burst(feet, Vector2.up, 170f, new Color(0.8f, 0.7f, 0.55f, 0.7f), 18, 1.5f, 5f, 0.6f, 1.1f, 0.8f, 0f, hard: false);

        // 서로 반대 방향으로 날아감 : 플레이어는 보스 반대쪽, 보스는 플레이어 쪽의 반대. 벽이 있으면 그 앞까지만
        playerDir = dir;   // 플레이어는 보스가 바라보던 방향으로 날아감
        playerKnockDist = WallLimited(target.Rb.position, playerDir, DS.seizePlayerKnockback, 0.6f);
        bossKnockDist = WallLimited(boss.Body.bounds.center, -playerDir, DS.seizeBossKnockback, boss.Body.bounds.extents.x + 0.2f);
        Enter(Phase.Knock);
    }

    private static float WallLimited(Vector2 origin, float direction, float distance, float margin)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(direction, 0f), distance + margin, LayerMask.GetMask("Ground"));
        return hit.collider != null ? Mathf.Max(0f, hit.distance - margin) : distance;
    }

    // 공중 상태 / 잡기 해제 : 물리, 중력, 입력 잠금 복구
    private void RestoreAll()
    {
        if (held != null)
        {
            held.Rb.gravityScale = savedGravity;
            held.InputLocked = false;
            held = null;
        }
        if (airborne)
        {
            airborne = false;
            boss.transform.position = new Vector3(boss.transform.position.x, restY, boss.transform.position.z);
            boss.Rb.bodyType = RigidbodyType2D.Dynamic;
            boss.Rb.velocity = Vector2.zero;
        }
    }

    protected override void OnStop()
    {
        RestoreAll();
        if (zone != null) zone.Dismiss();
        zone = null;
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
        boss.Anim.ResetTrigger(BossController.AnimParams.Grab);
        boss.Anim.ResetTrigger(DragonBoss.DragonAnim.Lift);
    }
}
