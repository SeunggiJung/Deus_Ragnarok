using System.Collections.Generic;
using UnityEngine;

// 하데스의 화려한 대형 패턴들 : 회전 탄막 / 공허의 소용돌이 / 심판

// 회전 탄막. 시전 뒤 보스를 중심으로 소용돌이치며 탄이 계속 뻗어나가고, 중간에 회전 방향이 뒤집힘
// 탄 사이의 틈을 보고 점프 / 구르기로 빠져나가야 함. 2형태는 팔이 3갈래로 더 촘촘
public class HadesSpiralNovaAction : BossAction
{
    private float startTime;
    private float fireStart;
    private float nextFire;
    private float angle;
    private float turnDir;
    private bool reversed;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesSpiralNovaAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 회전 탄막");
        boss.StopMoving();
        boss.FaceTarget();
        startTime = Time.time;
        float cast = HS.novaCastTime / boss.AnimSpeed;
        fireStart = startTime + cast;
        nextFire = fireStart;
        angle = Random.Range(0f, 360f);
        turnDir = Random.value < 0.5f ? 1f : -1f;
        reversed = false;

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Bolt, cast);
        Bounds b = boss.Body.bounds;
        Fx.Ring(b.center, Hades.FxColor, b.size.x * 2.5f, b.size.x * 0.5f, cast, 1f);   // 안쪽으로 모여드는 링
    }

    protected override NodeState OnUpdate()
    {
        if (Time.time < fireStart) return NodeState.Running;
        boss.Telegraph.Hide();

        float elapsed = Time.time - fireStart;
        // 절반이 지나면 회전 방향 반전 (한 방향에 익숙해진 타이밍을 흔듦)
        if (!reversed && elapsed >= HS.novaDuration * 0.5f)
        {
            reversed = true;
            turnDir = -turnDir;
            Bounds rb = boss.Body.bounds;
            Fx.Ring(rb.center, Hades.FxColor, 1f, rb.size.x * 3f, 0.4f, 1f);
        }

        angle += turnDir * HS.novaTurnSpeed * Time.deltaTime;

        int arms = Hades.Revived ? HS.form2NovaArms : HS.novaArms;
        while (Time.time >= nextFire)
        {
            nextFire += HS.novaInterval;
            Fire(arms);
        }
        return elapsed >= HS.novaDuration ? NodeState.Success : NodeState.Running;
    }

    private void Fire(int arms)
    {
        Vector2 origin = boss.Body.bounds.center;
        for (int i = 0; i < arms; i++)
        {
            float a = angle + 360f * i / arms;
            Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
            ProjectilePool.Instance.Get().Launch(origin, dir, HS.novaBoltSpeed, HS.novaDamage, 4f,
                boss.whatIsEnemy, LayerMask.GetMask("Ground"), null, Hades.FxColor, HS.novaBoltRadius);
        }
    }

    protected override void OnStop()
    {
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}

// 공허의 소용돌이. 보스 위에 검은 구체가 커지며 플레이어를 끌어당기고(경고 장판이 차오름),
// 다 차면 보스 주변이 크게 폭발. 끌려오는 속도가 플레이어 이동 속도와 비슷해서 제자리에 서 있으면 폭발 범위로 끌려옴
// 회피 : 반대쪽으로 계속 달려 범위 밖에 머물기 / 폭발 순간 구르기 무적
public class HadesVoidVortexAction : BossAction
{
    private enum Phase { Charge, Pull }

    private Phase phase;
    private float phaseStart;
    private GameObject orb;
    private SpriteRenderer orbSr;
    private GroundZone zone;
    private ParticleSystem gather;
    private float nextRing;
    private float centerX, groundY;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;
    private float PullSpeed => Hades.Revived ? HS.form2VortexPullSpeed : HS.vortexPullSpeed;

