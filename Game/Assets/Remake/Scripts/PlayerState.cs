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
        this.player = player;
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        player.GetComponent<Animator>().SetBool(animBoolName, true);
    }

    public virtual void Exit()
    {
        player.GetComponent<Animator>().SetBool(animBoolName, false);
    }
    public virtual void HandleInput() { }
    public virtual void Update() { }
    public virtual void PhysicsUpdate() { }
}
