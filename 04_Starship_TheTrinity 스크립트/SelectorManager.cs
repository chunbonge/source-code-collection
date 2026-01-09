using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Linq;

public interface ISelectorManager
{
    public void UpdateActionSelector(BaseUnit unit);
    public void AddSelectorExecuter(ISelectorExecutor executer);
    public UniTask ExecuteAll();
}

[CreateAssetMenu(fileName = "SelectorManager", menuName = "Selector/SelectorManager")]
public class SelectorManager : ScriptableObject , ISelectorManager
{
    [SerializeField] private InputHandler   _inputHandler;
    [SerializeField] private List<BaseSelector> _selectorList;

    private LinkedList<ISelectorExecutor> _executors = new LinkedList<ISelectorExecutor>();
    private LinkedListNode<ISelectorExecutor> _current;

    public void Init()
    {
        _executors.Clear();
        foreach (var selector in _selectorList)
        {
            selector.Init();
        }
    }

    public void UpdateActionSelector(BaseUnit unit)
    {
        foreach(var selector in _selectorList)
        {
            if(selector is ActionSelector actionSelector)
            {
                actionSelector.UpdateSelector(unit);
                break;
            }
        }
    }

    public void AddSelectorExecuter(ISelectorExecutor executer)
    {
        if (executer == null) return;
        _executors.AddLast(executer);
    }

    public async UniTask ExecuteAll()
    {
        _current = _executors.First;
        while (_current != null)
        {
            var node = _current;

            var selector = FindSelectorFor(node.Value);
            if (selector == null)
            {
                Debug.LogAssertion($"There is no Selector For : {node.Value}");
                continue;
            }

            bool goNext = await node.Value.ExecuteWith(selector);

            if (goNext)
                _current = _current.Next;
            else
            {
                if (_current.Previous != null)
                    _current = _current.Previous;
                else
                    await UniTask.Yield();
            }
        }
        _executors.Clear();
    }

    private BaseSelector FindSelectorFor(ISelectorExecutor executor)
    {
        foreach (var selector in _selectorList)
        {
            if (executor.SelectorType.IsInstanceOfType(selector))
                return selector;
        }

        return null;
    }

    // Bind to InputHandler
    private async UniTask SettingMenu()
    {
        
    }
}
