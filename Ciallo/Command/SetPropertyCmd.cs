using System;
using Frent;
using Microsoft.CodeAnalysis;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class SetPropertyCmd<T> : CommandBase
{
    private Optional<T> _oldValue;
    private Optional<T> _newValue;
    private readonly Func<Entity, ReactiveProperty<T>> _getProperty;


    public SetPropertyCmd(Func<Entity, ReactiveProperty<T>> getProperty, T newValue)
    {
        _getProperty = getProperty;
        _newValue = newValue;
    }

    public SetPropertyCmd(Func<Entity, ReactiveProperty<T>> getProperty, T oldValue, T newValue) : this(getProperty, newValue)
    {
        _oldValue = oldValue;
    }

    public SetPropertyCmd(T oldValue, Func<Entity, ReactiveProperty<T>> getProperty)
    {
        _oldValue = oldValue;
        _getProperty = getProperty;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        if (!_oldValue.HasValue) _oldValue = _getProperty(targetE).Value;
        if (!_newValue.HasValue) _newValue = _getProperty(targetE).Value;
    }

    public override void Do(Entity targetE)
    {
        _getProperty(targetE).Value = _newValue.Value;
    }

    public override void Undo(Entity targetE)
    {
        _getProperty(targetE).Value = _oldValue.Value;
    }
}