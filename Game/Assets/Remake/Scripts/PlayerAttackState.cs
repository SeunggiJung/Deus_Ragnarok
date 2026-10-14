using UnityEngine;

// 지상 공격 : 3단 콤보 (1타 내려치기 → 2타 올려치기 → 3타 찌르기).
// - 각 단계는 애니메이터의 Attack1 / Attack2 / Attack3 상태 (2, 3타 모션은 Tools/Deus/Generate Player Combo Clips 로 생성)
// - 모션은 끝까지 재생된다 (구르기만 캔슬 가능). 모션 후반(comboChainStart)부터 누른 클릭만 콤보로 인정하고,
//   모션이 끝나는 순간 바로 다음 타로 이어짐. 끝난 직후 comboResumeGrace 안에 눌러도 이어짐
public class PlayerAttackState : PlayerState
{
    public const int MaxStep = 2; // 0 ~ 2 (3단)

    public int Step { get; private set; }

    private bool advancing;   // 콤보 이어가기로 재진입하는 중인가
    private bool buffered;    // 후반 구간에 눌러 둔 공격 입력
    private float lungeStart;
    private float lungeEnd;
    private float lungeSpeed;
    private float swingSoundTime;
    private bool swingSoundPlayed;
    // 모션 시작 후 휘두르는 소리가 나는 시점 (판정 직전. 1타 내려치기 / 2타 올려치기 / 3타 찌르기)
    private static readonly float[] SwingSoundDelay = { 0.18f, 0.14f, 0.16f };

    private int resumeStep;       // 모션이 끝난 직후 다시 누르면 이어질 단계
    private float resumeUntil = float.NegativeInfinity;

    public PlayerAttackState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    private string StateName => "Attack" + (Step + 1);

    public override void Enter()
    {
        if (advancing) Step = Mathf.Min(Step + 1, MaxStep);
        else Step = Time.time <= resumeUntil ? resumeStep : 0;
        advancing = false;
        buffered = false;
        resumeUntil = float.NegativeInfinity;

        animBoolName = StateName; // 단계마다 다른 애니메이터 bool 사용
        base.Enter();

        PlayerStats stats = player.Stats;
        lungeSpeed = GetOrDefault(stats.comboLungeSpeed, Step, 0f);
        lungeStart = Time.time + GetOrDefault(stats.comboLungeDelay, Step, 0f);
        lungeEnd = lungeStart + GetOrDefault(stats.comboLungeTime, Step, 0f);
        swingSoundTime = Time.time + SwingSoundDelay[Step];
        swingSoundPlayed = false;

        // 공격 시작 시 제자리 (휘두르는 순간에 짧게 앞으로 나감 : PhysicsUpdate)
        player.SetVelocity(0, player.Rb.velocity.y);
    }

    public override void HandleInput()
    {
        base.HandleInput();
        if (TryDodge()) return; // 구르기만 모션 캔슬 가능 (판정 이벤트 전에 캔슬하면 공격은 나가지 않음)

        // 후반 구간에 누른 클릭만 보관 (너무 일찍 누른 클릭은 무시 → 모션이 스킵되지 않음)
        if (player.Input.AttackPressed && Step < MaxStep && InChainWindow())
            buffered = true;
    }

    public override void Update()
    {
        base.Update();

        // 모션 도중 구르기로 캔슬하면 상태가 바뀌어 이 소리도 나지 않음
        if (!swingSoundPlayed && Time.time >= swingSoundTime)
        {
            swingSoundPlayed = true;
            GameAudio.Play(Step == 0 ? Sfx.SwingSlash : Step == 1 ? Sfx.SwingUpper : Sfx.SwingThrust, 0.9f);
        }

        AnimatorStateInfo info = player.Anim.GetCurrentAnimatorStateInfo(0);
        if (player.Anim.IsInTransition(0) || !info.IsName(StateName)) return;
        if (info.normalizedTime < 1.0f) return;

        // 모션 종료
        if (buffered && player.TryUseStamina(player.Stats.attackCost))
        {
            player.CheckFlip(player.Input.MoveX); // 콤보 사이에 방향 전환 가능
            advancing = true;
            stateMachine.ChangeState(this);
            return;
        }

        if (Step < MaxStep)
        {
            resumeStep = Step + 1;
            resumeUntil = Time.time + player.Stats.comboResumeGrace;
        }
        stateMachine.ChangeState(player.IdleState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float vx = Time.time >= lungeStart && Time.time < lungeEnd ? player.FacingDir * lungeSpeed : 0f;
        player.SetVelocity(vx, player.Rb.velocity.y);
    }

    private bool InChainWindow()
    {
        AnimatorStateInfo info = player.Anim.GetCurrentAnimatorStateInfo(0);
        if (player.Anim.IsInTransition(0) || !info.IsName(StateName)) return false;
        return info.normalizedTime >= GetOrDefault(player.Stats.comboChainStart, Step, 0.6f);
    }

    private static float GetOrDefault(float[] values, int index, float fallback)
    {
        return values != null && index < values.Length ? values[index] : fallback;
    }
}
