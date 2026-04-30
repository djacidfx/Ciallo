using Frent;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Event raised when a tree mutation happens. Imitate web browser Dom tree's mutation event.
/// </summary>
/// <remarks>
/// Not implemented reparent in EntityNodeTree, not sure if needed but leave space for it.
/// </remarks>
public readonly record struct TreeMutationEvent(
    TreeMutationKind Kind,
    Entity Target,
    Entity OldParent,
    int OldIndex,
    Entity NewParent,
    int NewIndex
);

public enum TreeMutationKind
{
    Add,
    Remove,
    Move,
}

/// <summary>Fired on a child entity when it is inserted into a parent.</summary>
public readonly record struct ChildInsertedEvent(int Index, Entity Parent);

/// <summary>Fired on a child entity when it is removed from its parent.</summary>
public readonly record struct ChildRemovedEvent(int Index, Entity Parent);

/// <summary>Fired on a child entity when it is moved within (or across) parents.</summary>
public readonly record struct ChildMovedEvent(int OldIndex, int NewIndex, Entity Parent);

public record MoveOrReparentAsExitEnter
{
    public MoveOrReparentAsExitEnter(
        Observable<ChildInsertedEvent> treeEntered,
        Observable<ChildRemovedEvent> treeExited,
        Observable<ChildMovedEvent> moved)
    {
        // Order matters, moved trigger exit first
        Removed = treeExited.Merge(moved.Select(et => new ChildRemovedEvent(et.OldIndex, et.Parent)));
        Added = treeEntered.Merge(moved.Select(et => new ChildInsertedEvent(et.NewIndex, et.Parent)));
    }

    public readonly Observable<ChildInsertedEvent> Added;
    public readonly Observable<ChildRemovedEvent> Removed;
}