using System;
using Frent;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class SetPropertyCmd<T> : CommandBase
{
    private T _oldValue;
    private readonly bool _inputOldValue;
    private T _newValue;
    private readonly bool _inputNewValue;
    private readonly Func<Entity, ReactiveProperty<T>> _getProperty;


    public SetPropertyCmd(Func<Entity, ReactiveProperty<T>> getProperty, T newValue)
    {
        _getProperty = getProperty;
        _newValue = newValue;
        _inputNewValue = true;
    }

    public SetPropertyCmd(Func<Entity, ReactiveProperty<T>> getProperty, T oldValue, T newValue) : this(getProperty, newValue)
    {
        _oldValue = oldValue;
        _inputOldValue = true;
    }

    public SetPropertyCmd(T oldValue, Func<Entity, ReactiveProperty<T>> getProperty)
    {
        _oldValue = oldValue;
        _inputOldValue = true;
        _getProperty = getProperty;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        if (!_inputOldValue) _oldValue = _getProperty(targetE).Value;
        if (!_inputNewValue) _newValue = _getProperty(targetE).Value;
    }

    public override void Do(Entity targetE)
    {
        _getProperty(targetE).Value = _newValue;
    }

    public override void Undo(Entity targetE)
    {
        _getProperty(targetE).Value = _oldValue;
    }
}