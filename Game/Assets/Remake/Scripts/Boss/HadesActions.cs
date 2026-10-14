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
            Fx.Ring(hitPoint, new Color(0.8f, 0.4f, 1f, 1f), 0.6f, 2.4f, 0.3f);
            Fx.Burst(hitPoint, Vector2.up, 120f, new Color(0.75f, 0.35f, 1f), 14, 3f, 8f, 0.1f, 0.25f, 0.6f, 1.2f);
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

// 화염탄 비. 시전 모션 뒤 바닥 곳곳에 낙하 지점(붉은 장판)이 하나씩 생기고, 경고 시간이 지나면 화염탄이 떨어져 폭발
// 원작처럼 맵 전체(arenaMinX ~ arenaMaxX)에 흩뿌리되, 첫 한 발은 플레이어 위치를 노림. 1형태 3발 / 2형태 5발
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
        boss.StopMoving();
        boss.FaceTarget();
        groundY = boss.FeetPosition.y;
        startTime = Time.time;
        spawned = 0;
        total = Hades.Revived ? HS.form2BombCount : HS.bombCount;
        nextSpawn = startTime + HS.bombCastTime;
        drops.Clear();

        boss.PlayAction(BossController.AnimParams.Throw);
        boss.Telegraph.Show(TelegraphKind.Bolt, HS.bombCastTime);
        Bounds b = boss.Body.bounds;
        Fx.Burst(new Vector2(b.center.x, b.max.y), Vector2.up, 40f, new Color(0.8f, 0.4f, 1f), 20, 6f, 14f, 0.15f, 0.35f, 0.8f, 0.4f);
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
        drop.zone.Lock(HS.bombWarnTime);

        var ball = new GameObject("HadesFireBomb");
        drop.ball = ball.transform;
        var sr = ball.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 32;
        if (HS.fireballFrames != null && HS.fireballFrames.Length > 0 && HS.fireballFrames[0] != null)
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
            sr.color = new Color(1f, 0.5f, 0.1f);
            ball.transform.localScale = Vector3.one * HS.bombRadius;
        }
        drops.Add(drop);
    }

    private void Explode(Drop d)
    {
        float y = d.zone != null ? d.zone.transform.position.y : groundY;

        // 장판 안(발이 바닥 근처)에 있는 대상에게 피해. 구르기 무적 / 점프로 띄우면 회피
        boss.DamageArea(new Vector2(d.x, y + 0.7f), new Vector2(HS.bombRadius * 2f, 1.6f), Damage);

        Vector2 feet = new Vector2(d.x, y);
        Fx.Ring(feet, new Color(1f, 0.45f, 0.1f, 1f), HS.bombRadius, HS.bombRadius * 2.6f, 0.4f);
        Fx.Burst(feet, Vector2.up, 120f, new Color(1f, 0.6f, 0.15f), 16, 3f, 9f, 0.12f, 0.3f, 0.7f, 1.5f);
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
