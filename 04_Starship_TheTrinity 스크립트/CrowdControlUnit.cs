using DataEnum;
using ObservableCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;
using static CrowdControlManager;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class CrowdControlUnit : IUpdatable
{
    // 현재 캐릭터한테 적용된 상태이상이 무엇인지 저장하기 위한 딕셔너리
    private readonly Dictionary<ELEMENT_TYPE, List<ELEMENT_STATUS_CATEGORY>> _currentEffects = new()
        {
            {ELEMENT_TYPE.PHYSICAL, new List<ELEMENT_STATUS_CATEGORY>() },
            {ELEMENT_TYPE.FIRE, new List<ELEMENT_STATUS_CATEGORY>() },
            {ELEMENT_TYPE.RADIATION, new List<ELEMENT_STATUS_CATEGORY>() },
            {ELEMENT_TYPE.GRAVITY, new List<ELEMENT_STATUS_CATEGORY>() },
            {ELEMENT_TYPE.VOID, new List<ELEMENT_STATUS_CATEGORY>() },
            {ELEMENT_TYPE.HOLY, new List<ELEMENT_STATUS_CATEGORY>() },
            {ELEMENT_TYPE.ETC, new List<ELEMENT_STATUS_CATEGORY>() }
        };
    public IReadOnlyDictionary<ELEMENT_TYPE, IReadOnlyList<ELEMENT_STATUS_CATEGORY>> CurrentEffects =>
        _currentEffects.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<ELEMENT_STATUS_CATEGORY>)kv.Value);

    // 캐릭터한테 적용된 상태이상의 중첩 또는 지속 턴 수를 저장하기 위한 딕셔너리
    private readonly Dictionary<ELEMENT_TYPE, ReactiveProperty<int>> _effectsCountDic = new()
    {
        { ELEMENT_TYPE.PHYSICAL, new ReactiveProperty<int>(0) },
        { ELEMENT_TYPE.FIRE, new ReactiveProperty<int>(0) },
        { ELEMENT_TYPE.RADIATION, new ReactiveProperty<int>(0) },
        { ELEMENT_TYPE.GRAVITY, new ReactiveProperty<int>(0) },
        { ELEMENT_TYPE.VOID, new ReactiveProperty<int>(0) },
        { ELEMENT_TYPE.HOLY, new ReactiveProperty<int>(0) },
        { ELEMENT_TYPE.ETC, new ReactiveProperty<int>(0) }
    };
    public IReadOnlyDictionary<ELEMENT_TYPE, ReadOnlyReactiveProperty<int>> EffectsCountDic =>
        _effectsCountDic.ToDictionary(kv => kv.Key, kv => kv.Value.ToReadOnlyReactiveProperty());

    private readonly Dictionary<ELEMENT_TYPE, List<ICrowdControl>> _stackableCC = new();
    private readonly Dictionary<ELEMENT_TYPE, ICrowdControl> _nonStackableCC = new();
    private readonly Dictionary<ELEMENT_TYPE, ICrowdControl> _pendingNonStackableCC = new();

    public ICrowdControl GetNonStackCC(ELEMENT_TYPE type) => _nonStackableCC.TryGetValue(type, out var cc) ? cc : null;
    public ELEMENT_TYPE Previous_CC_Type { get; private set; } = ELEMENT_TYPE.NONE;

    public void Add(ELEMENT_TYPE elementType, ELEMENT_STATUS_CATEGORY category)
    {
        if (elementType != ELEMENT_TYPE.ETC)
            Previous_CC_Type = elementType;

        _effectsCountDic[elementType].Value++;
        _currentEffects[elementType].Add(category);
    }

    public void Remove(ELEMENT_TYPE elementType)
    {
        _effectsCountDic[elementType].Value = 0;
        _currentEffects[elementType].Clear();

        if (!CheckAnyEffects())
            Previous_CC_Type = ELEMENT_TYPE.NONE;
    }

    // 중첩이 가능한 상태이상 저장용
    public void AddStackCC(ELEMENT_TYPE type, ICrowdControl cc)
    {
        _stackableCC.TryAdd(type, new List<ICrowdControl>());
        _stackableCC[type].Add(cc);
    }

    // 중첩이 되지 않는(지속 턴이 존재하는) 상태이상 저장용
    public void AddNonStackCC(ELEMENT_TYPE type, ICrowdControl cc)
    {
        _nonStackableCC.TryGetValue(type, out var pending);
        _nonStackableCC[type] = cc;

        // 스택형 상태이상의 OnDispose로직
        if (cc is AttributeControl ac)
        {
            _pendingNonStackableCC[type] = pending;
            ac.Effect.OnDispose += () => 
            { 
                _nonStackableCC[type] = _pendingNonStackableCC[type];
                _pendingNonStackableCC.Remove(type);
            };
        }

        // 공허 속성 상태이상의 OnDispose로직
        if (cc is Corrode corrodeCC)
        {
            _pendingNonStackableCC[type] = pending;
            corrodeCC.OnDispose += () =>
            {
                _nonStackableCC[type] = _pendingNonStackableCC[type];
                _pendingNonStackableCC.Remove(type);
            };
        }
    }

    public void RemoveStackCC(ELEMENT_TYPE type)
    {
        if (_stackableCC.TryGetValue(type, out var list))
        {
            foreach (var cc in list)
                cc.Dispose();
            list.Clear();
        }
        _stackableCC.Remove(type);
    }

    public void RemoveNonStackCC(ELEMENT_TYPE type)
    {
        if (_nonStackableCC.TryGetValue(type, out var cc))
        {
            cc.Dispose();
        }
        _nonStackableCC.Remove(type);
    }

    // Not Using
    public void OnRoundUpdate() { }

    // This method operate every turn
    public void OnTurnUpdate()
    {
        Reduce(ELEMENT_TYPE.PHYSICAL);
        Reduce(ELEMENT_TYPE.VOID);
        Reduce(ELEMENT_TYPE.HOLY);
        Reduce(ELEMENT_TYPE.ETC);
    }

    // 지속 턴이 존재하는 상태이상 감소 로직
    private void Reduce(ELEMENT_TYPE elementType)
    {
        if (_effectsCountDic[elementType].Value <= 0)
            return;

        _effectsCountDic[elementType].Value--;

        // 잠식 상태이상 감소 로직
        if (elementType == ELEMENT_TYPE.VOID)
        {
            var cc = GetNonStackCC(elementType);
            if(cc != null && cc is Corrode corrodeCC)
            {
                corrodeCC.DecreaseStack();
                if(corrodeCC.Count <= 0)
                {
                    corrodeCC.Dispose();
                }
            }
        }

        // 상태이상 정보 감소 후 갱신
        if (!ReduceCurrentEffects(elementType))
        {
            Debug.Log("Unexpected Error Ocurred!!!");
        }

        // 만약 상태이상 Count가 0이 되면 상태이상 컨테이너에서 상태이상 제거
        if (_effectsCountDic[elementType].Value <= 0)
        {
            _nonStackableCC.Remove(elementType);

            if (!CheckAnyEffects())
                Previous_CC_Type = ELEMENT_TYPE.NONE;
        }
    }

    // -1 : Unexpected Error, 0 : Failed, 1 : Success
    private bool ReduceCurrentEffects(ELEMENT_TYPE elementType)
    {
        if (!_currentEffects.TryGetValue(elementType, out var list) || !_effectsCountDic.TryGetValue(elementType, out var countRp))
            return false;

        // if Element Type is Chaos
        if (elementType == ELEMENT_TYPE.ETC)
        {
            list.RemoveAt(list.Count - 1);

            return true;
        }

        // if Element Type is Normal
        if (!list.Remove(ElementStatusRuleTable.GetEnhanced(elementType)))
        {
            list.Remove(ElementStatusRuleTable.GetBasic(elementType));
        }

        return true;
    }

    private bool CheckAnyEffects() => CurrentEffects.Any(kv => kv.Value.Count > 0);
}