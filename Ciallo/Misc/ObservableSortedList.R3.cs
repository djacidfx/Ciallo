#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using R3;

namespace ObservableCollections;

public static class ObservableSortedListR3Extensions
{
    public static Observable<DictionaryAddEvent<TKey, TValue>> ObserveDictionaryAdd<TKey, TValue>(
        this ObservableSortedList<TKey, TValue> source,
        CancellationToken cancellationToken = default)
        where TKey : struct
        => new SortedListAdd<TKey, TValue>(source, cancellationToken);

    public static Observable<DictionaryRemoveEvent<TKey, TValue>> ObserveDictionaryRemove<TKey, TValue>(
        this ObservableSortedList<TKey, TValue> source,
        CancellationToken cancellationToken = default)
        where TKey : struct
        => new SortedListRemove<TKey, TValue>(source, cancellationToken);

    public static Observable<DictionaryReplaceEvent<TKey, TValue>> ObserveDictionaryReplace<TKey, TValue>(
        this ObservableSortedList<TKey, TValue> source,
        CancellationToken cancellationToken = default)
        where TKey : struct
        => new SortedListReplace<TKey, TValue>(source, cancellationToken);

    public static Observable<CollectionChangedEvent<KeyValuePair<TKey, TValue>>> ObserveChanged<TKey, TValue>(
        this ObservableSortedList<TKey, TValue> source,
        CancellationToken cancellationToken = default)
        where TKey : struct
        => new SortedListChanged<TKey, TValue>(source, cancellationToken);

    public static Observable<int> ObserveCountChanged<TKey, TValue>(
        this ObservableSortedList<TKey, TValue> source,
        bool notifyCurrentCount = false,
        CancellationToken cancellationToken = default)
        where TKey : struct
        => new SortedListCountChanged<TKey, TValue>(source, notifyCurrentCount, cancellationToken);
}

// ── shared base ──────────────────────────────────────────────────────────────

abstract class SortedListObserverBase<TKey, TValue, TEvent> : IDisposable
    where TKey : struct
{
    readonly ObservableSortedList<TKey, TValue> _source;
    readonly NotifyCollectionChangedEventHandler<KeyValuePair<TKey, TValue>> _delegate;
    readonly CancellationTokenRegistration _ctr;

    protected readonly Observer<TEvent> Observer;

    protected SortedListObserverBase(
        ObservableSortedList<TKey, TValue> source,
        Observer<TEvent> observer,
        CancellationToken cancellationToken)
    {
        _source = source;
        Observer = observer;
        _delegate = Handle;
        source.CollectionChanged += _delegate;

        if (cancellationToken.CanBeCanceled)
            _ctr = cancellationToken.UnsafeRegister(
                static s => { var self = (SortedListObserverBase<TKey, TValue, TEvent>)s!; self.Observer.OnCompleted(); self.Dispose(); },
                this);
    }

    protected abstract void Handle(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e);

    public void Dispose()
    {
        _source.CollectionChanged -= _delegate;
        _ctr.Dispose();
    }
}

// ── Add ───────────────────────────────────────────────────────────────────────

sealed class SortedListAdd<TKey, TValue>(ObservableSortedList<TKey, TValue> source, CancellationToken ct)
    : Observable<DictionaryAddEvent<TKey, TValue>>
    where TKey : struct
{
    protected override IDisposable SubscribeCore(Observer<DictionaryAddEvent<TKey, TValue>> observer)
        => new Sub(source, observer, ct);

    sealed class Sub(ObservableSortedList<TKey, TValue> source, Observer<DictionaryAddEvent<TKey, TValue>> observer, CancellationToken ct)
        : SortedListObserverBase<TKey, TValue, DictionaryAddEvent<TKey, TValue>>(source, observer, ct)
    {
        protected override void Handle(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                Observer.OnNext(new DictionaryAddEvent<TKey, TValue>(e.NewItem.Key, e.NewItem.Value));
        }
    }
}

// ── Remove ────────────────────────────────────────────────────────────────────

