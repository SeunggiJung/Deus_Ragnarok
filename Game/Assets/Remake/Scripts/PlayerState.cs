using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : IState
{
    protected PlayerController player;
    protected StateMachine stateMachine;
    protected string animBoolName; // 애니메이터의 파라미터 이름

    public PlayerState(PlayerController player, StateMachine stateMachine, string animBoolName)
    {
        this.player = player;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        player.Anim.SetBool(animBoolName, true);
    }

    public virtual void Exit()
    {
        player.Anim.SetBool(animBoolName, false);
    }
    public virtual void HandleInput() { }

    // 구르기 캔슬 : 지상/공중/공격 중 어디서든 구르기 입력이 들어오면 현재 모션을 끊고 회피.
    // 공중 구르기는 체공 중 1회 (연타로 공중에 떠 있는 것 방지). 전환했으면 true
    protected bool TryDodge()
    {
        if (!player.Input.DodgePressed) return false;
        if (Crusher.IsInDangerZone(player.transform.position)) return false; // 내려찍는 장애물 구역에서는 구르기 불가

        bool grounded = player.IsGrounded();
        if (!grounded && !player.CanAirDodge) return false;
        if (!player.TryUseStamina(player.Stats.dodgeCost)) return false;

        if (!grounded) player.CanAirDodge = false;
        stateMachine.ChangeState(player.DodgeState);
        return true;
    }
    public virtual void Update() { }
    public virtual void PhysicsUpdate() { }
}
