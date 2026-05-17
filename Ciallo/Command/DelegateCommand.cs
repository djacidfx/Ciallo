using System;
using Ciallo.Command;

// ReSharper disable once CheckNamespace
namespace Ciallo;

public sealed class DelegateCommand(Action doAction, Action undoAction) : ICommand
{
    public void Do() => doAction();
    public void Undo() => undoAction();
}
