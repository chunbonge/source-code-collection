using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DataEntity;
using DataEnum;
using UnityEngine;

public interface IUnitManager
{
    public List<BaseUnit> GetEnemyUnits();
    public List<BaseUnit> GetPlayerUnits();
    public List<BaseUnit> GetUnit(SIDE side);
    public List<BaseUnit> GetAllUnits();
}

[CreateAssetMenu(fileName = "UnitManager", menuName = "GameScene/UnitManager", order = 2)]
public class UnitManager : ScriptableObject , IUnitManager
{
    [SerializeField] private ScriptableListBaseUnit currentUnitList = null;
    [SerializeField] private EntitySpawner spawner;
    [SerializeField] private UnitPositioner positioner;

    private List<BaseUnit> unitList = new List<BaseUnit>();

    public void Init()
    {
        spawner.Init();
    }

    public void Prepare()
    {
        foreach (BaseUnit unit in currentUnitList)
        {
            unit.m_FinishedDying += (deadUnit) => { currentUnitList.Remove(deadUnit); SetUnitPosition(); };
        }
    }
    
    public PlayerUnit CreatePlayerUnit(EntityData entityData, int index)
    {
        var playerUnit = spawner.CreatePlayerUnit(entityData, index);
        
        currentUnitList.Add(playerUnit);
        unitList.Add(playerUnit);

        SetUnitPosition();
        
        return playerUnit;
    }

    public EnemyUnit CreateEnemyUnit(EntityData entityData, int index)
    {
        var enemyUnit = spawner.CreateEnemyUnit(entityData, index);
        
        currentUnitList.Add(enemyUnit);
        unitList.Add(enemyUnit);

        SetUnitPosition();
        
        return enemyUnit;
    }

    public List<BaseUnit> GetEnemyUnits()
    {
        return currentUnitList.GetEnemyUnits();
    }

    public List<BaseUnit> GetPlayerUnits()
    {
        return currentUnitList.GetPlayerUnits();
    }

    public List<BaseUnit> GetUnit(SIDE side)
    {
        if (side == SIDE.PLAYER)
            return currentUnitList.GetPlayerUnits();
        else if (side == SIDE.ENEMY)
            return currentUnitList.GetEnemyUnits();

        return null;
    }

    public List<BaseUnit> GetAllUnits()
    {
        return currentUnitList.GetUnits();
    }

    private void SetUnitPosition()
    {
        positioner.SetPositionForUnits(currentUnitList.GetPlayerUnits(), currentUnitList.GetEnemyUnits());
    }
}