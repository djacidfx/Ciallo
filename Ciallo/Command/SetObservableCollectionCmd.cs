using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Command;

public abstract class SetObservableCollectionBase<TCollection> : CommandBase
{
    private readonly Func<Entity, TCollection> _getCollection;
    private Action<TCollection> _captureChanges;

    public TCollection Collection;

    protected SetObservableCollectionBase(
        Func<Entity, TCollection> getCollection,
        Action<TCollection> captureChanges)
    {
        _getCollection = getCollection;
        _captureChanges = captureChanges;
    }

    protected SetObservableCollectionBase(
        TCollection collection,
        Action<TCollection> captureChanges)
    {
        Collection = collection;
        _captureChanges = captureChanges;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        if (_getCollection != null) Collection = _getCollection(targetE);
        CaptureCollectionHistory(_captureChanges);
        _captureChanges = null;
    }

    protected abstract void CaptureCollectionHistory(Action<TCollection> captureChanges);
}

public class SetObservableList<T> : SetObservableCollectionBase<ObservableList<T>>
{
    public readonly List<CollectionChangedEvent<T>> CollectionHistory = [];

    public SetObservableList(
        Func<Entity, ObservableList<T>> getCollection,
        Action<ObservableList<T>> captureChanges
    ) : base(getCollection, captureChanges) { }

    public SetObservableList(
        ObservableList<T> collection,
        Action<ObservableList<T>> captureChanges
    ) : base(collection, captureChanges) { }

    protected override void CaptureCollectionHistory(Action<ObservableList<T>> captureChanges)
    {
        CollectionHistory.Clear();
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        captureChanges(Collection);
    }

    public override void Do(Entity targetE)
    {
        if (!IsExecuted) return;

        foreach (var et in CollectionHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Insert(et.NewStartingIndex, et.NewItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.RemoveAt(et.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewStartingIndex] = et.NewItem;
                    break;
                case NotifyCollectionChangedAction.Move:
                    Collection.Move(et.OldStartingIndex, et.NewStartingIndex);
                    break;
                default:
                    throw new NotSupportedException($"Do is not supported for action {et.Action}");
            }
        }
    }

    public override void Undo(Entity targetE)
    {
        var reversedHistory = CollectionHistory.AsEnumerable().Reverse();
        var list = Collection;
        foreach (var et in reversedHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    list.RemoveAt(et.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    list.Insert(et.OldStartingIndex, et.OldItem);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    list[et.NewStartingIndex] = et.OldItem;
                    break;
                case NotifyCollectionChangedAction.Move:
                    list.Move(et.NewStartingIndex, et.OldStartingIndex);
                    break;
                default:
                    throw new NotSupportedException($"Undo is not supported for action {et.Action}");
            }
        }
    }
}

public class SetObservableHashSet<T> : SetObservableCollectionBase<ObservableHashSet<T>>
{
    public readonly List<CollectionChangedEvent<T>> CollectionHistory = [];

    public SetObservableHashSet(
        Func<Entity, ObservableHashSet<T>> getCollection,
        Action<ObservableHashSet<T>> captureChanges
    ) : base(getCollection, captureChanges) { }

    public SetObservableHashSet(
        ObservableHashSet<T> collection,
        Action<ObservableHashSet<T>> captureChanges
    ) : base(collection, captureChanges) { }

    protected override void CaptureCollectionHistory(Action<ObservableHashSet<T>> captureChanges)
    {
        CollectionHistory.Clear();
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        captureChanges(Collection);
    }

    public override void Do(Entity targetE)
    {
        if (!IsExecuted) return;

        foreach (var et in CollectionHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Add(et.NewItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Remove(et.OldItem);
                    break;
                default:
                    throw new NotSupportedException($"Do is not supported for action {et.Action}");
            }
        }
    }

    public override void Undo(Entity targetE)
    {
        var reversedHistory = CollectionHistory.AsEnumerable().Reverse();
        foreach (var et in reversedHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Remove(et.NewItem);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Add(et.OldItem);
                    break;
                default:
                    throw new NotSupportedException($"Undo is not supported for action {et.Action}");
            }
        }
    }
}

