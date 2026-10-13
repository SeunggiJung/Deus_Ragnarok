using UnityEngine;

// 보스 행동 트리의 행동(Leaf) 노드들

public abstract class BossAction : BTNode
{
    protected readonly BossController boss;
    protected BossStats S => boss.Stats;

    protected BossAction(BossController boss) => this.boss = boss;
}

// 애니메이션 하나를 재생하고 끝날 때까지 Running. 진행률에 맞춰 판정 등을 처리할 수 있음
public abstract class BossAnimAction : BossAction
{
    private const float Timeout = 4f; // 애니메이터 문제로 상태에 못 들어가도 갇히지 않도록
    private readonly string trigger;
    private readonly string stateName;
    private bool entered;
    private float startTime;

    protected BossAnimAction(BossController boss, string trigger, string stateName) : base(boss)
    {
        this.trigger = trigger;
        this.stateName = stateName;
    }

    protected override void OnStart()
    {
        entered = false;
        startTime = Time.time;
        boss.FaceTarget();
        boss.PlayAction(trigger);
        OnBegin();
    }

    protected override NodeState OnUpdate()
    {
        AnimatorStateInfo info = boss.Anim.GetCurrentAnimatorStateInfo(0);
        bool inState = info.IsName(stateName) && !boss.Anim.IsInTransition(0);

        // 같은 패턴이 연달아 나올 때 이전 재생의 끝부분(진행률 1 이상)을 새 재생으로 착각하지 않도록
        if (!entered && inState && info.normalizedTime < 0.9f)
            entered = true;

        if (entered && inState)
            OnAnimUpdate(info.normalizedTime);

        bool finished = entered && (!inState || info.normalizedTime >= 1f);
        if (finished || Time.time - startTime > Timeout)
            return NodeState.Success;
        return NodeState.Running;
    }

    protected override void OnStop()
    {
        boss.Anim.ResetTrigger(trigger);
        OnEnd();
    }

    protected virtual void OnBegin() { }
    protected virtual void OnAnimUpdate(float normalizedTime) { }
    protected virtual void OnEnd() { }
}

// 추적. 근접 거리에 도달하거나 짧은 시간(0.25초)이 지나면 Success를 반환해 트리가 패턴을 다시 고르게 함
public class ChaseAction : BossAction
{
    private const float ReevaluateInterval = 0.25f;
    private float startTime;

    public ChaseAction(BossController boss) : base(boss) { }

    protected override void OnStart() => startTime = Time.time;

    protected override NodeState OnUpdate()
    {
        if (boss.DistanceToTarget <= S.meleeRange * 0.8f)
        {
            boss.FaceTarget();
            boss.StopMoving();
            return NodeState.Success;
        }

        boss.FaceTarget();
        boss.Anim.SetBool(BossController.AnimParams.Moving, true);
        boss.SetVelocityX(boss.DirectionToTarget * boss.MoveSpeed);

        return Time.time - startTime >= ReevaluateInterval ? NodeState.Success : NodeState.Running;
    }
}

// 근접 연타 (Attack_2). 애니메이션의 Hit 이벤트 2회마다 근접 판정
public class MeleeComboAction : BossAnimAction
{
    public MeleeComboAction(BossController boss) : base(boss, BossController.AnimParams.Melee, "Melee") { }

    protected override void OnBegin() => boss.HitEventHandler = boss.MeleeHit;
    protected override void OnEnd() => boss.HitEventHandler = null;
}

// 지면 가시 (Attack_1). 내려찍는 동작의 작은 가시가 예고, 진행률 slamHitTime에 큰 가시가 솟으며 주변 광역 피해
public class SlamAction : BossAnimAction
{
    private bool erupted;

    public SlamAction(BossController boss) : base(boss, BossController.AnimParams.Slam, "Slam") { }

    protected override void OnBegin() => erupted = false;

    protected override void OnAnimUpdate(float t)
    {
        if (erupted || t < S.slamHitTime) return;
        erupted = true;

        Vector2 area = S.slamArea * (boss.IsPhase2 ? 1.25f : 1f);
        boss.DamageArea(boss.FeetPosition + Vector2.up * area.y * 0.5f, area, S.slamDamage);
    }
}

// 에너지탄 (Attack_3). 진행률 throwReleaseTime에 플레이어를 향해 발사, 2페이즈에서는 3갈래
public class ThrowAction : BossAnimAction
{
    private bool released;

    public ThrowAction(BossController boss) : base(boss, BossController.AnimParams.Throw, "Throw") { }

    protected override void OnBegin() => released = false;

    protected override void OnAnimUpdate(float t)
    {
        if (released || t < S.throwReleaseTime) return;
        released = true;

        Vector2 firePoint = boss.attackCheck != null ? (Vector2)boss.attackCheck.position : (Vector2)boss.transform.position;
        Vector2 aim = TargetCenter() - firePoint;
        // 보스 뒤쪽으로 쏘지 않도록 바라보는 방향으로 제한
        if (Mathf.Sign(aim.x) != boss.FacingDir) aim.x = boss.FacingDir * Mathf.Abs(aim.x) + boss.FacingDir * 0.01f;

        if (boss.IsPhase2)
        {
            Fire(firePoint, Rotate(aim, S.phase2SpreadAngle));
            Fire(firePoint, aim);
            Fire(firePoint, Rotate(aim, -S.phase2SpreadAngle));
        }
        else
        {
            Fire(firePoint, aim);
        }
    }

    private Vector2 TargetCenter()
    {
        var col = boss.Target.GetComponent<Collider2D>();
        return col != null ? (Vector2)col.bounds.center : (Vector2)boss.Target.transform.position;
    }

