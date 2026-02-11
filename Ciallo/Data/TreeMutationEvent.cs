using Frent;

namespace Ciallo.Data;

/// <summary>
/// Event raised when a tree mutation happens. Imitate web browser Dom tree's mutation event.
/// </summary>
public readonly record struct TreeMutationEvent(
    TreeMutationKind Kind,
    Entity Node,
    Entity OldParent,
    int OldIndex,
    Entity NewParent,
    int NewIndex
);

public enum TreeMutationKind
{
    Insert,
    Remove,
    Move,
    Clear,
}

public readonly record struct TreeNodeAddChildEvent(int Index, Entity Child);

public readonly record struct TreeNodeRemoveChildEvent(int Index, Entity Child);

public readonly record struct TreeNodeMoveChildEvent(int OldIndex, int NewIndex, Entity Child);