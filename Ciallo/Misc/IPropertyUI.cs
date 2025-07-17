using Godot;

namespace Ciallo.Misc;

public interface IPropertyUI
{
    public string Name { get; }
    public Control Control { get; }
}