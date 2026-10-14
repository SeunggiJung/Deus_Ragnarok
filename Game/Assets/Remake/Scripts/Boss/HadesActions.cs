using System.Collections.Generic;
using UnityEngine;

// 하데스 보스 전용 행동 노드

// 마법 타격. 공격 모션 중간에 앞쪽 범위로 피해 (원작 : 공격 범위 6 안이면 피해). 구르기 무적 / 거리 벌리기로 회피
// 2형태에서는 두 가지 공격 모션(attack / attack2)을 번갈아 사용
public class HadesMeleeAction : BossAction
{
    private float startTime;
    private float duration;
    private bool hit;
    private bool useSecondMotion;
    private string trigger;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesMeleeAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 마법 타격");
        boss.StopMoving();
        boss.FaceTarget();
        hit = false;
        startTime = Time.time;

        // 2형태 : 모션을 번갈아 사용 (애니메이터의 melee / slam 트리거가 각각 attack / attack2)
        trigger = Hades.Revived && useSecondMotion ? BossController.AnimParams.Slam : BossController.AnimParams.Melee;
        if (Hades.Revived) useSecondMotion = !useSecondMotion;

        duration = HS.meleeDuration * (Hades.Revived ? 0.8f : 1f) / boss.AnimSpeed;
        boss.PlayAction(trigger);
        boss.Telegraph.Show(TelegraphKind.Melee, duration * HS.meleeHitRatio);
    }

    protected override NodeState OnUpdate()
    {
        float t = Time.time - startTime;
        if (!hit && t >= duration * HS.meleeHitRatio)
        {
            hit = true;
            boss.Telegraph.Hide();

            Bounds b = boss.Body.bounds;
            float dir = boss.FacingDir;
            var center = new Vector2(b.center.x + dir * HS.meleeReach * 0.5f, b.center.y);
            boss.DamageArea(center, new Vector2(HS.meleeReach, b.size.y), boss.AttackDamage);

            Vector2 hitPoint = new Vector2(b.center.x + dir * HS.meleeReach * 0.7f, b.min.y + 0.6f);
            Fx.Ring(hitPoint, Hades.FxColor, 0.6f, 2.4f, 0.3f);
            Fx.Burst(hitPoint, Vector2.up, 120f, Hades.FxColor, 14, 3f, 8f, 0.1f, 0.25f, 0.6f, 1.2f);
        }
        return t >= duration ? NodeState.Success : NodeState.Running;
    }

    protected override void OnStop()
    {
        boss.Telegraph.Hide();
        boss.Anim.ResetTrigger(BossController.AnimParams.Melee);
        boss.Anim.ResetTrigger(BossController.AnimParams.Slam);
    }
}

// 화염탄 비. 시전 모션 뒤 바닥 곳곳에 낙하 지점(장판)이 하나씩 생기고, 경고 시간이 지나면 탄이 떨어져 폭발
// 원작처럼 맵 전체(arenaMinX ~ arenaMaxX)에 흩뿌리되, 첫 한 발은 플레이어 위치를 노림.
// 1형태는 주황 화염탄, 2형태(검은 로브 + 보라 불꽃)는 보라 암흑탄
// 회피 : 장판 밖으로 이동 / 폭발 순간 구르기 무적 / 점프
public class HadesBombAction : BossAction
{
    private class Drop
    {
        public GroundZone zone;
        public Transform ball;
        public float landTime;
        public float x;
    }

    private const float FallHeight = 12f;

    private readonly List<Drop> drops = new List<Drop>();
    private float startTime;
    private float nextSpawn;
    private int spawned;
    private int total;
    private float groundY;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;
    private float Damage => Hades.Revived ? HS.form2BombDamage : HS.bombDamage;