sealed class SortedListRemove<TKey, TValue>(ObservableSortedList<TKey, TValue> source, CancellationToken ct)
    : Observable<DictionaryRemoveEvent<TKey, TValue>>
    where TKey : struct
{
    protected override IDisposable SubscribeCore(Observer<DictionaryRemoveEvent<TKey, TValue>> observer)
        => new Sub(source, observer, ct);

    sealed class Sub(ObservableSortedList<TKey, TValue> source, Observer<DictionaryRemoveEvent<TKey, TValue>> observer, CancellationToken ct)
        : SortedListObserverBase<TKey, TValue, DictionaryRemoveEvent<TKey, TValue>>(source, observer, ct)
    {
        protected override void Handle(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
                Observer.OnNext(new DictionaryRemoveEvent<TKey, TValue>(e.OldItem.Key, e.OldItem.Value));
        }
    }
}

// ── Replace ───────────────────────────────────────────────────────────────────

sealed class SortedListReplace<TKey, TValue>(ObservableSortedList<TKey, TValue> source, CancellationToken ct)
    : Observable<DictionaryReplaceEvent<TKey, TValue>>
    where TKey : struct
{
    protected override IDisposable SubscribeCore(Observer<DictionaryReplaceEvent<TKey, TValue>> observer)
        => new Sub(source, observer, ct);

    sealed class Sub(ObservableSortedList<TKey, TValue> source, Observer<DictionaryReplaceEvent<TKey, TValue>> observer, CancellationToken ct)
        : SortedListObserverBase<TKey, TValue, DictionaryReplaceEvent<TKey, TValue>>(source, observer, ct)
    {
        protected override void Handle(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e)
        {
            if (e.Action == NotifyCollectionChangedAction.Replace)
                Observer.OnNext(new DictionaryReplaceEvent<TKey, TValue>(e.NewItem.Key, e.OldItem.Value, e.NewItem.Value));
        }
    }
}

// ── Changed (all actions) ─────────────────────────────────────────────────────

sealed class SortedListChanged<TKey, TValue>(ObservableSortedList<TKey, TValue> source, CancellationToken ct)
    : Observable<CollectionChangedEvent<KeyValuePair<TKey, TValue>>>
    where TKey : struct
{
    protected override IDisposable SubscribeCore(Observer<CollectionChangedEvent<KeyValuePair<TKey, TValue>>> observer)
        => new Sub(source, observer, ct);

    sealed class Sub(ObservableSortedList<TKey, TValue> source, Observer<CollectionChangedEvent<KeyValuePair<TKey, TValue>>> observer, CancellationToken ct)
        : SortedListObserverBase<TKey, TValue, CollectionChangedEvent<KeyValuePair<TKey, TValue>>>(source, observer, ct)
    {
        protected override void Handle(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e)
        {
            Observer.OnNext(new CollectionChangedEvent<KeyValuePair<TKey, TValue>>(
                e.Action, e.NewItem, e.OldItem,
                e.NewStartingIndex, e.OldStartingIndex,
                e.SortOperation));
        }
    }
}

// ── CountChanged ──────────────────────────────────────────────────────────────

sealed class SortedListCountChanged<TKey, TValue>(ObservableSortedList<TKey, TValue> source, bool notifyCurrentCount, CancellationToken ct)
    : Observable<int>
    where TKey : struct
{
    protected override IDisposable SubscribeCore(Observer<int> observer)
        => new Sub(source, notifyCurrentCount, observer, ct);

    sealed class Sub : SortedListObserverBase<TKey, TValue, int>
    {
        readonly ObservableSortedList<TKey, TValue> _source;
        int _prevCount;

        public Sub(ObservableSortedList<TKey, TValue> source, bool notifyCurrentCount, Observer<int> observer, CancellationToken ct)
            : base(source, observer, ct)
        {
            _source = source;
            _prevCount = source.Count;
            if (notifyCurrentCount)
                observer.OnNext(_prevCount);
        }

        protected override void Handle(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                    Observer.OnNext(_source.Count);
                    break;
                case NotifyCollectionChangedAction.Reset when _prevCount != _source.Count:
                    Observer.OnNext(_source.Count);
                    break;
            }
            _prevCount = _source.Count;
        }
    }
}
