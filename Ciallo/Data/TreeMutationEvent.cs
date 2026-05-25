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

public readonly record struct NodeInsertedEvent(int Index, Entity Parent);
public readonly record struct NodeRemovedEvent(int Index, Entity Parent);
public readonly record struct NodeMovedEvent(int OldIndex, int NewIndex, Entity Parent);

public record MoveOrReparentAsExitEnter
{
    public MoveOrReparentAsExitEnter(
        Observable<NodeInsertedEvent> treeEntered,
        Observable<NodeRemovedEvent> treeExited,
        Observable<NodeMovedEvent> moved)
    {
        // Use dedicated subjects so that Removed always fires before Added
        // regardless of subscriber registration order.
        var movedRemoved = new Subject<NodeRemovedEvent>();
        var movedAdded = new Subject<NodeInsertedEvent>();
        moved.Subscribe(et =>
        {
            movedRemoved.OnNext(new NodeRemovedEvent(et.OldIndex, et.Parent));
            movedAdded.OnNext(new NodeInsertedEvent(et.NewIndex, et.Parent));
        });
        Removed = treeExited.Merge(movedRemoved);
        Added = treeEntered.Merge(movedAdded);
    }

    public readonly Observable<NodeInsertedEvent> Added;
    public readonly Observable<NodeRemovedEvent> Removed;
}