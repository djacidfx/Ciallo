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
        // Use dedicated subjects so that Removed always fires before Added
        // regardless of subscriber registration order.
        var movedRemoved = new Subject<ChildRemovedEvent>();
        var movedAdded = new Subject<ChildInsertedEvent>();
        moved.Subscribe(et =>
        {
            movedRemoved.OnNext(new ChildRemovedEvent(et.OldIndex, et.Parent));
            movedAdded.OnNext(new ChildInsertedEvent(et.NewIndex, et.Parent));
        });
        Removed = treeExited.Merge(movedRemoved);
        Added = treeEntered.Merge(movedAdded);
    }

    public readonly Observable<ChildInsertedEvent> Added;
    public readonly Observable<ChildRemovedEvent> Removed;
}