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
    public Func<Entity, TCollection> GetCollection { get; }
    public Action<TCollection> Action { get; }

    public TCollection Collection;

    protected SetObservableCollectionBase(
        Func<Entity, TCollection> getCollection,
        Action<TCollection> action)
    {
        GetCollection = getCollection;
        Action = action;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        Collection = GetCollection(targetE);
    }
}

public class SetObservableList<T> : SetObservableCollectionBase<ObservableList<T>>
{
    public readonly List<CollectionChangedEvent<T>> CollectionHistory = [];

    public SetObservableList(
        Func<Entity, ObservableList<T>> getCollection,
        Action<ObservableList<T>> action
    ) : base(getCollection, action) { }

    public override void Do(Entity targetE)
    {
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        Action(Collection);
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
        Action<ObservableHashSet<T>> action
    ) : base(getCollection, action) { }

    public override void Do(Entity targetE)
    {
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        Action(Collection);
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
        Action<ObservableDictionary<TKey, TValue>> action
    ) : base(getCollection, action) { }

    public override void Do(Entity targetE)
    {
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        Action(Collection);
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
        Action<ObservableSortedDictionary<TKey, TValue>> action
    ) : base(getCollection, action) { }

    public override void Do(Entity targetE)
    {
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        Action(Collection);
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
        Action<ObservableSortedList<TKey, TValue>> action
    ) : base(getCollection, action) { }

    public override void Do(Entity targetE)
    {
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        Action(Collection);
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
    public CommandBuilder SetObservableCollection<T>(
        Func<Entity, ObservableList<T>> getCollection,
        Action<ObservableList<T>> action)
    {
        var cmd = new SetObservableList<T>(getCollection, action) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    public CommandBuilder SetObservableCollection<T>(
        Func<Entity, ObservableHashSet<T>> getCollection,
        Action<ObservableHashSet<T>> action)
    {
        var cmd = new SetObservableHashSet<T>(getCollection, action) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    public CommandBuilder SetObservableCollection<TKey, TValue>(
        Func<Entity, ObservableDictionary<TKey, TValue>> getCollection,
        Action<ObservableDictionary<TKey, TValue>> action)
    {
        var cmd = new SetObservableDictionary<TKey, TValue>(getCollection, action) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    public CommandBuilder SetObservableCollection<TKey, TValue>(
        Func<Entity, ObservableSortedDictionary<TKey, TValue>> getCollection,
        Action<ObservableSortedDictionary<TKey, TValue>> action)
        where TKey : notnull
    {
        var cmd = new SetObservableSortedDictionary<TKey, TValue>(getCollection, action) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }

    public CommandBuilder SetObservableCollection<TKey, TValue>(
        Func<Entity, ObservableSortedList<TKey, TValue>> getCollection,
        Action<ObservableSortedList<TKey, TValue>> action)
        where TKey : struct
    {
        var cmd = new SetObservableSortedList<TKey, TValue>(getCollection, action) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;
    }
}