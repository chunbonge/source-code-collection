using ObservableCollections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;
using Unity.Collections;
using Unity.VisualScripting;
using DataEnum;

public class CombatEffectUnit
{
    public enum COMBAT_EFFECT_TYPE
    {
        NORMAL,
        CC
    }

    private readonly ObservableList<(string Name, IEffectable Value)> _normalEffectList = new();
    public IReadOnlyObservableList<(string Name, IEffectable Value)> NormalEffectList => _normalEffectList;

    private readonly Dictionary<string, Action> onEachTurnList = new();

    public void OnStartTurn()
    {
        foreach(var action in onEachTurnList)
        {
            action.Value.Invoke();
        }
    }

    public void AddOnStartTurnEffect(string name, Action action)
    {
        onEachTurnList[name] = action;
    }

    public void Add(string name, IEffectable combatEffect)
    {
        _normalEffectList.Add((name, combatEffect));
        combatEffect.OnDispose += () =>
        {
            Remove(name, combatEffect);
        };
    }

    public void Remove(string name, IEffectable combatEffect)
    {
        _normalEffectList.Remove((name, combatEffect));

        if (combatEffect is IStartTurnEffectProvider provider)
        {
            string key = provider.StartTurnKey;

            // 아직 같은 StartTurnKey를 가진 이펙트가 남아있는지 체크
            bool stillExists = _normalEffectList.ToList().Any(e => e.Value is IStartTurnEffectProvider p && p.StartTurnKey == key);

            // 하나도 남아있지 않다면 해당 키의 턴 시작 효과 제거
            if (!stillExists)
            {
                onEachTurnList.Remove(key);
            }
        }
    }
}