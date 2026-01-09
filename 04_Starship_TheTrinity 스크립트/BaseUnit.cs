using DataEntity;
using DataEnum;
using UnityEngine;
using System;
using System.Linq;
using ObservableCollections;
using R3;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections.Generic;

public interface IUpdatable
{
    public void OnRoundUpdate();
    public void OnTurnUpdate();
}

[RequireComponent(typeof(UnitAttachments))]
public abstract class BaseUnit : MonoBehaviour, IUpdateTimeline
{
    [SerializeField, Required]
    private AnimationHandler _animHandler;
    [SerializeField, ShowIf("HasSupporter"), Required]
    protected SupporterUnit _supporterUnit;

    [SerializeField] private UNIT_TYPE _unitType;
    [SerializeField] private float hitBlendAmount = 0.75f;
    [SerializeField] private float hitBlendDuration = 0.25f;
    [SerializeField] private bool HasSupporter = false;

    [SerializeField] 
    private UnitSoundContainer _soundContainer;
    public UnitSoundContainer SoundContainer => _soundContainer;
    private UnitAttachments _attachments;
    public UnitAttachments Attachments => _attachments;

    protected UnitStat _stat;
    protected ISoundService _soundService;
    protected ICombatTextManager _textManager;
    protected ICrowdControlManager _crowdControlManager;

    public UnitCombatInfo CombatInfo;
    public CrowdControlUnit CCUnit;
    public CombatEffectUnit CEUnit;

    public Action<BaseUnit> m_FinishedDying;
    private readonly List<IUpdatable> _updatableList = new();

    // temp
    public ELEMENT_TYPE My_Temp_Type;

    private const string HIT_BLEND_TWEEN_ID = "HIT_BLEND";

    public void Initialize(EntityData data, int priority)
    {
        _attachments = GetComponent<UnitAttachments>();
        CombatInfo = new UnitCombatInfo();
        CCUnit = new CrowdControlUnit();
        CEUnit = new CombatEffectUnit();
        _stat = new UnitStat(data, priority);

        My_Temp_Type = data.Temp_Type;
        Attachments.GetSpriteRenderer().material = new Material(Attachments.GetSpriteRenderer().material);

        _animHandler.Init(_soundContainer);

        ServiceLocator.For(this)
            .Get(out _soundService)
            .Get(out _textManager)
            .Get(out _crowdControlManager);

        TimelinePublisher.SubscribeObserver(this);

        _updatableList.Add(_stat.ModifierStat.Mediator);
        _updatableList.Add(CCUnit);

        if (HasSupporter)
            _supporterUnit.Initialize();
    }

    public void GetDamage(IDamageInfo damageInfo)
    {
        _stat.GetDamaged(damageInfo.DamageValue);
        CombatInfo.LastAttacker = damageInfo.Attacker;

        var material = Attachments.GetSpriteRenderer().material;
        DOTween.Kill(_stat.CoreStat.Name + HIT_BLEND_TWEEN_ID);
        material.SetFloat("_HitEffectBlend", hitBlendAmount);
        material.DOFloat(0.0f, "_HitEffectBlend", hitBlendDuration)
            .SetEase(Ease.Linear)
            .SetId(_stat.CoreStat.Name + HIT_BLEND_TWEEN_ID);

        if (this is PlayerUnit)
        {
            _animHandler.ChangeAnimation(ANIMATION.HIT);
        }

        if (HasSupporter)
        {
            _supporterUnit.OnDamaged();
        }

        _textManager.OnDamage(this, damageInfo);

        if (_stat.HP <= 0f)
        {
            OnDie().Forget();
            return;
        }

        if(damageInfo.ElementType != ELEMENT_TYPE.NONE)
        {
            _stat.IncreaseElementGauge(damageInfo.ElementType, damageInfo.ElementGaugeIncreaseValue);
        }
    }

    public void GetHeal(float value)
    {
        _stat.GetHealed(value);

        // TODO : Heal Logic
    }

    public void OnAttack(int cost = 0)
    {
        if (cost == 0)
        {
            _stat.OnNormalAttack();
        }
        else
        {
            _stat.OnSkillAttack(cost);
        }
    }

    public void OnElementGaugeFull(ELEMENT_TYPE elementType)
    {
        if(elementType != ELEMENT_TYPE.NONE && elementType != ELEMENT_TYPE.ETC)
        {
            _crowdControlManager.AddCrowdControl(elementType, this, CombatInfo.LastAttacker);
        }
    }

    public async virtual UniTask OnRevive()
    {
        // TODO : Revive Logic
    }

    public async virtual UniTask OnDie()
    {
        if (HasSupporter)
            _supporterUnit.OnDie(CombatInfo).Forget();

        PlayDeathSound();

        using (var eventDisposer = new EventDisposer(new CombatEvent("DeathEvent")))
        {
            Attachments.GetSpriteRenderer().sortingLayerName = "Actor";

            await _animHandler.ChangeAnimationAsync(ANIMATION.DEATH);
            await OnFinshedDeathAnim();
            Attachments.GetSpriteRenderer().sortingLayerName = "Character";
        }

        TimelinePublisher.DiscribeObserver(this);
    }

    public void RoundUpdate()
    {
        foreach (var updatable in _updatableList)
        {
            updatable.OnRoundUpdate();
        }
    }

    public void TurnUpdate()
    {
        foreach (var updatable in _updatableList)
        {
            updatable.OnTurnUpdate();
        }
    }

    public AnimationHandler GetAnimationHandler() => _animHandler;
    public UnitStat GetStat() => _stat;
    public UNIT_TYPE GetUnitType() => _unitType;

    protected abstract UniTask OnFinshedDeathAnim();
    protected abstract void PlayDeathSound();
}