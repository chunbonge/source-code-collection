using System.Collections.Generic;
using DataEntity;
using DataEnum;
using UnityEngine;
using static CrowdControlManager;
using static CombatEffectManager;
using static CombatEffectUnit;
using System.Linq;
using System;
using R3;

public interface ICrowdControl
{
    public string ID { get; }
    public ElementStatusData CCData { get; }
    public void ApplyCrowdControl(CCContext context);
    public void Dispose();
}

public interface IBasicCrowdControl { }
public interface IEnhancedCrowdControl { }

public interface AttributeControl
{
    public IEffectable Effect { get; }
    public void AddDuration();
}

public abstract class CrowdControlBase : ICrowdControl
{
    public ElementStatusData CCData { get; private set; }

    public virtual void ApplyCrowdControl(CCContext context)
    {
        CCData = context.Data;
    }

    public virtual void Dispose() { }
    public abstract string ID { get; }
}

public abstract class AttributeCrowdControlBase : CrowdControlBase, AttributeControl
{
    protected EffectInfo Info;
    public IEffectable Effect { get; private set; }

    public override void ApplyCrowdControl(CCContext context)
    {
        base.ApplyCrowdControl(context);

        Info = CreateEffectInfoList(context.Data);
        Effect = context.effectManager.AddCombatEffect(EffectID, Info, new EffectContext(context.Target, context.Caster));
    }

    public override void Dispose()
    {
        Effect?.Dispose();
        Effect = null;
    }

    public void AddDuration()
    {
        Effect.ExtendTimerInternal(1);
    }

    protected abstract string EffectID { get; }
    public abstract EffectInfo CreateEffectInfoList(ElementStatusData data);
}

public class Stun : CrowdControlBase, IBasicCrowdControl
{
    public override string ID => "40011001";
}

public class Burn : AttributeCrowdControlBase, IBasicCrowdControl
{
    public override string ID => "40011002";
    protected override string EffectID => "Burn";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Contamination : AttributeCrowdControlBase, IBasicCrowdControl
{
    public override string ID => "40011003";
    protected override string EffectID => "Contamination";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Suppress : AttributeCrowdControlBase, IBasicCrowdControl
{
    public override string ID => "40011004";
    protected override string EffectID => "Suppress";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Strange : CrowdControlBase, IBasicCrowdControl
{
    public override string ID => "40011005";
}

public class Silence : CrowdControlBase, IBasicCrowdControl
{
    public override string ID => "40011006";
}

public class Weakness : AttributeCrowdControlBase, IEnhancedCrowdControl
{
    public override string ID => "40022001";
    protected override string EffectID => "Weakness";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Overheat : AttributeCrowdControlBase, IEnhancedCrowdControl
{
    public override string ID => "40022002";
    protected override string EffectID => "Burn";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Exposure : AttributeCrowdControlBase, IEnhancedCrowdControl
{
    public override string ID => "40022003";
    protected override string EffectID => "Contamination";


    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Bind : AttributeCrowdControlBase, IEnhancedCrowdControl
{
    public override string ID => "40022004";
    protected override string EffectID => "Suppress";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public class Corrode : CrowdControlBase, IEnhancedCrowdControl
{
    public override string ID => "40022005";
    public int Count { get; private set; } = 1;
    public event Action OnDispose = delegate { };

    public override void ApplyCrowdControl(CCContext context)
    {
        base.ApplyCrowdControl(context);
    }

    public override void Dispose()
    {
        OnDispose.Invoke();
    }

    public void IncreaseStack() => Count++;
    public void DecreaseStack() => Count--;
}

public class Dominate : AttributeCrowdControlBase, IEnhancedCrowdControl
{
    public override string ID => "40022006";
    protected override string EffectID => "Dominate";

    public override EffectInfo CreateEffectInfoList(ElementStatusData data)
    {
        return new EffectInfo
        (
            data.Element_Status_Name,
            (float)data.Element_Status_Value[0],
            data.Element_Status_Duration_Turn
        );
    }
}

public interface IChaos
{
    public void ReapplyCrowdControl(ELEMENT_TYPE elementType);
}

public class Chaos : CrowdControlBase, IChaos
{
    public override string ID => "40033001";
    public IEffectable Effect { get; private set; }

    private ELEMENT_TYPE elementType;

    private readonly Dictionary<ELEMENT_TYPE, string> chaosIDs = new Dictionary<ELEMENT_TYPE, string>()
    {
        { ELEMENT_TYPE.PHYSICAL, "PhysicalGaugeResistance" },
        { ELEMENT_TYPE.FIRE, "FireGaugeResistance" },
        { ELEMENT_TYPE.RADIATION, "RadiationGaugeResistance" },
        { ELEMENT_TYPE.GRAVITY, "GravityGaugeResistance" },
        { ELEMENT_TYPE.VOID, "VoidGaugeResistance" },
        { ELEMENT_TYPE.HOLY, "HolyGaugeResistance" }
    };

    private Func<IEffectable> EffectCreator;

    public override void ApplyCrowdControl(CCContext context)
    {
        elementType = context.Target.CCUnit.Previous_CC_Type;

        var effectInfo = new EffectInfo(context.Data.Element_Status_Name, (float)context.Data.Element_Status_Value[0], context.Data.Element_Status_Duration_Turn);

        EffectCreator = () =>
        {
            return context.effectManager.AddCombatEffect(
                chaosIDs[elementType],
                effectInfo,
                new EffectContext(context.Target, context.Caster));
        };

        Effect = EffectCreator();
    }

    public void ReapplyCrowdControl(ELEMENT_TYPE elementType)
    {
        if (EffectCreator == null) return;

        Dispose();
        this.elementType = elementType;
        Effect = EffectCreator();
    }

    public override void Dispose()
    {
        Effect?.Dispose();
        Effect = null;
    }
}