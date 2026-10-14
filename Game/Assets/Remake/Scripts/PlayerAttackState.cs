using UnityEngine;

// 지상 공격 : 연타하면 3단 콤보 (휘두르기 → 반대 베기 → 찌르기).
// - 각 단계는 애니메이터의 Attack1 / Attack2 / Attack3 상태 (모션은 Tools/Deus/Generate Player Combo Clips 로 생성)
// - 판정 이후(comboCancelTime)부터 다음 공격 입력을 받아 모션을 끊고 바로 다음 단계로 이어짐 (선입력 보관)
// - 같은 구간부터 이동 / 점프로도 캔슬 가능 → 후딜레이를 줄여 속도감 확보
public class PlayerAttackState : PlayerState
{
    public const int MaxStep = 2; // 0 ~ 2 (3단)

    public int Step { get; private set; }

    private bool advancing;   // 콤보 이어가기로 재진입하는 중인가
    private bool buffered;    // 판정 이전에 눌러 둔 공격 입력
    private float lungeEnd;
    private float lungeSpeed;

    public PlayerAttackState(PlayerController player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName) { }

    private string StateName => "Attack" + (Step + 1);

    public override void Enter()
    {
        Step = advancing ? Mathf.Min(Step + 1, MaxStep) : 0;
        advancing = false;
        buffered = false;

        animBoolName = StateName; // 단계마다 다른 애니메이터 bool 사용
        base.Enter();

        PlayerStats stats = player.Stats;
        lungeSpeed = GetOrDefault(stats.comboLungeSpeed, Step, 0f);
        lungeEnd = Time.time + GetOrDefault(stats.comboLungeTime, Step, 0f);

        // 공격 시작 시 제자리 (콤보 단계에 따라 짧게 앞으로 밀고 나감 : PhysicsUpdate)
        player.SetVelocity(0, player.Rb.velocity.y);
    }

    // 공격 모션 도중 구르기로 캔슬 (판정 이벤트 전에 캔슬하면 공격은 나가지 않음)
    public override void HandleInput()
    {
        base.HandleInput();
        if (TryDodge()) return;

        if (player.Input.AttackPressed)
            buffered = true;
    }

    public override void Update()
    {
        base.Update();

        // 진입 직후에는 애니메이터가 아직 이전 상태를 재생 중이므로 실제로 들어온 뒤부터 판단
        AnimatorStateInfo info = player.Anim.GetCurrentAnimatorStateInfo(0);
        if (player.Anim.IsInTransition(0) || !info.IsName(StateName)) return;

        float t = info.normalizedTime;
        PlayerStats stats = player.Stats;
        bool canCancel = t >= GetOrDefault(stats.comboCancelTime, Step, 0.6f);

        if (canCancel)
        {
            if (buffered && Step < MaxStep && player.TryUseStamina(stats.attackCost))
            {
                player.CheckFlip(player.Input.MoveX); // 콤보 사이에 방향 전환 가능
                advancing = true;
                stateMachine.ChangeState(this);
                return;
            }

            if (player.Input.JumpPressed && player.TryUseStamina(stats.jumpCost))
            {
                stateMachine.ChangeState(player.JumpState);
                return;
            }

            if (player.Input.MoveX != 0f)
            {
                stateMachine.ChangeState(player.MoveState);
                return;
            }
        }

        if (t >= 1.0f)
            stateMachine.ChangeState(player.IdleState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        float vx = Time.time < lungeEnd ? player.FacingDir * lungeSpeed : 0f;
        player.SetVelocity(vx, player.Rb.velocity.y);
    }

    private static float GetOrDefault(float[] values, int index, float fallback)
    {
        return values != null && index < values.Length ? values[index] : fallback;
    }
}
