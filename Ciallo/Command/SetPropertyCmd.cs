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
    private readonly ReactiveProperty<T> _property;
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

    public SetPropertyCmd(ReactiveProperty<T> property, T newValue)
    {
        _property = property;
        _newValue = newValue;
        _inputNewValue = true;
    }

    public SetPropertyCmd(ReactiveProperty<T> property, T oldValue, T newValue) : this(property, newValue)
    {
        _oldValue = oldValue;
        _inputOldValue = true;
    }

    public SetPropertyCmd(T oldValue, ReactiveProperty<T> property)
    {
        _oldValue = oldValue;
        _inputOldValue = true;
        _property = property;
    }
    
    private ReactiveProperty<T> Resolve(Entity targetE) => _property ?? _getProperty(targetE);

    public override void BeforeFirstDo(Entity targetE)
    {
        if (!_inputOldValue) _oldValue = Resolve(targetE).Value;
        if (!_inputNewValue) _newValue = Resolve(targetE).Value;
    }

    public override void Do(Entity targetE)
    {
        Resolve(targetE).Value = _newValue;
    }

    public override void Undo(Entity targetE)
    {
        Resolve(targetE).Value = _oldValue;
    }
}