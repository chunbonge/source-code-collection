using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using DataEnum;
using TMPro;

[CreateAssetMenu(fileName = "UnitSelector", menuName = "GameScene/UnitSelector", order = 1)]
public class UnitSelector : BaseSelector
{
    [SerializeField] private UnitSelectorObject unitSelectArrowPrefab;
    private UnitSelectorController controller;
    private IUnitManager _unitManager;

    private List<UnitSelectorObject> arrowList = new();
    private ITarget<BaseUnit> _bag;
    private ITargetStrategy _strategy;

    private SIDE _side;
    private bool isConfirmed;
    private bool isCancled;

    public override void Init()
    {
        InputHandler inputHandler;
        ServiceLocator.For(this)
            .Get(out inputHandler)
            .Get(out _unitManager);

        _side       = SIDE.NONE;
        isConfirmed = false;
        arrowList.Clear();
        
        controller = new UnitSelectorController(
            inputHandler,
            _unitManager,
            () => isConfirmed = true,
            () => isCancled = true,
            SetUnitSelectArrow
        );
    }

    public async UniTask<bool> SelectTarget(ITarget<BaseUnit> bag, ITargetStrategy strategy, SIDE side)
    {
        _bag        = bag;
        _strategy   = strategy;
        _side       = side;
        isConfirmed = false;
        isCancled   = false;
        controller.UpdateIndex(_unitManager.GetUnit(side).Count, side);

        switch (strategy)
        {
            case SingleTargetStrategy:
            case SplashTargetStrategy:
                using (var inputDisposer = new InputDisposer(controller.InputHandler, InputHandler.InputState.SelectUnit))
                {
                    CreateUnitSelectArrow(bag, strategy, controller.GetSelectionIndex(_side));
                    controller.Prepare();
                    controller.OnStartSelect(side, strategy.Filter);
                    await UniTask.WaitUntil(() => isConfirmed == true || isCancled == true);
                    controller.OnEndSelect(side);
                    DestroyUnitSelectArrow();
                }
                break;
            case AllTargetStrategy:
                using (var inputDisposer = new InputDisposer(controller.InputHandler, InputHandler.InputState.SelectUnit))
                {
                    CreateUnitSelectArrow(bag, strategy, controller.GetSelectionIndex(_side));
                    controller.Prepare();
                    await UniTask.WaitUntil(() => isConfirmed == true || isCancled == true);
                    DestroyUnitSelectArrow();
                }
                break;
            case RandomTargetStratgy:
                strategy.SelectTarget(_unitManager.GetUnit(_side), bag);
                break;
        }

        return !isCancled;
    }

    public bool SelectRandomTarget(ITarget<BaseUnit> bag, ITargetStrategy strategy)
    {
        strategy.SelectTarget(_unitManager.GetPlayerUnits(), bag);

        if(bag.Targets.Count == 0) 
            return false;

        return true;
    }

    private void SetUnitSelectArrow(int targetIndex)
    {
        DestroyUnitSelectArrow();
        CreateUnitSelectArrow(_bag, _strategy, targetIndex);
    }

    private void CreateUnitSelectArrow(ITarget<BaseUnit> bag, ITargetStrategy strategy, int targetIndex)
    {
        strategy.SelectTarget(_unitManager.GetUnit(_side), bag, targetIndex);

        foreach(var target in bag.Targets)
        {
            UnitSelectorObject arrow = Instantiate(unitSelectArrowPrefab, target.Attachments.GetUnitSelectArrowPos(), false);
            bool IsSelectable = strategy.Filter == null || !strategy.Filter(target);
            arrow.Init(_side, IsSelectable);
            arrowList.Add(arrow);
        }
    }

    private void DestroyUnitSelectArrow()
    {
        foreach(var arrow in arrowList)
        {
            Destroy(arrow.gameObject);
        }
        arrowList.Clear();
    }
}