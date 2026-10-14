using System.Collections.Generic;
using UnityEngine;

// 하데스 2형태 전용 행동 노드 (1형태에는 없는 더 어렵고 다양한 패턴)

// 돌진 연타. 플레이어 쪽으로 파고들며 3연속 타격. 한 번 맞고 끝나지 않고 계속 따라붙으므로
// 첫 타격을 구르기(무적)로 빠져나가거나, 타격 사이에 거리를 벌려야 함
public class HadesComboAction : BossAction
{
    private enum Phase { Lunge, Swing }

    private Phase phase;
    private float phaseStart;
    private int hitIndex;
    private bool swingHit;
    private float lungeDir;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;
    private float Speed => boss.AnimSpeed;

    public HadesComboAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 돌진 연타");
        hitIndex = 0;
        BeginLunge();
        boss.Telegraph.Show(TelegraphKind.Melee, HS.comboLungeTime);
    }

    private void BeginLunge()
    {
        boss.FaceTarget();
        lungeDir = boss.FacingDir;
        phase = Phase.Lunge;
        phaseStart = Time.time;
        swingHit = false;
        // 이동 중 모션(걷기)으로 파고듦
        boss.Anim.SetBool(BossController.AnimParams.Moving, true);
    }

    private void BeginSwing()
    {
        boss.StopMoving();
        boss.FaceTarget();
        phase = Phase.Swing;
        phaseStart = Time.time;
        swingHit = false;
        // 홀수 번째는 두 번째 모션으로 번갈아 (같은 트리거 연속 재생은 애니메이터가 무시)
        boss.PlayAction(hitIndex % 2 == 0 ? BossController.AnimParams.Melee : BossController.AnimParams.Slam);
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;
        float swingTime = HS.comboHitDuration / Speed;

        if (phase == Phase.Lunge)
        {
            // 플레이어가 이미 가까우면 제자리에서 바로 휘두름
            bool close = boss.DistanceToTarget <= HS.comboReach * 0.5f;
            if (!close && elapsed < HS.comboLungeTime)
            {
                boss.SetVelocityX(lungeDir * HS.comboLungeSpeed);
                return NodeState.Running;
            }
            boss.Telegraph.Hide();
            BeginSwing();
            return NodeState.Running;
        }

        // Swing
        if (!swingHit && elapsed >= swingTime * 0.5f)
        {
            swingHit = true;
            Bounds b = boss.Body.bounds;
            float dir = boss.FacingDir;
            boss.DamageArea(new Vector2(b.center.x + dir * HS.comboReach * 0.5f, b.center.y),
                new Vector2(HS.comboReach, b.size.y), boss.AttackDamage * HS.comboDamageRatio);

            Vector2 point = new Vector2(b.center.x + dir * HS.comboReach * 0.7f, b.min.y + 0.6f);
            Fx.Ring(point, Hades.FxColor, 0.5f, 2.2f, 0.25f);
            Fx.Burst(point, Vector2.up, 120f, Hades.FxColor, 12, 3f, 8f, 0.1f, 0.25f, 0.5f, 1f);
        }

        if (elapsed >= swingTime)
        {
            hitIndex++;
            if (hitIndex >= HS.comboHits) return NodeState.Success;
            BeginLunge();
        }
        return NodeState.Running;
    }

    protected override void OnStop()
    {
        boss.StopMoving();
        boss.Telegraph.Hide();
        boss.Anim.ResetTrigger(BossController.AnimParams.Melee);
        boss.Anim.ResetTrigger(BossController.AnimParams.Slam);
    }
}

// 암흑 낙뢰. 맵 곳곳에 작은 장판이 연달아 찍히고, 경고 시간이 지나면 하늘에서 기둥이 내리꽂힘
// 처음 몇 발은 플레이어 위치를 노리고 나머지는 맵 전체에 흩뿌려져서 계속 움직여야 함 (구르기 무적 / 점프 불가 : 기둥은 높이가 있음)
public class HadesSkyStrikeAction : BossAction
{
    private class Strike
    {
        public GroundZone zone;
        public float x, y;
        public float time;
        public bool done;
    }

