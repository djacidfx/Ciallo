#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace ObservableCollections;

public partial class ObservableSortedDictionary<TKey, TValue>
{
    public ISynchronizedView<KeyValuePair<TKey, TValue>, TView> CreateView<TView>(
        Func<KeyValuePair<TKey, TValue>, TView> transform)
    {
        return new View<TView>(this, transform);
    }

    // ISynchronizedView backed by a SortedList so that IndexOfKey() provides the true
    // sorted position for every Add/Remove/Replace event.
    sealed class View<TView> : ISynchronizedView<KeyValuePair<TKey, TValue>, TView>
    {
        readonly ObservableSortedDictionary<TKey, TValue> _source;
        readonly Func<KeyValuePair<TKey, TValue>, TView> _selector;
        ISynchronizedViewFilter<KeyValuePair<TKey, TValue>, TView> _filter;

        // SortedList gives O(log n) IndexOfKey() which we need to produce correct indexes.
        readonly SortedList<TKey, (TValue Value, TView View)> _sortedList;
        int _filteredCount;

        public View(ObservableSortedDictionary<TKey, TValue> source,
                    Func<KeyValuePair<TKey, TValue>, TView> selector)
        {
            _source = source;
            _selector = selector;
            _filter = SynchronizedViewFilter<KeyValuePair<TKey, TValue>, TView>.Null;
            SyncRoot = new object();

            lock (source.SyncRoot)
            {
                _sortedList = new SortedList<TKey, (TValue, TView)>(source._dictionary.Comparer);
                foreach (var kvp in source._dictionary)
                    _sortedList.Add(kvp.Key, (kvp.Value, selector(kvp)));
                _filteredCount = _sortedList.Count;
                source.CollectionChanged += SourceCollectionChanged;
            }
        }

        public object SyncRoot { get; }

        public event NotifyViewChangedEventHandler<KeyValuePair<TKey, TValue>, TView>? ViewChanged;
        public event Action<RejectedViewChangedAction, int, int>? RejectedViewChanged;
        public event Action<NotifyCollectionChangedAction>? CollectionStateChanged;

        public ISynchronizedViewFilter<KeyValuePair<TKey, TValue>, TView> Filter
        {
            get { lock (SyncRoot) return _filter; }
        }

        public int Count
        {
            get { lock (SyncRoot) return _filteredCount; }
        }

        public int UnfilteredCount
        {
            get { lock (SyncRoot) return _sortedList.Count; }
        }

        public void Dispose()
        {
            _source.CollectionChanged -= SourceCollectionChanged;
        }

