using Frent;
using ObservableCollections;
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

public record MoveOrReparentAsExitEnter
{
    public MoveOrReparentAsExitEnter(
        Observable<CollectionAddEvent<Entity>> treeEntered,
        Observable<CollectionRemoveEvent<Entity>> treeExited,
        Observable<CollectionMoveEvent<Entity>> moved)
    {
        // Order matters, moved trigger exit first
        Removed = treeExited.Merge(moved.Select(et => new CollectionRemoveEvent<Entity>(et.OldIndex, et.Value)));
        Added = treeEntered.Merge(moved.Select(et => new CollectionAddEvent<Entity>(et.NewIndex, et.Value)));
    }

    public readonly Observable<CollectionAddEvent<Entity>> Added;
    public readonly Observable<CollectionRemoveEvent<Entity>> Removed;
}