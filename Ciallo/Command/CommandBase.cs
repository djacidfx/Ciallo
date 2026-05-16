using System;
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

    public void Commit(bool execute = true, MergeMode mergeMode = MergeMode.Disable)
    {
        var cm = Document.Get<CommandManager>();

        // Add Do/Undo Reference methods, order matters:
        var obj = new CommandWrapperObject(this);


        UndoRedo.MergeMode mode = mergeMode switch
        {
            MergeMode.Disable => UndoRedo.MergeMode.Disable,
            MergeMode.ForceMergeLatest => UndoRedo.MergeMode.All,
            MergeMode.MergeEnd => UndoRedo.MergeMode.Ends,
            _ => throw new ArgumentOutOfRangeException(nameof(mergeMode), mergeMode, null)
        };

        string actionName = ClassName;
        if (mergeMode == MergeMode.ForceMergeLatest)
        {
            string name = cm.GetCurrentActionName();
            if (!string.IsNullOrEmpty(name))
                actionName = name;
        }

        cm.CreateAction(actionName, mode);
        cm.AddDo(obj);
        cm.AddUndo(obj);
        cm.CommitAction(execute);
    }

    public override string ToString()
    {
        return $"{ClassName}";
    }
}

public enum MergeMode
{
    Disable,
    ForceMergeLatest, // Always merge with the latest command, despite command name.
    // For squential commands, merge with the latest command if it has the same name.
    // When undo/redo only excute first command's Undo and latest command's Do.
    MergeEnd,
}