using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using DataEnum;
using UnityEngine;

public class CombatSelectionContext
{
    public IUnitAction Action;
    public ITarget<BaseUnit> Target;
}

[CreateAssetMenu(fileName = "CombatManager", menuName = "GameScene/CombatManager", order = 1)]
public class CombatManager : ScriptableObject
{
    [SerializeField] private TimelineManager timelineManagerPrefab;

    private TimelineManager _timelineManager;
    private BaseUnit currentTurnUnit;

    private EventRegistry<List<BaseUnit>, BaseUnit> DequeueCurrentUnit = new();

    public bool executed;

    private IUnitManager _unitManager;
    private IUnitActionExecuter _actionExecuter;
    private ICombatTextManager _textManager;
    private ISelectorManager _selectorManager;
    private ISoundService _soundService;

    public void Init()
    {
        _timelineManager = Instantiate(timelineManagerPrefab);

        ServiceLocator.For(this)
                      .Get(out _unitManager)
                      .Get(out _actionExecuter)
                      .Get(out _textManager)
                      .Get(out _selectorManager)
                      .Get(out _soundService);

        _timelineManager.Init();
    }

    public void CreateObjects()
    {
        _timelineManager.CreateTimeline(_unitManager.GetAllUnits());
    }

    public void Prepare()
    {
        _timelineManager.Prepare(_unitManager.GetAllUnits());
        DequeueCurrentUnit.Register(_timelineManager.Pop);

        foreach (BaseUnit unit in _unitManager.GetAllUnits())
        {
            unit.m_FinishedDying += OnCharacterDie;
        }
    }

    public async UniTask StartCombat()
    {
        while (_unitManager.GetEnemyUnits().Count != 0 && _unitManager.GetPlayerUnits().Count != 0)
        {
            currentTurnUnit = DequeueCurrentUnit.Call(_unitManager.GetAllUnits());

            currentTurnUnit.CEUnit.OnStartTurn();
            await UniTask.WaitUntil(() => EventHandler.IsEventEmpty());

            Debug.Log($"{currentTurnUnit.GetStat().CoreStat.Name}'s turn");
            await UniTask.WaitUntil(() => !_textManager.IsTextOn);

            ForDebugControlEffect(currentTurnUnit, ELEMENT_TYPE.PHYSICAL);
            _selectorManager.UpdateActionSelector(currentTurnUnit);

            if (currentTurnUnit.CCUnit.EffectsCountDic[ELEMENT_TYPE.PHYSICAL].CurrentValue <= 0)
            {
                var context = new CombatSelectionContext();
                // Step 1 : Add Selections
                _selectorManager.AddSelectorExecuter(new ActionSelectorExecutor(currentTurnUnit, (action) => context.Action = action));
                _selectorManager.AddSelectorExecuter(new UnitSelectorExecutor(currentTurnUnit, context, (bag) => context.Target = bag));

                // Step 2 : Execute Selections
                await _selectorManager.ExecuteAll();

                // Step 3 : Execute Action to Target
                await _actionExecuter.ExecuteRequest(currentTurnUnit, context.Action, context.Target);
            }

            await UniTask.WaitUntil(() => EventHandler.IsEventEmpty());
            await UniTask.WaitForSeconds(1);

            currentTurnUnit.TurnUpdate();
        }

        DequeueCurrentUnit.UnregisterAll();
        // TODO: Check whether the enemy or the player wins
        // if()
    }

    private void ForDebugControlEffect(BaseUnit unit, ELEMENT_TYPE type)
    {
        var controlCount = unit.CCUnit.EffectsCountDic[type].CurrentValue;
        if (controlCount > 0)
        {
            string str = $"[{type}]\nStun : {unit.CCUnit.EffectsCountDic[type].CurrentValue}";
            var cc = unit.CCUnit.GetNonStackCC(type);
            if (cc != null && cc is AttributeControl ca)
            {
                str += $"\nWeakness : {ca.Effect.TimerTmp.Remain}";
            }
            Debug.Log(str);
        }
    }

    public void OnCharacterDie(BaseUnit unit)
    {
        _timelineManager.DeleteBanners(unit);
        if (currentTurnUnit == unit)
            currentTurnUnit = DequeueCurrentUnit.Call(_unitManager.GetAllUnits());
    }

    public void OnFainting()
    {
        //_timeline.Actions.FaintingButton();
    }

    public void OnExtraTurn()
    {
        //_timeline.Actions.ExtraButton();
    }
}