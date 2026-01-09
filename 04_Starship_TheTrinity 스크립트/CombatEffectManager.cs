using System;
using System.Collections.Generic;
using DataEnum;
using Language.Lua;
using UnityEngine;
using static CombatEffectUnit;
using static CombatEffectManager;
using System.Linq;
using NUnit.Framework;
using VolFx;

public interface ICombatEffectManager
{
    public IEffectable AddCombatEffect(string ID, EffectInfo info, EffectContext context);
}

[CreateAssetMenu(fileName = "CombatEffectManager", menuName = "Manager/CombatEffectManager", order = 1)]
public class CombatEffectManager : ScriptableObject, ICombatEffectManager
{
    public record EffectInfo(string Name, float Value = 0.0f, int Duration = 0)
    {
        public string Name { get; } = Name;
        public float Value { get; } = Value;
        public int Duration { get; } = Duration;
    }
    public record EffectContext(BaseUnit Target, BaseUnit Caster)
    {
        public BaseUnit Target { get; } = Target;
        public BaseUnit Caster { get; } = Caster;
    }

    public IEffectable AddCombatEffect(string ID, EffectInfo info, EffectContext context)
    {
        BaseUnit target = context.Target;
        IEffectable combatEffect = null;

        var factory = EffectFactoryData.GetEffectFactory(ID);
        if (factory != null)
        {
            combatEffect = factory.Invoke();
            target.CEUnit.Add(info.Name, combatEffect);
        }

        combatEffect?.Apply(info, context);
        return combatEffect;
    }
}