    private void Fire(Vector2 from, Vector2 dir)
    {
        ProjectilePool.Instance.Get().Launch(from, dir, S.throwSpeed, S.throwDamage, S.throwLifetime,
            boss.whatIsEnemy, LayerMask.GetMask("Ground"), S.projectileSprite, S.projectileColor, S.throwRadius);
    }

    private static Vector2 Rotate(Vector2 v, float degrees) => Quaternion.Euler(0f, 0f, degrees) * v;
}

// 돌진. 예고(주황색 점멸) → 바라보는 방향으로 고속 돌진(몸에 닿으면 피해, 1회) → 짧은 후딜
public class ChargeAction : BossAction
{
    private enum Phase { Telegraph, Dash, Recover }
    private const float RecoverTime = 0.5f;
    private const float BlinkInterval = 0.1f;

    private Phase phase;
    private float phaseStart;
    private float dir;
    private float startX;
    private bool hitTarget;

    public ChargeAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.StopMoving();
        boss.FaceTarget();
        dir = boss.FacingDir;
        phase = Phase.Telegraph;
        phaseStart = Time.time;
        hitTarget = false;
    }

    protected override NodeState OnUpdate()
    {
        float elapsed = Time.time - phaseStart;

        switch (phase)
        {
            case Phase.Telegraph:
                // 예고 : 주황색으로 점멸 (2페이즈에서는 더 짧게)
                bool on = Mathf.FloorToInt(elapsed / BlinkInterval) % 2 == 0;
                boss.SetTint(on ? S.chargeTelegraphTint : Color.white);
                if (elapsed >= S.chargeTelegraph * (boss.IsPhase2 ? 0.75f : 1f))
                {
                    boss.RestoreTint();
                    boss.Anim.SetBool(BossController.AnimParams.Moving, true);
                    boss.Anim.speed = boss.AnimSpeed * 2f;
                    startX = boss.transform.position.x;
                    Enter(Phase.Dash);
                }
                return NodeState.Running;

            case Phase.Dash:
                boss.SetVelocityX(dir * S.chargeSpeed);
                CheckContactDamage();

                float traveled = Mathf.Abs(boss.transform.position.x - startX);
                bool timeout = elapsed > S.chargeDistance / S.chargeSpeed + 0.5f;
                if (traveled >= S.chargeDistance || IsWallAhead() || timeout)
                {
                    boss.StopMoving();
                    boss.Anim.speed = boss.AnimSpeed;
                    Enter(Phase.Recover);
                }
                return NodeState.Running;

            default: // Recover
                return elapsed >= RecoverTime ? NodeState.Success : NodeState.Running;
        }
    }

    private void Enter(Phase next)
    {
        phase = next;
        phaseStart = Time.time;
    }

    private void CheckContactDamage()
    {
        if (hitTarget) return;

        Bounds b = boss.Body.bounds;
        foreach (var hit in Physics2D.OverlapBoxAll(b.center, b.size + Vector3.one * 0.2f, 0f, boss.whatIsEnemy))
        {
            BaseCharacter target = hit.GetComponentInParent<BaseCharacter>();
            if (target == null || target.IsInvincible) continue; // 구르기로 피하면 돌진이 끝날 때까지 다시 맞을 수 있음
            target.TakeDamage(S.chargeDamage);
            hitTarget = true;
            return;
        }
    }

    private bool IsWallAhead()
    {
        Bounds b = boss.Body.bounds;
        Vector2 size = new Vector2(b.size.x * 0.5f, b.size.y * 0.7f);
        Vector2 center = (Vector2)b.center + Vector2.up * b.size.y * 0.1f;
        return Physics2D.BoxCast(center, size, 0f, new Vector2(dir, 0f), b.extents.x * 0.5f + 0.2f, LayerMask.GetMask("Ground")).collider != null;
    }

    protected override void OnStop()
    {
        boss.StopMoving();
        boss.Anim.speed = boss.AnimSpeed;
        boss.RestoreTint();
    }
}

// 방어 자세 (Ability). 짧은 시간에 여러 번 맞으면 사용. 받는 피해 감소 + 강인도 감소 없음
public class GuardAction : BossAction
{
    private float endTime;

    public GuardAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.FaceTarget();
        boss.PlayAction(BossController.AnimParams.Ability);
        boss.IsGuarding = true;
        boss.SetTint(S.guardTint);
        endTime = Time.time + S.guardDuration;
    }

    protected override NodeState OnUpdate() => Time.time >= endTime ? NodeState.Success : NodeState.Running;

    protected override void OnStop()
    {
        boss.IsGuarding = false;
        boss.ClearPressure();
        boss.RestoreTint();
        boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
    }
}

// 포효 (Ability). 체력이 절반 이하가 되면 한 번 사용. 무적 상태로 붉게 빛난 뒤 2페이즈 진입
public class RoarAction : BossAction
{
    private float endTime;

    public RoarAction(BossController boss) : base(boss) { }

    protected override void OnStart()
    {
        boss.PlayAction(BossController.AnimParams.Ability);
        boss.ForceInvincible = true;
        boss.SetTint(S.roarTint);
        endTime = Time.time + S.roarTime;
    }

    protected override NodeState OnUpdate()
    {
        if (Time.time < endTime) return NodeState.Running;
        boss.EnterPhase2();
        return NodeState.Success;
    }

    protected override void OnStop()
    {
        boss.ForceInvincible = false;
        boss.RestoreTint();
        boss.Anim.ResetTrigger(BossController.AnimParams.Ability);
    }
}
