using DataEnum;
using NaughtyAttributes;
using UnityEngine;
using Utils;

public interface IDamageInfo
{
    BaseUnit Attacker { get; }
    float DamageValue { get; }
    bool IsCritical { get; }
    ELEMENT_TYPE ElementType { get; }
    float ElementGaugeIncreaseValue { get; }
}

public record DamageInfo(BaseUnit Attacker, float DamageValue, bool IsCritical, ELEMENT_TYPE ElementType = default, float ElementGaugeIncreaseValue = default) : IDamageInfo
{
    public BaseUnit Attacker { get; } = Attacker;
    public float DamageValue { get; } = DamageValue;
    public bool IsCritical { get; } = IsCritical;
    public ELEMENT_TYPE ElementType { get; } = ElementType;
    public float ElementGaugeIncreaseValue { get; } = ElementGaugeIncreaseValue;
}

[CreateAssetMenu(fileName = "DamageFactory", menuName = "GameScene/DamageFactory")]
public class DamageFactory : ScriptableObject
{
    public static IDamageInfo CreateDamage<T>(BaseUnit caster, BaseUnit target) 
        where T : IDamageCalculator, new()
    {
        IDamageCalculator calculator = new T();

        var result = calculator.Calculate(caster, target);
        IDamageInfo damage = new DamageInfo(caster, result.damageValue, result.isCritical);

        return damage;
    }

    public static IDamageInfo CreateDamage<T, T2>(SKILL_ELEMENT_RATE rateType, ELEMENT_TYPE elementType, BaseUnit caster, BaseUnit target) 
        where T : IDamageCalculator, new() 
        where T2 : IElementGaugeCalculator, new()
    {
        IDamageCalculator damageCalculator = new T();
        IElementGaugeCalculator elementGaugeCalculator = new T2();

        var result1 = damageCalculator.Calculate(caster, target);

        float rate = 0.0f;
        switch(rateType)
        {
            case SKILL_ELEMENT_RATE.MINOR:
                rate = 1.0f;
                break;
            case SKILL_ELEMENT_RATE.STANDARD:
                rate = 1.3f;
                break;
            case SKILL_ELEMENT_RATE.GRAND:
                rate = 1.5f;
                break;
        }

        var result2 = elementGaugeCalculator.Calculate(caster, rate, elementType, target);
        IDamageInfo damage = new DamageInfo(caster, result1.damageValue, result1.isCritical, elementType, result2);

        return damage;
    }
}