public class SetObservableDictionary<TKey, TValue> : SetObservableCollectionBase<ObservableDictionary<TKey, TValue>>
{
    public readonly List<CollectionChangedEvent<KeyValuePair<TKey, TValue>>> CollectionHistory = [];

    public SetObservableDictionary(
        Func<Entity, ObservableDictionary<TKey, TValue>> getCollection,
        Action<ObservableDictionary<TKey, TValue>> captureChanges
    ) : base(getCollection, captureChanges) { }

    public SetObservableDictionary(
        ObservableDictionary<TKey, TValue> collection,
        Action<ObservableDictionary<TKey, TValue>> captureChanges
    ) : base(collection, captureChanges) { }

    protected override void CaptureCollectionHistory(Action<ObservableDictionary<TKey, TValue>> captureChanges)
    {
        CollectionHistory.Clear();
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        captureChanges(Collection);
    }

    public override void Do(Entity targetE)
    {
        if (!IsExecuted) return;

        foreach (var et in CollectionHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Add(et.NewItem.Key, et.NewItem.Value);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Remove(et.OldItem.Key);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewItem.Key] = et.NewItem.Value;
                    break;
                default:
                    throw new NotSupportedException($"Do is not supported for action {et.Action}");
            }
        }
    }

    public override void Undo(Entity targetE)
    {
        var reversedHistory = CollectionHistory.AsEnumerable().Reverse();
        foreach (var et in reversedHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Remove(et.NewItem.Key);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Add(et.OldItem.Key, et.OldItem.Value);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewItem.Key] = et.OldItem.Value;
                    break;
                default:
                    throw new NotSupportedException($"Undo is not supported for action {et.Action}");
            }
        }
    }
}

public class SetObservableSortedDictionary<TKey, TValue> : SetObservableCollectionBase<ObservableSortedDictionary<TKey, TValue>>
    where TKey : notnull
{
    public readonly List<CollectionChangedEvent<KeyValuePair<TKey, TValue>>> CollectionHistory = [];

    public SetObservableSortedDictionary(
        Func<Entity, ObservableSortedDictionary<TKey, TValue>> getCollection,
        Action<ObservableSortedDictionary<TKey, TValue>> captureChanges
    ) : base(getCollection, captureChanges) { }

    public SetObservableSortedDictionary(
        ObservableSortedDictionary<TKey, TValue> collection,
        Action<ObservableSortedDictionary<TKey, TValue>> captureChanges
    ) : base(collection, captureChanges) { }

    protected override void CaptureCollectionHistory(Action<ObservableSortedDictionary<TKey, TValue>> captureChanges)
    {
        CollectionHistory.Clear();
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        captureChanges(Collection);
    }

    public override void Do(Entity targetE)
    {
        if (!IsExecuted) return;

        foreach (var et in CollectionHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Add(et.NewItem.Key, et.NewItem.Value);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Remove(et.OldItem.Key);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewItem.Key] = et.NewItem.Value;
                    break;
                default:
                    throw new NotSupportedException($"Do is not supported for action {et.Action}");
            }
        }
    }

    public override void Undo(Entity targetE)
    {
        var reversedHistory = CollectionHistory.AsEnumerable().Reverse();
        foreach (var et in reversedHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Remove(et.NewItem.Key);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Add(et.OldItem.Key, et.OldItem.Value);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewItem.Key] = et.OldItem.Value;
                    break;
                default:
                    throw new NotSupportedException($"Undo is not supported for action {et.Action}");
            }
        }
    }
}