    public HadesBombAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 화염탄 비");
        boss.StopMoving();
        boss.FaceTarget();
        groundY = boss.FeetPosition.y;
        startTime = Time.time;
        spawned = 0;
        total = Hades.Revived ? HS.form2BombCount : HS.bombCount;
        nextSpawn = startTime + HS.bombCastTime / boss.AnimSpeed;
        drops.Clear();

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Bolt, HS.bombCastTime / boss.AnimSpeed);
        Bounds b = boss.Body.bounds;
        Fx.Burst(new Vector2(b.center.x, b.max.y), Vector2.up, 40f, Hades.FxColor, 20, 6f, 14f, 0.15f, 0.35f, 0.8f, 0.4f);
    }

    protected override NodeState OnUpdate()
    {
        float interval = HS.bombInterval * (boss.IsPhase2 ? 0.8f : 1f);

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
            float t = 1f - Mathf.Clamp01(remaining / HS.bombWarnTime);
            d.ball.position = new Vector3(d.x, groundY + 0.4f + FallHeight * (1f - t * t), 0f);   // 가속하며 낙하
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
        float x = atPlayer
            ? boss.Target.transform.position.x
            : Random.Range(HS.arenaMinX, HS.arenaMaxX);

        // 바닥이 없는 곳에는 떨어뜨리지 않음
        RaycastHit2D ground = Physics2D.Raycast(new Vector2(x, groundY + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
        if (ground.collider == null) return;
        float y = ground.point.y;

        var drop = new Drop { x = x, landTime = Time.time + HS.bombWarnTime };
        drop.zone = GroundZone.Create(new Vector2(x, y + 0.05f), HS.bombRadius);
        drop.zone.SetColors(Hades.ZoneIdleColor, Hades.ZoneLockedColor);
        drop.zone.Lock(HS.bombWarnTime);

        var ball = new GameObject("HadesFireBomb");
        drop.ball = ball.transform;
        var sr = ball.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 32;
        // 1형태 : 주황 화염구 스프라이트 / 2형태 : 보라 암흑구 (코드 생성)
        if (!Hades.Revived && HS.fireballFrames != null && HS.fireballFrames.Length > 0 && HS.fireballFrames[0] != null)
        {
            sr.sprite = HS.fireballFrames[0];
            float w = HS.fireballFrames[0].bounds.size.x;
            float scale = w > 0f ? HS.bombRadius * 1.6f / w : 1f;
            ball.transform.localScale = new Vector3(scale, scale, 1f);
            ball.transform.rotation = Quaternion.Euler(0f, 0f, -90f);   // 아래를 향하게
        }
        else
        {
            sr.sprite = Fx.Circle();
            sr.color = Hades.FxDarkColor;
            ball.transform.localScale = Vector3.one * HS.bombRadius * 1.1f;
            Fx.Trail(ball.transform, Hades.FxColor, 40f, 0.35f, 0.4f, new Vector2(0f, 3f), hard: true);   // 꼬리 불꽃
        }
        drops.Add(drop);
    }

    private void Explode(Drop d)
    {
        float y = d.zone != null ? d.zone.transform.position.y : groundY;

        // 장판 안(발이 바닥 근처)에 있는 대상에게 피해. 구르기 무적 / 점프로 띄우면 회피
        boss.DamageArea(new Vector2(d.x, y + 0.7f), new Vector2(HS.bombRadius * 2f, 1.6f), Damage);

        Vector2 feet = new Vector2(d.x, y);
        Fx.Ring(feet, Hades.FxColor, HS.bombRadius, HS.bombRadius * 2.6f, 0.4f);
        Fx.Burst(feet, Vector2.up, 120f, Hades.FxColor, 16, 3f, 9f, 0.12f, 0.3f, 0.7f, 1.5f);
        if (Hades.Revived)
            Fx.Burst(feet, Vector2.up, 70f, Hades.FxDarkColor, 14, 4f, 11f, 0.2f, 0.45f, 0.6f, 1.2f);   // 보라 폭발
        else
            FxSpriteAnim.Play(new Vector3(d.x, y + HS.bombRadius * 0.6f, 0f), HS.explosionFrames, 14f, HS.bombRadius * 2.4f, false, 0f, 33);

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

// 암흑탄 부채꼴. 시전 모션 뒤 플레이어를 향해 여러 발을 부채꼴로 발사 (2형태 : 5발 x 2연속)
// 탄은 일직선이라 점프 / 구르기 / 측면 이동으로 회피
public class HadesBoltAction : BossAction
{
    private float startTime;
    private int volleysTotal;
    private int volleysFired;
    private float nextFire;
    private float finishTime;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesBoltAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 암흑탄 부채꼴");
        boss.StopMoving();
        boss.FaceTarget();
        startTime = Time.time;
        volleysFired = 0;
        volleysTotal = Hades.Revived ? HS.form2BoltVolleys : 1;
        nextFire = startTime + HS.boltCastTime / boss.AnimSpeed;
        finishTime = float.PositiveInfinity;

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Bolt, HS.boltCastTime / boss.AnimSpeed);
    }

    protected override NodeState OnUpdate()
    {
        if (volleysFired < volleysTotal && Time.time >= nextFire)
        {
            boss.Telegraph.Hide();
            boss.FaceTarget();
            Fire();
            volleysFired++;
            nextFire = Time.time + HS.boltVolleyInterval;
            if (volleysFired >= volleysTotal) finishTime = Time.time + 0.35f;   // 마지막 발사 후 잠깐의 후딜
        }
        return Time.time >= finishTime ? NodeState.Success : NodeState.Running;
    }

    private void Fire()
    {
        Bounds b = boss.Body.bounds;
        Vector2 from = new Vector2(b.center.x + boss.FacingDir * b.extents.x * 0.6f, b.center.y + b.extents.y * 0.2f);
        var col = boss.Target.GetComponent<Collider2D>();
        Vector2 targetPos = col != null ? (Vector2)col.bounds.center : (Vector2)boss.Target.transform.position;
        Vector2 aim = targetPos - from;
        // 보스 뒤쪽으로 쏘지 않도록 바라보는 방향으로 제한
        if (Mathf.Sign(aim.x) != boss.FacingDir) aim.x = boss.FacingDir * Mathf.Abs(aim.x) + boss.FacingDir * 0.01f;

        int count = Hades.Revived ? HS.form2BoltCount : HS.boltCount;
        float damage = Hades.Revived ? HS.form2BoltDamage : HS.boltDamage;
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : Mathf.Lerp(-HS.boltSpreadAngle, HS.boltSpreadAngle, i / (float)(count - 1));
            Vector2 dir = Quaternion.Euler(0f, 0f, angle) * aim;
            ProjectilePool.Instance.Get().Launch(from, dir, HS.boltSpeed, damage, 3f,
                boss.whatIsEnemy, LayerMask.GetMask("Ground"), null, Hades.FxColor, HS.boltRadius);
        }
        Fx.Burst(from, aim, 50f, Hades.FxColor, 12, 4f, 10f, 0.1f, 0.25f, 0.4f, 0.6f);
    }

    protected override void OnStop()
    {
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Throw);
    }
}

