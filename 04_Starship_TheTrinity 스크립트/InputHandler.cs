using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UserInputAction;

[CreateAssetMenu(fileName = "InputHandler", menuName = "Core/InputHandler", order = 1)]
public class InputHandler : ScriptableObject, UserInputAction.ISelectUnitActions, UserInputAction.ISelectActionActions, UserInputAction.IQTEActions, UserInputAction.IParryActions, UserInputAction.ISelectPlanetActions, UserInputAction.ISkipActions
{
    private UserInputAction _userInputAction;

    public enum InputState
    {
        None,
        SelectUnit,
        SelectAction,
        QTE,
        Parry,
        SelectPlanet,
        Skip
    }

    private InputState _currentInputState = InputState.None;

    public InputState CurrentInputState
    {
        get => _currentInputState;

        set
        {
            switch (CurrentInputState)
            {
                case InputState.None:
                    break;
                case InputState.SelectUnit:
                    DisposeOnSelectUnitActions();
                    break;
                case InputState.SelectAction:
                    DisposeOnSelectActionActions();
                    break;
                case InputState.QTE:
                    DisposeQTEActions();
                    break;
                case InputState.Parry:
                    DisposeParryActions();
                    break;
                case InputState.SelectPlanet:
                    DisposeOnSelectPlanetActions();
                    break;
                case InputState.Skip:
                    DisposeOnSkipActions();
                    break;
            }

            _currentInputState = value;
            
            switch (value)
            {
                case InputState.None:
                    _userInputAction.Disable();
                    break;
                case InputState.SelectUnit:
                    _userInputAction.Disable();
                    _userInputAction.SelectUnit.Enable();
                    break;
                case InputState.SelectAction:
                    _userInputAction.Disable();
                    _userInputAction.SelectAction.Enable();
                    break;
                case InputState.QTE:
                    _userInputAction.Disable();
                    _userInputAction.QTE.Enable();
                    break;
                case InputState.Parry:
                    _userInputAction.Disable();
                    _userInputAction.Parry.Enable();
                    break;
                case InputState.SelectPlanet:
                    _userInputAction.Disable();
                    _userInputAction.SelectPlanet.Enable();
                    break;
                case InputState.Skip:
                    _userInputAction.Disable();
                    _userInputAction.Skip.Enable();
                    break;
            }
        }
    }

    public void Init()
    {
        if (_userInputAction == null)
        {
            _userInputAction = new UserInputAction();
            _userInputAction.SelectUnit.SetCallbacks(this);
            _userInputAction.SelectAction.SetCallbacks(this);
            _userInputAction.QTE.SetCallbacks(this);
            _userInputAction.Parry.SetCallbacks(this);
            _userInputAction.SelectPlanet.SetCallbacks(this);
            _userInputAction.Skip.SetCallbacks(this);
        }

        CurrentInputState = InputState.None;
    }

    #region[Action - SelectUnit]

    public event Action<int> OnSelectUnitEnemySelectionMove;
    public event Action<int> OnSelectUnitPlayerSelectionMove;
    public event Action OnSelectUnitTouch;
    public event Action OnSelectUnitSelectionConfirm;
    public event Action OnSelectUnitSelectionCancle;

    public void OnEnemySelectionMove(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectUnitEnemySelectionMove?.Invoke((int)context.ReadValue<float>());
    }

    public void OnPlayerSelectionMove(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectUnitPlayerSelectionMove?.Invoke((int)context.ReadValue<float>());
    }

    public void OnTouch(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectUnitTouch?.Invoke();
    }

    public void OnSelectionConfirm(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectUnitSelectionConfirm?.Invoke();
    }

    public void OnSelectionCancle(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectUnitSelectionCancle?.Invoke();
    }

    private void DisposeOnSelectUnitActions()
    {
        OnSelectUnitEnemySelectionMove  = null;
        OnSelectUnitPlayerSelectionMove = null;
        OnSelectUnitTouch               = null;
        OnSelectUnitSelectionConfirm    = null;
        OnSelectUnitSelectionCancle     = null;
    }

    #endregion

    #region[Action - SelectAction]

    public event Action OnSelectActionBaseAttack;
    public event Action OnSelectActionSkillSelect;
    public event Action OnSelectActionUseItem;

    public void OnBaseAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectActionBaseAttack?.Invoke();
    }

    public void OnSkillSelect(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectActionSkillSelect?.Invoke();
    }

    public void OnUseItem(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnSelectActionUseItem?.Invoke();
    }

    private void DisposeOnSelectActionActions()
    {
        OnSelectActionBaseAttack    = null;
        OnSelectActionSkillSelect   = null;
        OnSelectActionUseItem       = null;
    }

    #endregion

    #region[Action - QTE]

    public event Action OnQTEButtonA;

    public void OnButtonA(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnQTEButtonA?.Invoke();
    }

    private void DisposeQTEActions()
    {
        OnQTEButtonA = null;
    }

    #endregion

    #region[Action - Parry]

    public event Action OnParry;

    public void OnPerformParry(InputAction.CallbackContext context)
    {
        if (context.performed)
            OnParry?.Invoke();
    }

    private void DisposeParryActions()
    {
        OnParry = null;
    }

    #endregion

    #region[Action - SelectPlanet]

    public event Action<float> OnMoveToPlanetAction;
    public event Action<Vector3> OnControlSpaceshipAction;
    public void OnMoveToPlanet(InputAction.CallbackContext context)
    {
        if(context.performed)
        {
            OnMoveToPlanetAction?.Invoke(context.ReadValue<float>());
        }
    }

    public void OnControlSpaceship(InputAction.CallbackContext context)
    {
        if(context.performed || context.canceled)
        {
            OnControlSpaceshipAction?.Invoke(context.ReadValue<Vector3>());
        }
    }

    private void DisposeOnSelectPlanetActions()
    {
        OnMoveToPlanetAction = null;
        OnControlSpaceshipAction = null;
    }

    #endregion

    #region[Action - Skip]

    public event Action OnSkipSkip;
    public void OnSkip(InputAction.CallbackContext context)
    {
        if(context.performed)
            OnSkipSkip?.Invoke();
    }

    public void DisposeOnSkipActions()
    {
        OnSkipSkip = null;
    }

    #endregion
}