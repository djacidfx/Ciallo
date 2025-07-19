using Godot;

namespace Ciallo.Core;

public abstract partial class ActionBase : GodotObject
{
    public bool Undoable { get; set; }
    
    
}