    private const float CastTime = 0.7f;

    private readonly List<Strike> strikes = new List<Strike>();
    private float startTime;
    private float nextSpawn;
    private int spawned;
    private float groundY;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesSkyStrikeAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 암흑 낙뢰");
        boss.StopMoving();
        boss.FaceTarget();
        strikes.Clear();
        spawned = 0;
        groundY = boss.FeetPosition.y;
        startTime = Time.time;
        float cast = CastTime / boss.AnimSpeed;
        nextSpawn = startTime + cast;

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Bolt, cast);
        Bounds b = boss.Body.bounds;
        Fx.Burst(new Vector2(b.center.x, b.max.y), Vector2.up, 40f, Hades.FxColor, 24, 6f, 14f, 0.15f, 0.35f, 0.8f, 0.4f);
    }

    protected override NodeState OnUpdate()
    {
        if (spawned < HS.skyStrikes && Time.time >= nextSpawn)
        {
            if (spawned == 0) boss.Telegraph.Hide();
            Spawn(spawned < HS.skyAimed);
            spawned++;
            nextSpawn = Time.time + HS.skyInterval;
        }

        bool allDone = spawned >= HS.skyStrikes;
        foreach (Strike s in strikes)
        {
            if (s.done) continue;
            allDone = false;
            if (Time.time >= s.time) Hit(s);
        }
        return allDone ? NodeState.Success : NodeState.Running;
    }

    private void Spawn(bool aimed)
    {
        float playerX = boss.Target.transform.position.x;
        float x = aimed
            ? playerX + Random.Range(-1.2f, 1.2f)
            : Random.Range(HS.arenaMinX, HS.arenaMaxX);

        RaycastHit2D ground = Physics2D.Raycast(new Vector2(x, groundY + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
        if (ground.collider == null) return;

        var s = new Strike { x = x, y = ground.point.y, time = Time.time + HS.skyWarnTime };
        s.zone = GroundZone.Create(new Vector2(x, s.y + 0.05f), HS.skyRadius);
        s.zone.SetColors(Hades.ZoneIdleColor, Hades.ZoneLockedColor);
        s.zone.Lock(HS.skyWarnTime);
        strikes.Add(s);
    }

    private void Hit(Strike s)
    {
        s.done = true;
        // 기둥은 하늘에서 바닥까지 닿으므로 점프로도 피할 수 없음 (구르기 무적 / 범위 밖으로 이동)
        boss.DamageArea(new Vector2(s.x, s.y + 3f), new Vector2(HS.skyRadius * 2f, 7f), HS.skyDamage);

        FxBeam.Spawn(new Vector2(s.x, s.y + 7f), new Vector2(HS.skyRadius * 1.6f, 14f), new Color(Hades.FxColor.r, Hades.FxColor.g, Hades.FxColor.b, 0.85f), 0.25f);
        FxBeam.Spawn(new Vector2(s.x, s.y + 7f), new Vector2(HS.skyRadius * 0.7f, 14f), new Color(1f, 1f, 1f, 0.9f), 0.18f, 35);
        Vector2 feet = new Vector2(s.x, s.y);
        Fx.Ring(feet, Hades.FxColor, HS.skyRadius, HS.skyRadius * 2.4f, 0.3f);
        Fx.Burst(feet, Vector2.up, 60f, Hades.FxColor, 14, 5f, 11f, 0.12f, 0.3f, 0.4f, 1f);
        if (s.zone != null) s.zone.Dismiss();
    }

    protected override void OnStop()
    {
        foreach (Strike s in strikes)
            if (s.zone != null) s.zone.Dismiss();
        strikes.Clear();
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}

// 암흑 광선. 바닥 높이의 가로 광선이 양쪽으로 길게 뻗어 일정 시간 유지됨 (경고선 → 발사)
// 회피 : 점프로 넘기 / 구르기 무적으로 통과. 광선이 닿는 동안은 일정 간격으로 계속 피해를 줌
public class HadesBeamAction : BossAction
{
    private enum Phase { Warn, Fire }

    private Phase phase;
    private float phaseStart;
    private float nextTick;
    private float boltY;
    private GameObject warnLeft, warnRight;
    private SpriteRenderer warnLeftSr, warnRightSr;
    private float originX;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesBeamAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 암흑 광선");
        boss.StopMoving();
        boss.FaceTarget();
        phase = Phase.Warn;
        phaseStart = Time.time;
        originX = boss.Body.bounds.center.x;
        boltY = boss.FeetPosition.y + HS.beamHeightAboveGround;

        boss.PlayAction(BossController.AnimParams.Throw);
        float warn = HS.beamWarnTime / boss.AnimSpeed;
        boss.Telegraph.Show(TelegraphKind.Heavy, warn);

        // 경고선 : 양쪽으로 길게 뻗는 얇은 선 (깜빡임)
        warnRight = CreateLine(1f, out warnRightSr);
        warnLeft = CreateLine(-1f, out warnLeftSr);
    }

    private GameObject CreateLine(float dir, out SpriteRenderer sr)
    {
        var go = new GameObject("HadesBeamWarn");
        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Fx.Square();
        sr.sortingOrder = 30;
        go.transform.position = new Vector3(originX + dir * HS.beamLength * 0.5f, boltY, 0f);
        go.transform.localScale = new Vector3(HS.beamLength, 0.12f, 1f);
        return go;
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        if (phase == Phase.Warn)
        {
            float warn = HS.beamWarnTime / boss.AnimSpeed;
            float blink = 0.35f + 0.5f * Mathf.Abs(Mathf.Sin(elapsed * 14f));
            Color c = new Color(Hades.FxColor.r, Hades.FxColor.g, Hades.FxColor.b, blink);
            warnLeftSr.color = c;
            warnRightSr.color = c;
            if (elapsed >= warn)
            {
                boss.Telegraph.Hide();
                Fire();
            }
            return NodeState.Running;
        }

        // Fire : 일정 간격으로 광선 범위에 피해
        if (Time.time >= nextTick)
        {
            nextTick = Time.time + HS.beamTickInterval;
            DamageTick();
        }
        return elapsed >= HS.beamDuration ? NodeState.Success : NodeState.Running;
    }

    private void Fire()
    {
        phase = Phase.Fire;
        phaseStart = Time.time;
        nextTick = Time.time;
        DestroyLines();

        // 발사 연출 : 굵고 밝은 광선 + 중심의 흰 심지
        Color c = Hades.FxColor;
        float h = HS.beamHeight;
        for (float dir = -1f; dir <= 1f; dir += 2f)
        {
            Vector2 center = new Vector2(originX + dir * HS.beamLength * 0.5f, boltY);
            FxBeam.Spawn(center, new Vector2(HS.beamLength, h), new Color(c.r, c.g, c.b, 0.55f), HS.beamDuration);
            FxBeam.Spawn(center, new Vector2(HS.beamLength, h * 0.35f), new Color(1f, 1f, 1f, 0.8f), HS.beamDuration, 35);
        }
        Bounds b = boss.Body.bounds;
        Fx.Burst(new Vector2(b.center.x, boltY), Vector2.up, 360f, c, 22, 4f, 10f, 0.12f, 0.3f, 0.4f, 0.8f);
    }

    private void DamageTick()
    {
        float h = HS.beamHeight;
        for (float dir = -1f; dir <= 1f; dir += 2f)
        {
            Vector2 center = new Vector2(originX + dir * HS.beamLength * 0.5f, boltY);
            boss.DamageArea(center, new Vector2(HS.beamLength, h), HS.beamDamage);
        }
    }

    private void DestroyLines()
    {
        if (warnLeft != null) Object.Destroy(warnLeft);
        if (warnRight != null) Object.Destroy(warnRight);
    }

    protected override void OnStop()
    {
        DestroyLines();
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}
