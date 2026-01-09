using UnityEngine;
using System;
using DataEnum;

public class BasicStatModifier<T> : StatModifier
{
    private readonly BUFF_TYPE type;
    private readonly Func<T, T> operation;

    public BasicStatModifier(BUFF_TYPE type, UPDATE_TYPE updateType, Func<T, T> operation, EffectTimer timer = null) : base(updateType, timer)
    {
        this.type = type;
        this.operation = operation;
    }

    public override void Handle(object sender, IQuery query)
    {
        if(query is Query<T> typedQuery && operation != null && type.Equals(typedQuery.BuffType))
        {
            typedQuery.Value = operation(typedQuery.Value);
        }
    }
}

public abstract class StatModifier : IDisposable
{
    private readonly EffectTimer _timer;
    public TimelineTimer Timer => _timer;
    public UPDATE_TYPE UpdateType { get; } = UPDATE_TYPE.NONE;
    public event Action<StatModifier> OnDispose = delegate { };
    public event Action OnTimelineUpdate = delegate { };

    protected StatModifier(UPDATE_TYPE updateType, EffectTimer timer)
    {
        if (timer == null) return;

        UpdateType = updateType;
        _timer = timer;
        _timer.OnTimerStop += () => { Dispose(); };
        _timer.Start();
    }

    public abstract void Handle(object sender, IQuery query);

    public void OnTurnUpdate()
    {
        OnTimelineUpdate.Invoke();
    }

    public void Dispose()
    {
        OnDispose.Invoke(this);
    }
}
