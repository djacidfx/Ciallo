using Ciallo.Data;
using Frent;
using Godot;
using Godot.Collections;
using Humanizer;

namespace Ciallo.Command;

public abstract class CommandBase : ICommand
{
    public bool IsExecuted;
    public Entity TargetE { protected get; set; }
    public World WorkingWorld => TargetE.World;
    public Entity Document => WorkingWorld.Document();
    public virtual string ClassName => GetType().Name.Humanize();
    public SceneTree SceneTree => (SceneTree)Engine.GetMainLoop();

    public Array<Node> GetNodesInGroup(StringName group) => SceneTree.GetNodesInGroup(group);

    protected CommandBase(Entity targetE = default)
    {
        TargetE = targetE;
    }


    public abstract void BeforeFirstDo(Entity targetE);
    public abstract void Do(Entity targetE);
    public abstract void Undo(Entity targetE);
    public virtual void OnDeletedAsDo() { }
    public virtual void OnDeletedAsUndo() { }

    public void Do()
    {
        if (!IsExecuted) BeforeFirstDo(TargetE);
        Do(TargetE);
        IsExecuted = true;
    }

    public void Undo() => Undo(TargetE);

    public override string ToString()
    {
        return $"{ClassName}";
    }
}
