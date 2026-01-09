using System;
using System.Collections.Generic;
using DataEnum;
using R3;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UnitSelectorController
{
    public InputHandler InputHandler => _inputHandler;
    private InputHandler _inputHandler;
    private IUnitManager _unitManager;

    private UnityAction confirm;
    private UnityAction cancle;
    private UnityAction<int> select;

    private int _selectedUnitIndex;
    private int _previousEnemySelectionIndex;
    private int _previousPlayerSelectionIndex;

    private int _maxUnitCount;

    private SIDE targetSide = SIDE.NONE;
    private Func<BaseUnit, bool> _filter;

    public UnitSelectorController(InputHandler inputHandler, IUnitManager unitManager, UnityAction confirm, UnityAction cancle, UnityAction<int> select)
    {
        _inputHandler = inputHandler;
        _unitManager = unitManager;

        this.confirm = confirm;
        this.cancle = cancle;
        this.select = select;
    }

    public void Prepare()
    {
        _inputHandler.OnSelectUnitSelectionConfirm += () => confirm();
        _inputHandler.OnSelectUnitSelectionCancle += () => cancle();
    }

    public void UpdateIndex(int maxUnitCount, SIDE side)
    {
        _maxUnitCount = maxUnitCount;

        if (side == SIDE.ENEMY)
            _previousEnemySelectionIndex = _previousEnemySelectionIndex > _maxUnitCount - 1 ? _maxUnitCount - 1 : _previousEnemySelectionIndex;
        else if (side == SIDE.PLAYER)
            _previousPlayerSelectionIndex = _previousPlayerSelectionIndex > _maxUnitCount - 1 ? _maxUnitCount - 1 : _previousPlayerSelectionIndex;
    }

    public void OnStartSelect(SIDE side, Func<BaseUnit, bool> filter)
    {
        targetSide = side;
        _filter = filter;

        if (side == SIDE.ENEMY)
        {
            _selectedUnitIndex = _previousEnemySelectionIndex;
            _inputHandler.OnSelectUnitEnemySelectionMove += OnUnitSelect;
        }
        else if (side == SIDE.PLAYER)
        {
            _selectedUnitIndex = _previousPlayerSelectionIndex;
            _inputHandler.OnSelectUnitPlayerSelectionMove += OnUnitSelect;
        }

        _inputHandler.OnSelectUnitTouch += OnUnitTouch;
    }

    public void OnEndSelect(SIDE side)
    {
        targetSide = SIDE.NONE;
        _filter = null;

        if (side == SIDE.ENEMY)
            _previousEnemySelectionIndex = _selectedUnitIndex;
        else if (side == SIDE.PLAYER)
            _previousPlayerSelectionIndex = _selectedUnitIndex;
    }

    private void OnUnitTouch()
    {
        if (targetSide == SIDE.NONE) return;

        Vector2 screen = Pointer.current.position.ReadValue();
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(screen);
        LayerMask mask = LayerMask.GetMask("Character");
        var hit = Physics2D.Raycast(mousePos, Vector2.zero, 100.0f, mask);

        if (hit.collider)
        {
            var unitList = _unitManager.GetUnit(targetSide);
            var unit = hit.collider.GetComponentInParent<BaseUnit>();

            int idx = unitList.IndexOf(unit);
            if (idx < 0)
                return;

            bool selectable = _filter == null || !_filter(unit);

            if (idx != _selectedUnitIndex)
            {
                _selectedUnitIndex = idx;
                select(_selectedUnitIndex);
                return;
            }

            if (selectable)
                confirm();
        }
    }

    private void OnUnitSelect(int value)
    {
        int temp = _selectedUnitIndex;
        _selectedUnitIndex = Mathf.Clamp(_selectedUnitIndex + value, 0, _maxUnitCount - 1);

        if (_selectedUnitIndex != temp)
            select(_selectedUnitIndex);
    }

    public int GetSelectionIndex(SIDE side)
    {
        if (side == SIDE.PLAYER)
            return _previousPlayerSelectionIndex;
        else if (side == SIDE.ENEMY)
            return _previousEnemySelectionIndex;
        else
            return 0;
    }
}