    public HadesVoidVortexAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 공허의 소용돌이");
        boss.StopMoving();
        boss.FaceTarget();
        phase = Phase.Charge;
        phaseStart = Time.time;
        Bounds b = boss.Body.bounds;
        centerX = b.center.x;
        groundY = boss.FeetPosition.y;

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Heavy, HS.vortexChargeTime / boss.AnimSpeed);

        // 검은 구체 + 안쪽으로 빨려드는 입자
        orb = new GameObject("HadesVoidOrb");
        orb.transform.position = new Vector3(centerX, b.max.y + 1f, 0f);
        orbSr = orb.AddComponent<SpriteRenderer>();
        orbSr.sprite = Fx.Circle();
        orbSr.sortingOrder = 36;
        orbSr.color = new Color(0.08f, 0f, 0.15f, 0.95f);
        orb.transform.localScale = Vector3.one * 0.5f;
        var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(orb.transform, false);
        glow.transform.localScale = Vector3.one * 1.5f;
        glow.sprite = Fx.Ring();
        glow.sortingOrder = 37;
        glow.color = Hades.FxColor;

        gather = Fx.Gather(Hades.FxColor, 5f, 0.9f, 45f, 0.35f);
        gather.transform.position = orb.transform.position;
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;
        float chargeTime = HS.vortexChargeTime / boss.AnimSpeed;

        if (phase == Phase.Charge)
        {
            orb.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 2.4f, elapsed / chargeTime);
            if (elapsed >= chargeTime)
            {
                boss.Telegraph.Hide();
                zone = GroundZone.Create(new Vector2(centerX, groundY + 0.05f), HS.vortexRadius);
                zone.SetColors(Hades.ZoneIdleColor, Hades.ZoneLockedColor);
                zone.Lock(HS.vortexPullTime);
                phase = Phase.Pull;
                phaseStart = Time.time;
                nextRing = Time.time;
            }
            return NodeState.Running;
        }

        // Pull : 플레이어를 보스 쪽으로 끌어당김 (이동 입력 위에 위치를 더해서 밀어냄)
        boss.FaceTarget();
        PullPlayer();
        orb.transform.localScale = Vector3.one * (2.4f + 0.25f * Mathf.Sin(elapsed * 18f));
        if (Time.time >= nextRing)
        {
            nextRing = Time.time + 0.3f;
            Fx.Ring(orb.transform.position, Hades.FxColor, HS.vortexRadius * 2.6f, 1.5f, 0.55f, 1f);   // 안쪽으로 수축하는 링
        }

        if (elapsed >= HS.vortexPullTime)
        {
            Blast();
            return NodeState.Success;
        }
        return NodeState.Running;
    }

    private void PullPlayer()
    {
        var player = boss.Target as PlayerController;
        if (player == null || player.IsDead) return;
        // 물리 위치(Rb.position) 기준으로 계산 : transform은 물리 갱신 전까지 옛 값이라 섞어 쓰면 어긋남
        Vector2 pos = player.Rb.position;
        float side = pos.x >= centerX ? 1f : -1f;           // 플레이어가 있는 쪽
        float stopX = centerX + side * 1.5f;                // 보스와 겹치기 직전까지만 (보스를 지나치지 않음)
        if ((pos.x - stopX) * side <= 0f) return;

        pos.x = Mathf.MoveTowards(pos.x, stopX, PullSpeed * Time.deltaTime);
        pos.x = Mathf.Clamp(pos.x, Hades.HadesStats.arenaMinX, Hades.HadesStats.arenaMaxX);   // 방 밖으로 끌려 나가지 않음
        player.Rb.position = pos;
    }

    private void Blast()
    {
        // 범위 안이면 높이와 상관없이 피해 (구르기 무적으로만 회피)
        boss.DamageArea(new Vector2(centerX, groundY + 4f), new Vector2(HS.vortexRadius * 2f, 9f), HS.vortexDamage);

        Vector2 feet = new Vector2(centerX, groundY);
        Fx.Ring(feet, Hades.FxColor, HS.vortexRadius * 0.5f, HS.vortexRadius * 2.4f, 0.5f);
        Fx.Ring(feet, Hades.FxDarkColor, HS.vortexRadius * 0.3f, HS.vortexRadius * 1.8f, 0.4f);
        Fx.Burst(orb.transform.position, Vector2.up, 360f, Hades.FxColor, 40, 6f, 16f, 0.2f, 0.5f, 0.8f, 0.3f);
        Fx.Burst(feet, Vector2.up, 120f, Hades.FxDarkColor, 24, 5f, 13f, 0.25f, 0.55f, 0.7f, 1f);
        FxBeam.Spawn(new Vector2(centerX, groundY + 6f), new Vector2(HS.vortexRadius * 1.6f, 12f),
            new Color(Hades.FxColor.r, Hades.FxColor.g, Hades.FxColor.b, 0.6f), 0.3f);
    }

    private void Cleanup()
    {
        if (orb != null) Object.Destroy(orb);
        if (gather != null)
        {
            gather.Stop();
            Object.Destroy(gather.gameObject, 1f);
        }
        if (zone != null) zone.Dismiss();
        orb = null; gather = null; zone = null;
    }

    protected override void OnStop()
    {
        Cleanup();
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}

