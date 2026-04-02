using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Frent;
using ObservableCollections;
using R3;

namespace Ciallo.Command;

public class SetObservableCollection<TValue> : CommandBase
{
    public Func<Entity, IObservableCollection<TValue>> GetCollection { get; }
    public Action<IObservableCollection<TValue>> Action { get; }
    public readonly List<CollectionChangedEvent<TValue>> CollectionHistory = [];

    public IObservableCollection<TValue> Collection { get; private set; }

    public SetObservableCollection(
        Func<Entity, IObservableCollection<TValue>> getCollection,
        Action<IObservableCollection<TValue>> action)
    {
        GetCollection = getCollection;
        Action = action;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        Collection = GetCollection(targetE);
    }

    public override void Do(Entity targetE)
    {
        using var _ = Collection.ObserveChanged().Subscribe(CollectionHistory.Add);
        Action(Collection);
    }

    public override void Undo(Entity targetE)
    {
        // Undo according to history
        var reversedHistory = CollectionHistory.AsEnumerable().Reverse();
        switch (Collection)
        {
            case ObservableList<TValue> list:
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
                break;
            case ObservableHashSet<TValue> set:
                foreach (var et in reversedHistory)
                {
                    switch (et.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            set.Remove(et.NewItem);
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            set.Add(et.OldItem);
                            break;
                        default:
                            throw new NotSupportedException($"Undo is not supported for action {et.Action}");
                    }
                }
                break;
            default:
                throw new NotSupportedException($"Collection type {Collection.GetType()} is not supported yet");
        }

        // May need reflection (or ICollection<TPair>) to deal with ObservableDictionary, which is not supported yet.

        // Clear history
        CollectionHistory.Clear();
    }
}

public partial class CommandBuilder
{
    public CommandBuilder SetObservableCollection<T>(
        Func<Entity, ObservableList<T>> getCollection,
        Action<ObservableList<T>> action)
    {
        var cmd = new SetObservableCollection<T>(getCollection, AdaptedAction) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;

        void AdaptedAction(IObservableCollection<T> collection) => action((ObservableList<T>)collection);
    }

    public CommandBuilder SetObservableCollection<T>(
        Func<Entity, ObservableHashSet<T>> getCollection,
        Action<ObservableHashSet<T>> action)
    {
        var cmd = new SetObservableCollection<T>(getCollection, AdaptedAction) { TargetE = TargetE };
        Commands.Add(cmd);
        return this;

        void AdaptedAction(IObservableCollection<T> collection) => action((ObservableHashSet<T>)collection);
    }
}