using System;
using System.Collections.Generic;
using UnityEngine;

// 경량 Behavior Tree.
// - 노드는 매 프레임 Tick되어 Running / Success / Failure를 반환
// - Sequence / Selector는 실행 중(Running)인 자식을 기억해 다음 Tick에 그 자식부터 이어서 실행 (메모리 컴포지트)
//   → 공격 패턴 같은 긴 행동이 끝날 때까지 다른 분기가 끼어들지 않음
// - 그로기 / 사망처럼 즉시 끊어야 하는 상황은 트리 바깥에서 Abort()로 중단
public enum NodeState { Running, Success, Failure }

public abstract class BTNode
{
    private bool started;
    public bool IsRunning => started;

    public NodeState Tick()
    {
        if (!started)
        {
            started = true;
            OnStart();
        }

        NodeState state = OnUpdate();

        if (state != NodeState.Running)
        {
            started = false;
            OnStop();
        }
        return state;
    }

    // 실행 중인 노드를 강제로 중단 (자식까지 전파)
    public void Abort()
    {
        if (!started) return;
        started = false;
        OnAbort();
        OnStop();
    }

    protected virtual void OnStart() { }
    protected abstract NodeState OnUpdate();
    protected virtual void OnStop() { }
    protected virtual void OnAbort() { }
}

#region Composites
public abstract class Composite : BTNode
{
    protected readonly List<BTNode> children;
    protected int current;

    protected Composite(params BTNode[] children)
    {
        this.children = new List<BTNode>(children);
    }

    protected override void OnStart() => current = 0;

    protected override void OnAbort()
    {
        foreach (var child in children)
            child.Abort();
    }
}

// 자식을 순서대로 실행. 하나라도 실패하면 실패, 모두 성공하면 성공 (AND)
public class Sequence : Composite
{
    public Sequence(params BTNode[] children) : base(children) { }

    protected override NodeState OnUpdate()
    {
        while (current < children.Count)
        {
            NodeState state = children[current].Tick();
            if (state != NodeState.Success) return state; // Running 또는 Failure
            current++;
        }
        return NodeState.Success;
    }
}

// 자식을 순서대로 시도. 하나라도 성공하면 성공, 모두 실패하면 실패 (OR, 우선순위)
public class Selector : Composite
{
    public Selector(params BTNode[] children) : base(children) { }

    protected override NodeState OnUpdate()
    {
        while (current < children.Count)
        {
            NodeState state = children[current].Tick();
            if (state != NodeState.Failure) return state; // Running 또는 Success
            current++;
        }
        return NodeState.Failure;
    }
}

// 시작할 때마다 자식 순서를 섞는 Selector. 같은 조건에서도 매번 다른 패턴을 고르게 해 예측을 어렵게 함
public class RandomSelector : Selector
{
    public RandomSelector(params BTNode[] children) : base(children) { }

    protected override void OnStart()
    {
        base.OnStart();
        for (int i = children.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (children[i], children[j]) = (children[j], children[i]);
        }
    }
}
#endregion

#region Decorators / Leaves
// 조건 검사 (즉시 Success / Failure)
public class Condition : BTNode
{
    private readonly Func<bool> predicate;
    public Condition(Func<bool> predicate) => this.predicate = predicate;
    protected override NodeState OnUpdate() => predicate() ? NodeState.Success : NodeState.Failure;
}

// 즉시 실행되는 단순 행동
public class ActionNode : BTNode
{
    private readonly Func<NodeState> action;
    public ActionNode(Func<NodeState> action) => this.action = action;
    protected override NodeState OnUpdate() => action();
}

// 자식이 끝나면(성공/실패/중단) 쿨다운 시작, 쿨다운 동안은 Failure 반환해 다른 분기로 넘어가게 함
public class Cooldown : BTNode
{
    private readonly BTNode child;
    private readonly Func<float> duration;
    private float readyTime = float.NegativeInfinity;

    public Cooldown(Func<float> duration, BTNode child)
    {
        this.duration = duration;
        this.child = child;
    }

    protected override NodeState OnUpdate()
    {
        if (!child.IsRunning && Time.time < readyTime)
            return NodeState.Failure;

        NodeState state = child.Tick();
        // 조건 불충족(Failure)으로 시작조차 못 한 경우는 쿨다운을 걸지 않음
        if (state == NodeState.Success)
            readyTime = Time.time + duration();
        return state;
    }

    protected override void OnAbort()
    {
        child.Abort();
        readyTime = Time.time + duration(); // 그로기 등으로 끊겨도 같은 패턴이 바로 반복되지 않도록
    }
}

// 일정 시간 대기
public class Wait : BTNode
{
    private readonly Func<float> duration;
    private float endTime;

    public Wait(Func<float> duration) => this.duration = duration;
    protected override void OnStart() => endTime = Time.time + duration();
    protected override NodeState OnUpdate() => Time.time >= endTime ? NodeState.Success : NodeState.Running;
}
#endregion