// 심판 (2형태 전용). 하데스가 팔을 들면 바닥 전체가 붉게 물들고 초록색 안전지대가 몇 곳만 남음
// 경고 시간이 끝나면 안전지대 밖은 전부 폭발 → 제한 시간 안에 안전지대로 이동해야 함 (구르기 무적으로도 회피 가능)
public class HadesJudgementAction : BossAction
{
    private readonly List<GroundZone> safeZones = new List<GroundZone>();
    private readonly List<float> safeX = new List<float>();
    private GameObject floor;
    private SpriteRenderer floorSr;
    private float startTime;
    private float groundY;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesJudgementAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 심판");
        boss.StopMoving();
        boss.FaceTarget();
        startTime = Time.time;
        groundY = boss.FeetPosition.y;
        safeZones.Clear();
        safeX.Clear();

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Heavy, 0.8f / boss.AnimSpeed);

        // 바닥 전체를 덮는 경고 (맥동하는 붉은 띠)
        float width = HS.arenaMaxX - HS.arenaMinX + 6f;
        floor = new GameObject("HadesJudgementFloor");
        floor.transform.position = new Vector3((HS.arenaMinX + HS.arenaMaxX) * 0.5f, groundY + 0.35f, 0f);
        floor.transform.localScale = new Vector3(width, 0.7f, 1f);
        floorSr = floor.AddComponent<SpriteRenderer>();
        floorSr.sprite = Fx.Square();
        floorSr.sortingOrder = 8;
        floorSr.color = new Color(0.9f, 0.1f, 0.2f, 0.35f);

        PlaceSafeZones();
    }

    // 안전지대 : 플레이어 기준 양쪽으로 서로 떨어뜨려 배치 (한 곳은 가깝고 한 곳은 멀리)
    private void PlaceSafeZones()
    {
        float px = boss.Target.transform.position.x;
        float minX = HS.arenaMinX + HS.judgementSafeRadius;
        float maxX = HS.arenaMaxX - HS.judgementSafeRadius;
        float side = Random.value < 0.5f ? -1f : 1f;

        for (int i = 0; i < HS.judgementSafeZones; i++)
        {
            float x = px + side * Random.Range(3f, 6f) * (i + 1);
            x = Mathf.Clamp(x, minX, maxX);
            // 이미 배치한 안전지대와 겹치면 반대편으로
            foreach (float other in safeX)
                if (Mathf.Abs(x - other) < HS.judgementSafeRadius * 2.5f)
                    x = Mathf.Clamp(px - side * Random.Range(3f, 6f) * (i + 1), minX, maxX);
            side = -side;

            RaycastHit2D ground = Physics2D.Raycast(new Vector2(x, groundY + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
            float y = ground.collider != null ? ground.point.y : groundY;

            var zone = GroundZone.Create(new Vector2(x, y + 0.08f), HS.judgementSafeRadius);
            zone.SetColors(new Color(0.3f, 1f, 0.5f), new Color(0.3f, 1f, 0.5f));   // 초록 = 안전
            safeZones.Add(zone);
            safeX.Add(x);
        }
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - startTime;
        float warn = HS.judgementWarnTime;

        // 경고 : 바닥이 점점 빠르게 깜빡임
        float speed = Mathf.Lerp(6f, 28f, elapsed / warn);
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * speed);
        floorSr.color = new Color(0.9f, 0.1f + 0.1f * pulse, 0.2f, 0.25f + 0.35f * pulse);
        if (elapsed >= 0.8f / boss.AnimSpeed) boss.Telegraph.Hide();

        if (elapsed < warn) return NodeState.Running;

        Detonate();
        return NodeState.Success;
    }

    private void Detonate()
    {
        // 플레이어가 안전지대 밖이면 피해 (구르기 무적이면 TakeDamage가 무시)
        var target = boss.Target;
        if (target != null && !target.IsDead)
        {
            float px = target.transform.position.x;
            bool safe = false;
            foreach (float x in safeX)
                if (Mathf.Abs(px - x) <= HS.judgementSafeRadius) { safe = true; break; }
            if (!safe) target.TakeDamage(HS.judgementDamage);
        }

        // 연출 : 안전지대를 뺀 바닥 곳곳에서 기둥이 솟음
        Color c = Hades.FxColor;
        for (float x = HS.arenaMinX - 2f; x <= HS.arenaMaxX + 2f; x += 1.6f)
        {
            bool inSafe = false;
            foreach (float sx in safeX)
                if (Mathf.Abs(x - sx) <= HS.judgementSafeRadius) { inSafe = true; break; }
            if (inSafe) continue;

            FxBeam.Spawn(new Vector2(x, groundY + 6f), new Vector2(1.5f, 12f), new Color(c.r, c.g, c.b, 0.7f), 0.4f);
            Fx.Burst(new Vector2(x, groundY), Vector2.up, 50f, c, 6, 5f, 12f, 0.15f, 0.35f, 0.6f, 1f);
        }
        FxBeam.Spawn(new Vector2((HS.arenaMinX + HS.arenaMaxX) * 0.5f, groundY + 0.4f),
            new Vector2(HS.arenaMaxX - HS.arenaMinX + 6f, 0.9f), new Color(1f, 1f, 1f, 0.8f), 0.3f, 35);
        Cleanup();
    }

    private void Cleanup()
    {
        if (floor != null) Object.Destroy(floor);
        foreach (var z in safeZones)
            if (z != null) z.Dismiss();
        safeZones.Clear();
        floor = null;
    }

    protected override void OnStop()
    {
        Cleanup();
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}