public class SetObservableSortedList<TKey, TValue> : SetObservableCollectionBase<ObservableSortedList<TKey, TValue>>
    where TKey : struct
{
    public readonly List<CollectionChangedEvent<KeyValuePair<TKey, TValue>>> CollectionHistory = [];

    public SetObservableSortedList(
        Func<Entity, ObservableSortedList<TKey, TValue>> getCollection,
        Action<ObservableSortedList<TKey, TValue>> captureChanges
    ) : base(getCollection, captureChanges) { }

    public SetObservableSortedList(
        ObservableSortedList<TKey, TValue> collection,
        Action<ObservableSortedList<TKey, TValue>> captureChanges
    ) : base(collection, captureChanges) { }

    protected override void CaptureCollectionHistory(Action<ObservableSortedList<TKey, TValue>> captureChanges)
    {
        CollectionHistory.Clear();
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        captureChanges(Collection);
    }

    public override void Do(Entity targetE)
    {
        if (!IsExecuted) return;

        foreach (var et in CollectionHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Add(et.NewItem.Key, et.NewItem.Value);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Remove(et.OldItem.Key);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewItem.Key] = et.NewItem.Value;
                    break;
                default:
                    throw new NotSupportedException($"Do is not supported for action {et.Action}");
            }
        }
    }

    public override void Undo(Entity targetE)
    {
        var reversedHistory = CollectionHistory.AsEnumerable().Reverse();
        foreach (var et in reversedHistory)
        {
            switch (et.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Collection.Remove(et.NewItem.Key);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Collection.Add(et.OldItem.Key, et.OldItem.Value);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Collection[et.NewItem.Key] = et.OldItem.Value;
                    break;
                default:
                    throw new NotSupportedException($"Undo is not supported for action {et.Action}");
            }
        }
    }
}

public partial class CommandBuilder
{
    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<T>(
        Func<Entity, ObservableList<T>> getCollection,
        Action<ObservableList<T>> captureChanges)
    {
        var cmd = new SetObservableList<T>(getCollection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<T>(
        ObservableList<T> collection,
        Action<ObservableList<T>> captureChanges)
    {
        var cmd = new SetObservableList<T>(collection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<T>(
        Func<Entity, ObservableHashSet<T>> getCollection,
        Action<ObservableHashSet<T>> captureChanges)
    {
        var cmd = new SetObservableHashSet<T>(getCollection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<T>(
        ObservableHashSet<T> collection,
        Action<ObservableHashSet<T>> captureChanges)
    {
        var cmd = new SetObservableHashSet<T>(collection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<TKey, TValue>(
        Func<Entity, ObservableDictionary<TKey, TValue>> getCollection,
        Action<ObservableDictionary<TKey, TValue>> captureChanges)
    {
        var cmd = new SetObservableDictionary<TKey, TValue>(getCollection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<TKey, TValue>(
        ObservableDictionary<TKey, TValue> collection,
        Action<ObservableDictionary<TKey, TValue>> captureChanges)
    {
        var cmd = new SetObservableDictionary<TKey, TValue>(collection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<TKey, TValue>(
        Func<Entity, ObservableSortedDictionary<TKey, TValue>> getCollection,
        Action<ObservableSortedDictionary<TKey, TValue>> captureChanges)
        where TKey : notnull
    {
        var cmd = new SetObservableSortedDictionary<TKey, TValue>(getCollection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<TKey, TValue>(
        ObservableSortedDictionary<TKey, TValue> collection,
        Action<ObservableSortedDictionary<TKey, TValue>> captureChanges)
        where TKey : notnull
    {
        var cmd = new SetObservableSortedDictionary<TKey, TValue>(collection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<TKey, TValue>(
        Func<Entity, ObservableSortedList<TKey, TValue>> getCollection,
        Action<ObservableSortedList<TKey, TValue>> captureChanges)
        where TKey : struct
    {
        var cmd = new SetObservableSortedList<TKey, TValue>(getCollection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    /// <summary>
    /// Captures an observable collection mutation once; redo replays the captured collection changes.
    /// </summary>
    public CommandBuilder SetObservableCollection<TKey, TValue>(
        ObservableSortedList<TKey, TValue> collection,
        Action<ObservableSortedList<TKey, TValue>> captureChanges)
        where TKey : struct
    {
        var cmd = new SetObservableSortedList<TKey, TValue>(collection, captureChanges) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }
}
