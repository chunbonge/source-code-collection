using System;
using DataEnum;

public abstract class TimelineTimer
{
    protected int _initialDuration;
    protected int Duration { get; set; }

    protected TimelineTimer(int duration)
    {
        _initialDuration = duration;
        IsRunning = false;
    }

    public abstract void UpdateTimer();

    public Action OnTimerStart = delegate { };
    public Action OnTimerStop = delegate { };

    public bool IsRunning { get; set; }
    public float Remain => Duration;

    public void Start()
    {
        Duration = _initialDuration;

        if (!IsRunning)
        {
            IsRunning = true;
            OnTimerStart.Invoke();
        }
    }

    public void Stop()
    {
        if (IsRunning)
        {
            IsRunning = false;
            OnTimerStop.Invoke();
        }
    }

    public void Reset() => Duration = _initialDuration;
    public void Reset(int initDuration)
    {
        _initialDuration = initDuration;
        Duration = _initialDuration;
    }
}