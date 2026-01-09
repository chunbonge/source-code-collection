using Cysharp.Threading.Tasks;
using DataEnum;
using System;
using UnityEngine;

public interface ISelectorExecutor
{
    public Type SelectorType { get; }
    public UniTask<bool> ExecuteWith(BaseSelector selector);
}

public abstract class SelectorExecutor<TSelector> : ISelectorExecutor where TSelector : BaseSelector
{
    public Type SelectorType => typeof(TSelector);
    public async UniTask<bool> ExecuteWith(BaseSelector selector) => await Execute((TSelector)selector);
    public abstract UniTask<bool> Execute(TSelector selector);
}

public class ActionSelectorExecutor : SelectorExecutor<ActionSelector>
{
    private readonly BaseUnit _currentUnit;
    private readonly Action<IUnitAction> _onSelected;

    public ActionSelectorExecutor(BaseUnit unit, Action<IUnitAction> onSelected)
    {
        _currentUnit = unit;
        _onSelected = onSelected;
    }

    public override async UniTask<bool> Execute(ActionSelector selector)
    {
        if (_currentUnit is PlayerUnit player)
            await selector.SelectAction(player, _onSelected);
        else if (_currentUnit is EnemyUnit enemy)
            selector.SelectAction(enemy, _onSelected);

        return true;
    }
}

public class UnitSelectorExecutor : SelectorExecutor<UnitSelector>
{
    private readonly BaseUnit _currentUnit;
    private readonly CombatSelectionContext _context;
    private readonly Action<ITarget<BaseUnit>> _onSelected;

    public UnitSelectorExecutor(BaseUnit unit, CombatSelectionContext context, Action<ITarget<BaseUnit>> onSelected)
    {
        _currentUnit = unit;
        _context = context;
        _onSelected = onSelected;
    }

    public override async UniTask<bool> Execute(UnitSelector selector)
    {
        ITarget<BaseUnit> target = new TargetFactory().CreateTarget(_context.Action.Action_Type);
        ITargetStrategy targetStrategy = new TargetStrategyFactory().CreateTargetStrategy(_context.Action.Action_Type, _context.Action.Target_Filter);

        bool isSelected = false;
        if (_context.Action.Target_Type != SIDE.NONE)
        {
            if (_currentUnit is PlayerUnit)
            {
                isSelected = await selector.SelectTarget(target, targetStrategy, _context.Action.Target_Type);
            }
            else if (_currentUnit is EnemyUnit)
            {
                targetStrategy = new TargetStrategyFactory().CreateTargetStrategy(TARGET_TYPE.RANDOM, _context.Action.Target_Filter);
                isSelected = selector.SelectRandomTarget(target, targetStrategy);
            }
        }
        else
        {
            isSelected = true;
        }

        _onSelected?.Invoke(target);

        return isSelected;
    }
}
