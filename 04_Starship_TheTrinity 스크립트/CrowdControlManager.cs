using DataEntity;
using DataEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public interface ICrowdControlManager
{
    public void AddCrowdControl(ELEMENT_TYPE element_type, BaseUnit target, BaseUnit caster);
    public void RemoveCrowdControl(ELEMENT_TYPE element_type, BaseUnit target);
}

[CreateAssetMenu(fileName = "CrowdControlManager", menuName = "Manager/CrowdControlManager", order = 1)]
public class CrowdControlManager : ScriptableObject, ICrowdControlManager
{
    public record CCContext(ElementStatusData Data, ICombatEffectManager effectManager, BaseUnit Target, BaseUnit Caster)
    {
        public ElementStatusData Data { get; } = Data;
        public ICombatEffectManager effectManager { get; } = effectManager;
        public BaseUnit Target { get; } = Target;
        public BaseUnit Caster { get; } = Caster;
    }

    private DataHandler _dataHandler;
    private ICombatEffectManager _effectManager;

    public void Init()
    {
        ServiceLocator.For(this)
            .Get(out _dataHandler)
            .Get(out _effectManager);
    }

    public void AddCrowdControl(ELEMENT_TYPE element_type, BaseUnit target, BaseUnit caster)
    {
        var previousElement = target.CCUnit.Previous_CC_Type;
        if (previousElement != element_type && previousElement != ELEMENT_TYPE.NONE)
        {
            #region [Chaos상태이상 저장]
            var chaos = target.CCUnit.GetNonStackCC(ELEMENT_TYPE.ETC) as IChaos;
            // If Chaos Element_Status_Effect not exist
            if (chaos == null)
            {
                target.CCUnit.Add(ELEMENT_TYPE.ETC, ELEMENT_STATUS_CATEGORY.CHAOS);
                var chaosCrowdControl = CrowdControlFactory.CreateCC(ELEMENT_STATUS_CATEGORY.CHAOS);
                target.CCUnit.AddNonStackCC(ELEMENT_TYPE.ETC, chaosCrowdControl);
                var chaosContext = CreateContext(chaosCrowdControl, target, caster);
                if (chaosContext != null)
                {
                    chaosCrowdControl.ApplyCrowdControl(chaosContext);
                }
            }
            // If Chaos Element_Status_Effect already exists -> Save Update Action
            else
            {
                chaos.ReapplyCrowdControl(target.CCUnit.Previous_CC_Type);
            }
            #endregion
        }

        #region [상태이상 정보만 저장]
        ELEMENT_STATUS_CATEGORY category;
        // If Same CC Stack Exist
        if (target.CCUnit.Previous_CC_Type == element_type)
        {
            category = ElementStatusRuleTable.GetEnhanced(element_type);
        }
        // If Same CC Stack Not Exist
        else
        {
            category = ElementStatusRuleTable.GetBasic(element_type);
        }
        target.CCUnit.Add(element_type, category);
        #endregion

        #region [ICrowdControl 저장]
        ICrowdControl crowdControl = CrowdControlFactory.CreateCC(category);
        var context = CreateContext(crowdControl, target, caster);
        if (context != null)
        {
            // 스택기반 상태이상 저장
            if (ElementStatusRuleTable.IsStackableElement(element_type))
            {
                crowdControl.ApplyCrowdControl(context);
                target.CCUnit.AddStackCC(element_type, crowdControl);
            }
            // 지속턴기반 상태이상 저장
            else
            {
                var cc = target.CCUnit.GetNonStackCC(element_type);
                // 만약 상태이상이 아예 저장되어있지 않거나, 중첩 상태이상이 저장되어있지 않다면
                if (cc == null || cc is IBasicCrowdControl)
                {
                    crowdControl.ApplyCrowdControl(context);
                    target.CCUnit.AddNonStackCC(element_type, crowdControl);
                }
                // 만약 AttributeControl 상태이상이 저장되어있다면 
                else
                {
                    // 새로 추가될 예정이었던 상태이상이 스탯형 상태이상이라면 Duration++
                    if (crowdControl is AttributeControl && cc is AttributeControl ca)
                    {
                        ca.AddDuration();
                    }

                    // 잠식 상태이상 로직 - 예외처리
                    if  (crowdControl is Corrode && cc is Corrode corrodeCC)
                    {
                        corrodeCC.IncreaseStack();
                    }
                }
            }
        }
        #endregion
    }

    public void RemoveCrowdControl(ELEMENT_TYPE element_type, BaseUnit target)
    {
        target.CCUnit.Remove(element_type);

        if (ElementStatusRuleTable.IsStackableElement(element_type))
        {
            target.CCUnit.RemoveStackCC(element_type);
        }
        else
        {
            target.CCUnit.RemoveNonStackCC(element_type);
        }
    }

    private CCContext CreateContext(ICrowdControl cc, BaseUnit target, BaseUnit caster)
    {
        var elementStatusData = _dataHandler.FindElementStatusData(cc.ID);

        if (elementStatusData == null)
        {
            Debug.LogWarning($"No CC Data : {cc}");
            return null;
        }

        var context = new CCContext(elementStatusData, _effectManager, target, caster);

        return context;
    }
}