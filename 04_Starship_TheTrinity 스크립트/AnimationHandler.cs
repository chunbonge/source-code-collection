using Cysharp.Threading.Tasks;
using DataEnum;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class AnimationHandler : MonoBehaviour
{    
    private Animator anim;

    private ANIMATION _currentAnimation;
    private ANIMATION _previousAnimation;

    private UnitSoundContainer _soundContainer;
    private ISoundService soundService;
    
    public void Init(UnitSoundContainer soundContainer = null)
    {
        _soundContainer = soundContainer;
        anim = GetComponent<Animator>();
        _currentAnimation = ANIMATION.IDLE;

        ServiceLocator.For(this).Get(out soundService);
    }

    public async UniTask<bool> ChangeAnimationAsync(ANIMATION animation, float fadeTime = 0.0f, CancellationToken ct = default)
    {
        _previousAnimation = _currentAnimation;
        _currentAnimation = animation;
        int stateHash = ChangeAnimation(animation, fadeTime);
        
        var token = ct != default ? ct : this.GetCancellationTokenOnDestroy();
        return await WaitForAnimationFinished(0, stateHash, token);
    }

    public int ChangeAnimation(ANIMATION animation, float fadeTime = 0f)
    {
        int stateHash = 0;
        switch (animation)
        {
            case ANIMATION.IDLE:
                stateHash = AnimHash.Idle;
                break;
            case ANIMATION.ATTACK:
                stateHash = AnimHash.Attack;
                break;
            case ANIMATION.HIT:
                stateHash = AnimHash.Hit;
                break;
            case ANIMATION.DEATH:
                stateHash = AnimHash.Death;
                break;
            case ANIMATION.MOVE:
                stateHash = AnimHash.Move;
                break;
            case ANIMATION.RETREAT:
                stateHash = AnimHash.Retreat;
                break;
        }

        _previousAnimation = _currentAnimation;
        _currentAnimation = animation;
        anim.CrossFade(stateHash, fadeTime);

        return stateHash;
    }

    public void ResetAnimation()
    {
        ChangeAnimation(_previousAnimation);
    }

    private async UniTask<bool> WaitForAnimationFinished(int layerIndex, int stateHash, CancellationToken ct = default)
    {
        // 1) Wait until the animator actually enters the target state
        //    (handles transition delay / cross-fade)
        await UniTask.WaitUntil(() =>
        {
            var cur = anim.GetCurrentAnimatorStateInfo(layerIndex);
            return cur.fullPathHash == stateHash;
        }, cancellationToken: ct);

        // 2) Wait until the target state finishes playing once
        await UniTask.WaitUntil(() =>
        {
            if(anim.IsInTransition(layerIndex)) return false;

            var cur = anim.GetCurrentAnimatorStateInfo(layerIndex);

            bool leftTarget = (cur.fullPathHash != stateHash);
            bool finished = (cur.fullPathHash == stateHash && cur.normalizedTime >= 1f);

            return finished || leftTarget;
        }, cancellationToken: ct);

        return true;
    }

    #region[Event]
    [SerializeField]
    private List <AnimationEventCombat> animationEvents = new();

    public void OnAnimationEventTriggered(ANIMATION_EVENT eventType)
    {
        AnimationEventCombat matchingEvent = animationEvents.Find(e => e.eventType == eventType);
        matchingEvent?.OnAnimationEvent?.Invoke();
    }

    public event Action Attack;
    public event Action Move;

    /// <summary>
    /// This Method Operate at Animation Event
    /// </summary>
    /// <param name="state"></param>
    private void OperateEvent(UNIT_STATE state)
    {
        switch (state)
        {
            case UNIT_STATE.ATTACK:
                soundService.PlayEffectSound(_soundContainer.AttackSound);
                Attack?.Invoke();
                Attack = null;
                break;
            case UNIT_STATE.MOVE:
                Move?.Invoke();
                Move = null;
                break;
            default:
                Debug.LogWarning("State is not set properly!!!");
                break;
        }
    }

    /// <summary>
    /// Animation Event - Used for move duration, never change!!!
    /// </summary>
    private void StartMovePosition() { }

    /// <summary>
    /// Animation Event  - Used for move duration, never change!!!
    /// </summary>
    private void EndMovePosition() { }

    #endregion
}