        public void AttachFilter(ISynchronizedViewFilter<KeyValuePair<TKey, TValue>, TView> newFilter)
        {
            if (newFilter.IsNullFilter())
            {
                ResetFilter();
                return;
            }

            lock (SyncRoot)
            {
                _filter = newFilter;
                _filteredCount = 0;
                foreach (var pair in _sortedList)
                {
                    var kv = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
                    if (_filter.IsMatch(kv, pair.Value.View))
                        _filteredCount++;
                }

                ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                    NotifyCollectionChangedAction.Reset, true));
            }
        }

        public void ResetFilter()
        {
            lock (SyncRoot)
            {
                _filter = SynchronizedViewFilter<KeyValuePair<TKey, TValue>, TView>.Null;
                _filteredCount = _sortedList.Count;
                ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                    NotifyCollectionChangedAction.Reset, true));
            }
        }

        public ISynchronizedViewList<TView> ToViewList() => new ViewList<TView>(this);

        public NotifyCollectionChangedSynchronizedViewList<TView> ToNotifyCollectionChanged()
            => new ViewList<TView>(this);

        public NotifyCollectionChangedSynchronizedViewList<TView> ToNotifyCollectionChanged(
            ICollectionEventDispatcher? collectionEventDispatcher)
            => new ViewList<TView>(this);

        public IEnumerator<TView> GetEnumerator()
        {
            lock (SyncRoot)
            {
                foreach (var pair in _sortedList)
                {
                    var kv = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
                    if (_filter.IsMatch(kv, pair.Value.View))
                        yield return pair.Value.View;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<(KeyValuePair<TKey, TValue> Value, TView View)> Filtered
        {
            get
            {
                lock (SyncRoot)
                {
                    foreach (var pair in _sortedList)
                    {
                        var kv = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
                        if (_filter.IsMatch(kv, pair.Value.View))
                            yield return (kv, pair.Value.View);
                    }
                }
            }
        }

        public IEnumerable<(KeyValuePair<TKey, TValue> Value, TView View)> Unfiltered
        {
            get
            {
                lock (SyncRoot)
                {
                    foreach (var pair in _sortedList)
                        yield return (new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value), pair.Value.View);
                }
            }
        }

        void SourceCollectionChanged(in NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>> e)
        {
            lock (SyncRoot)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        var view = _selector(e.NewItem);
                        _sortedList.Add(e.NewItem.Key, (e.NewItem.Value, view));
                        var index = _sortedList.IndexOfKey(e.NewItem.Key);

                        if (_filter.IsMatch(e.NewItem, view))
                        {
                            _filteredCount++;
                            ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                                NotifyCollectionChangedAction.Add, true,
                                newItem: (e.NewItem, view),
                                newStartingIndex: index));
                        }
                        else
                        {
                            RejectedViewChanged?.Invoke(RejectedViewChangedAction.Add, index, -1);
                        }
                        break;
                    }

                    case NotifyCollectionChangedAction.Remove:
                    {
                        var index = _sortedList.IndexOfKey(e.OldItem.Key);
                        if (index < 0)
                            break;

                        var (_, view) = _sortedList.Values[index];
                        _sortedList.RemoveAt(index);

                        if (_filter.IsMatch(e.OldItem, view))
                        {
                            _filteredCount--;
                            ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                                NotifyCollectionChangedAction.Remove, true,
                                oldItem: (e.OldItem, view),
                                oldStartingIndex: index));
                        }
                        else
                        {
                            RejectedViewChanged?.Invoke(RejectedViewChangedAction.Remove, index, -1);
                        }
                        break;
                    }

                    case NotifyCollectionChangedAction.Replace:
                    {
                        var index = _sortedList.IndexOfKey(e.NewItem.Key);
                        var (_, oldView) = _sortedList.Values[index];
                        var newView = _selector(e.NewItem);
                        _sortedList[e.NewItem.Key] = (e.NewItem.Value, newView);

                        var oldMatched = _filter.IsMatch(e.OldItem, oldView);
                        var newMatched = _filter.IsMatch(e.NewItem, newView);

                        if (oldMatched && newMatched)
                        {
                            ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                                NotifyCollectionChangedAction.Replace, true,
                                newItem: (e.NewItem, newView),
                                oldItem: (e.OldItem, oldView),
                                newStartingIndex: index,
                                oldStartingIndex: index));
                        }
                        else if (oldMatched)
                        {
                            _filteredCount--;
                            ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                                NotifyCollectionChangedAction.Remove, true,
                                oldItem: (e.OldItem, oldView),
                                oldStartingIndex: index));
                        }
                        else if (newMatched)
                        {
                            _filteredCount++;
                            ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                                NotifyCollectionChangedAction.Add, true,
                                newItem: (e.NewItem, newView),
                                newStartingIndex: index));
                        }
                        break;
                    }

                    case NotifyCollectionChangedAction.Reset:
                        _sortedList.Clear();
                        _filteredCount = 0;
                        ViewChanged?.Invoke(new SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView>(
                            NotifyCollectionChangedAction.Reset, true));
                        break;
                }

                CollectionStateChanged?.Invoke(e.Action);
            }
        }
    }

    // Read-only NotifyCollectionChangedSynchronizedViewList backed by View<TView>.
    // Maintains sorted order because the View already provides correct sorted indexes.
    // Does not support filter-aware alternate-index tracking (no write-back).
    sealed class ViewList<TView> : NotifyCollectionChangedSynchronizedViewList<TView>
    {
        readonly ISynchronizedView<KeyValuePair<TKey, TValue>, TView> _parent;
        readonly List<TView> _listView;

        public override event NotifyCollectionChangedEventHandler? CollectionChanged;
        public override event PropertyChangedEventHandler? PropertyChanged;

        public ViewList(ISynchronizedView<KeyValuePair<TKey, TValue>, TView> parent)
        {
            _parent = parent;
            lock (parent.SyncRoot)
            {
                _listView = parent.Unfiltered.Select(static x => x.View).ToList();
                parent.ViewChanged += Parent_ViewChanged;
            }
        }

        void Parent_ViewChanged(in SynchronizedViewChangedEventArgs<KeyValuePair<TKey, TValue>, TView> e)
        {
            // Called inside parent (View) lock; acquire our own gate next.
            lock (gate)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        var index = e.NewStartingIndex == -1 ? _listView.Count : e.NewStartingIndex;
                        _listView.Insert(index, e.NewItem.View);
                        CollectionChanged?.Invoke(this,
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItem.View, index));
                        PropertyChanged?.Invoke(this, ViewListStatics.CountArgs);
                        break;
                    }

                    case NotifyCollectionChangedAction.Remove:
                    {
                        int index;
                        if (e.OldStartingIndex == -1)
                        {
                            index = _listView.IndexOf(e.OldItem.View);
                            if (index == -1) break;
                        }
                        else
                        {
                            index = e.OldStartingIndex;
                        }
                        _listView.RemoveAt(index);
                        CollectionChanged?.Invoke(this,
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItem.View, index));
                        PropertyChanged?.Invoke(this, ViewListStatics.CountArgs);
                        break;
                    }

                    case NotifyCollectionChangedAction.Replace:
                    {
                        int index;
                        if (e.NewStartingIndex == -1)
                        {
                            index = _listView.IndexOf(e.OldItem.View);
                            if (index == -1) break;
                        }
                        else
                        {
                            index = e.NewStartingIndex;
                        }
                        _listView[index] = e.NewItem.View;
                        CollectionChanged?.Invoke(this,
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                e.NewItem.View, e.OldItem.View, index));
                        break;
                    }

                    case NotifyCollectionChangedAction.Reset:
                        _listView.Clear();
                        foreach (var item in _parent.Unfiltered)
                            _listView.Add(item.View);
                        CollectionChanged?.Invoke(this,
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        PropertyChanged?.Invoke(this, ViewListStatics.CountArgs);
                        break;
                }
            }
        }

        public override TView this[int index]
        {
            get { lock (gate) return _listView[index]; }
            set => throw new NotSupportedException("ObservableSortedDictionary view lists are read-only.");
        }

        public override int Count
        {
            get { lock (gate) return _listView.Count; }
        }

        public override IEnumerator<TView> GetEnumerator()
        {
            lock (gate)
            {
                foreach (var item in _listView)
                    yield return item;
            }
        }

        public override bool Contains(TView item)
        {
            lock (gate) return _listView.Contains(item);
        }

        public override int IndexOf(TView item)
        {
            lock (gate) return _listView.IndexOf(item);
        }

        public override void Dispose()
        {
            _parent.ViewChanged -= Parent_ViewChanged;
        }

        public override void Add(TView item) =>
            throw new NotSupportedException("ObservableSortedDictionary view lists are read-only.");

        public override void Insert(int index, TView item) =>
            throw new NotSupportedException("ObservableSortedDictionary view lists are read-only.");

        public override bool Remove(TView item) =>
            throw new NotSupportedException("ObservableSortedDictionary view lists are read-only.");

        public override void RemoveAt(int index) =>
            throw new NotSupportedException("ObservableSortedDictionary view lists are read-only.");

        public override void Clear() =>
            throw new NotSupportedException("ObservableSortedDictionary view lists are read-only.");
    }
}

// Non-generic statics to avoid "static field in generic type" allocations.
file static class ViewListStatics
{
    public static readonly PropertyChangedEventArgs CountArgs = new("Count");
}