// 지면 폭발 파도. 보스 앞에서부터 플레이어 쪽으로 기둥 장판이 한 줄로 깔리고, 경고 뒤 앞에서부터 차례로 폭발
// 2형태에서는 양쪽 방향 동시. 점프 / 구르기 무적으로 넘거나, 기둥 사이를 판단해 빠져나가야 함
public class HadesEruptionAction : BossAction
{
    private class Column
    {
        public GroundZone zone;
        public float x, y;
        public float eruptTime;
        public bool done;
    }

    private const float CastTime = 0.6f;

    private readonly List<Column> columns = new List<Column>();
    private float startTime;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesEruptionAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 지면 폭발 파도");
        boss.StopMoving();
        boss.FaceTarget();
        startTime = Time.time;
        columns.Clear();

        float castTime = CastTime / boss.AnimSpeed;
        boss.PlayAction(BossController.AnimParams.Slam);   // 내려찍는 자세
        boss.Telegraph.Show(TelegraphKind.Spikes, castTime);

        float dir = boss.FacingDir;
        int count = Hades.Revived ? HS.form2EruptionColumns : HS.eruptionColumns;
        float baseX = boss.FeetPosition.x;
        float groundY = boss.FeetPosition.y;

        BuildLine(baseX, groundY, dir, count, castTime);
        if (Hades.Revived)
            BuildLine(baseX, groundY, -dir, count, castTime);
    }

    private void BuildLine(float baseX, float groundY, float dir, int count, float castTime)
    {
        for (int i = 0; i < count; i++)
        {
            float x = baseX + dir * (HS.eruptionStartOffset + i * HS.eruptionSpacing);
            RaycastHit2D ground = Physics2D.Raycast(new Vector2(x, groundY + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
            if (ground.collider == null) break;   // 바닥이 끊기면(벽 / 낭떠러지) 거기까지

            var col = new Column { x = x, y = ground.point.y };
            col.zone = GroundZone.Create(new Vector2(x, col.y + 0.05f), HS.eruptionRadius);
            col.zone.SetColors(Hades.ZoneIdleColor, Hades.ZoneLockedColor);
            col.zone.Lock(castTime + HS.eruptionWarnTime + i * HS.eruptionStep);
            col.eruptTime = startTime + castTime + HS.eruptionWarnTime + i * HS.eruptionStep;
            columns.Add(col);
        }
    }

    protected override NodeState OnUpdate()
    {
        if (Time.time >= startTime + 0.2f) boss.Telegraph.Hide();

        bool allDone = true;
        foreach (Column c in columns)
        {
            if (c.done) continue;
            allDone = false;
            if (Time.time < c.eruptTime) continue;
            Erupt(c);
        }
        return allDone ? NodeState.Success : NodeState.Running;
    }

    private void Erupt(Column c)
    {
        c.done = true;
        // 기둥 안(발이 바닥 근처)에 있는 대상에게 피해. 점프로 띄우거나 구르기 무적이면 회피
        boss.DamageArea(new Vector2(c.x, c.y + 1.2f), new Vector2(HS.eruptionRadius * 2f, 2.6f), HS.eruptionDamage);

        Vector2 feet = new Vector2(c.x, c.y);
        Fx.Ring(feet, Hades.FxColor, HS.eruptionRadius, HS.eruptionRadius * 2.2f, 0.35f);
        Fx.Burst(feet, Vector2.up, 40f, Hades.FxColor, 18, 6f, 13f, 0.15f, 0.35f, 0.5f, 1.2f);
        Fx.Burst(feet, Vector2.up, 25f, Hades.FxDarkColor, 10, 4f, 9f, 0.2f, 0.45f, 0.5f, 1.0f);
        if (c.zone != null) c.zone.Dismiss();
    }

    protected override void OnStop()
    {
        foreach (Column c in columns)
            if (c.zone != null) c.zone.Dismiss();
        columns.Clear();
        boss.Telegraph.Hide();
        boss.StopMoving();
        boss.Anim.ResetTrigger(BossController.AnimParams.Slam);
    }
}

// 순간이동 강타. 하데스가 흐려지며 무적으로 사라지고, 플레이어를 따라다니는 장판이 고정되면 그 위치에 나타나 내려찍음
// 회피 : 장판이 고정된 뒤 범위 밖으로 벗어나기 / 내려찍는 순간 구르기 무적
public class HadesShadowStrikeAction : BossAction
{
    private enum Phase { Fade, Track, Lock, Strike }

    private const float FadeTime = 0.35f;
    private const float StrikeDelay = 0.25f;   // 나타난 뒤 내려찍기까지

    private Phase phase;
    private float phaseStart;
    private GroundZone zone;
    private float targetX, restY;
    private bool struck;
    private bool invincibleSet;

    private HadesBoss Hades => (HadesBoss)boss;
    private HadesBossStats HS => Hades.HadesStats;

    public HadesShadowStrikeAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        Debug.Log("[Hades] 패턴 : 순간이동 강타");
        boss.StopMoving();
        boss.FaceTarget();
        restY = boss.transform.position.y;
        struck = false;
        zone = null;
        phase = Phase.Fade;
        phaseStart = Time.time;
        boss.ForceInvincible = true;
        invincibleSet = true;
        boss.Telegraph.Show(TelegraphKind.Leap, FadeTime);
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Fade:
                float a = 1f - Mathf.Clamp01(elapsed / FadeTime);
                Color fx = Hades.FxColor;
                boss.SetTint(new Color(fx.r, fx.g, fx.b, Mathf.Max(0.05f, a)));
                if (elapsed >= FadeTime)
                {
                    boss.Telegraph.Hide();
                    float px = boss.Target.transform.position.x;
                    zone = GroundZone.Create(new Vector2(px, GroundYAt(px) + 0.05f), HS.teleportRadius);
                    zone.SetColors(Hades.ZoneIdleColor, Hades.ZoneLockedColor);
                    Enter(Phase.Track);
                }
                return NodeState.Running;

            case Phase.Track:
                // 장판이 플레이어를 부드럽게 따라다님
                float x = Mathf.MoveTowards(zone.X, ClampToArena(boss.Target.transform.position.x), 9f * Time.deltaTime);
                zone.SetX(x);
                if (elapsed >= HS.teleportTrackTime)
                {
                    targetX = zone.X;
                    zone.Lock(HS.teleportLockTime);
                    Enter(Phase.Lock);
                }
                return NodeState.Running;

            case Phase.Lock:
                if (elapsed >= HS.teleportLockTime)
                {
                    Appear();
                    Enter(Phase.Strike);
                }
                return NodeState.Running;

            default: // Strike
                if (!struck && elapsed >= StrikeDelay / boss.AnimSpeed)
                {
                    struck = true;
                    Strike();
                }
                return elapsed >= 0.8f / boss.AnimSpeed ? NodeState.Success : NodeState.Running;
        }
    }

    private void Appear()
    {
        boss.ForceInvincible = false;
        invincibleSet = false;
        boss.RestoreTint();

        // 장판 위(플레이어가 있던 자리)에 나타남. 플레이어를 바라보며 내려찍는 모션
        boss.Rb.position = new Vector2(targetX, restY);
        boss.transform.position = new Vector3(targetX, restY, boss.transform.position.z);
        boss.FaceTarget();
        boss.PlayAction(BossController.AnimParams.Melee);

        Bounds b = boss.Body.bounds;
        Fx.Ring(new Vector2(targetX, b.min.y), Hades.FxColor, 1f, HS.teleportRadius * 1.2f, 0.3f);
        Fx.Burst(b.center, Vector2.up, 360f, Hades.FxColor, 22, 4f, 10f, 0.12f, 0.3f, 0.4f, 0.8f);
    }

    private void Strike()
    {
        float y = zone != null ? zone.transform.position.y : boss.FeetPosition.y;
        // 장판 범위 안(발이 바닥 근처)이면 피해. 구르기 무적 / 점프로 회피
        boss.DamageArea(new Vector2(targetX, y + 1.2f), new Vector2(HS.teleportRadius * 2f, 2.6f), HS.teleportDamage);
        Vector2 feet = new Vector2(targetX, y);
        Fx.Ring(feet, Hades.FxColor, HS.teleportRadius, HS.teleportRadius * 2.4f, 0.4f);
        Fx.Burst(feet, Vector2.up, 130f, Hades.FxColor, 22, 4f, 11f, 0.12f, 0.3f, 0.5f, 1.4f);
        if (zone != null) zone.Dismiss();
    }

    private float ClampToArena(float x) => Mathf.Clamp(x, HS.arenaMinX, HS.arenaMaxX);

    private float GroundYAt(float x)
    {
        RaycastHit2D ground = Physics2D.Raycast(new Vector2(x, boss.FeetPosition.y + 3f), Vector2.down, 8f, LayerMask.GetMask("Ground"));
        return ground.collider != null ? ground.point.y : boss.FeetPosition.y;
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    // 패턴이 중간에 끊겨도(그로기 등) 몸 / 무적 / 색을 반드시 복구
    protected override void OnStop()
    {
        if (invincibleSet) boss.ForceInvincible = false;
        invincibleSet = false;
        boss.RestoreTint();
        boss.Telegraph.Hide();
        if (zone != null) zone.Dismiss();
        boss.Anim.ResetTrigger(BossController.AnimParams.Melee);
    }
}
