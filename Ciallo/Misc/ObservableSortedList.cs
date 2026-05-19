#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableCollections;

/// <summary>
/// An observable sorted key-value collection backed by <see cref="SortedList{TKey,TValue}"/>.
/// Compared with <see cref="ObservableSortedDictionary{TKey,TValue}"/> (which uses a red-black
/// tree), this type provides O(log n) floor/ceiling queries via binary search on the internal
/// key array, at the cost of O(n) insertion and removal.
/// Suited for small collections (fewer than ~200 entries) where floor/ceiling lookups dominate.
/// </summary>
public class ObservableSortedList<TKey, TValue> :
    IDictionary<TKey, TValue>,
    IReadOnlyObservableDictionary<TKey, TValue>
    where TKey : struct
{
    private readonly SortedList<TKey, TValue> _list = new();

    public event NotifyCollectionChangedEventHandler<KeyValuePair<TKey, TValue>>? CollectionChanged;

    public object SyncRoot => ((ICollection)_list).SyncRoot;

    // ── Binary search ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the 0-based index of <paramref name="key"/>, or the bitwise complement of the
    /// insertion point if not found — same semantics as <see cref="Array.BinarySearch"/>.
    /// </summary>
    private int BinarySearch(TKey key)
    {
        IList<TKey> keys = _list.Keys;
        IComparer<TKey> comparer = _list.Comparer;
        int lo = 0, hi = keys.Count - 1;
        while (lo <= hi)
        {
            int mid = lo + ((hi - lo) >> 1);
            int cmp = comparer.Compare(keys[mid], key);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return ~lo;
    }

    /// <summary>Returns the index of the largest key ≤ <paramref name="key"/>, or <c>-1</c> if none exists.</summary>
    public int FloorIndex(TKey key)
    {
        int idx = BinarySearch(key);
        if (idx >= 0) return idx;
        int ins = ~idx;
        return ins > 0 ? ins - 1 : -1;
    }

    /// <summary>Returns the index of the smallest key ≥ <paramref name="key"/>, or <c>-1</c> if none exists.</summary>
    public int CeilingIndex(TKey key)
    {
        int idx = BinarySearch(key);
        if (idx >= 0) return idx;
        int ins = ~idx;
        return ins < _list.Count ? ins : -1;
    }

    /// <summary>
    /// Returns the index of the nearest key to <paramref name="key"/>: prefers the floor (largest ≤ key),
    /// falling back to the ceiling (smallest ≥ key). Returns <c>-1</c> when the collection is empty.
    /// </summary>
    public int FindNearestIndex(TKey key)
    {
        int fi = FloorIndex(key);
        return fi >= 0 ? fi : CeilingIndex(key);
    }

    /// <summary>Returns the key at the specified index.</summary>
    public TKey GetKeyAtIndex(int index) => _list.Keys[index];

    /// <summary>Returns the value at the specified index.</summary>
    public TValue GetValueAtIndex(int index) => _list.Values[index];

    // ── IDictionary<TKey, TValue> ─────────────────────────────────────────────

    public int Count => _list.Count;

    public TValue this[TKey key]
    {
        get => _list[key];
        set
        {
            int idx = _list.IndexOfKey(key);
            if (idx >= 0)
            {
                var oldValue = _list.Values[idx];
                _list[key] = value;
                CollectionChanged?.Invoke(
                    NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Replace(
                        new KeyValuePair<TKey, TValue>(key, value),
                        new KeyValuePair<TKey, TValue>(key, oldValue),
                        idx, idx));
            }
            else
            {
                _list[key] = value;
                int newIdx = _list.IndexOfKey(key);
                CollectionChanged?.Invoke(
                    NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Add(
                        new KeyValuePair<TKey, TValue>(key, value), newIdx));
            }
        }
    }

    public ICollection<TKey> Keys => _list.Keys;
    public ICollection<TValue> Values => _list.Values;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _list.Keys;
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _list.Values;

    public void Add(TKey key, TValue value)
    {
        _list.Add(key, value);
        int idx = _list.IndexOfKey(key);
        CollectionChanged?.Invoke(
            NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Add(
                new KeyValuePair<TKey, TValue>(key, value), idx));
    }

    public bool ContainsKey(TKey key) => _list.ContainsKey(key);

    public bool Remove(TKey key)
    {
        int idx = _list.IndexOfKey(key);
        if (idx < 0) return false;
        var pair = new KeyValuePair<TKey, TValue>(key, _list.Values[idx]);
        _list.RemoveAt(idx);
        CollectionChanged?.Invoke(
            NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(pair, idx));
        return true;
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        => _list.TryGetValue(key, out value!);

    public void Clear()
    {
        _list.Clear();
        CollectionChanged?.Invoke(
            NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset());
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // ── ICollection<KeyValuePair<TKey, TValue>> ───────────────────────────────

    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        => TryGetValue(item.Key, out var v) && EqualityComparer<TValue>.Default.Equals(v, item.Value);

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        => ((ICollection<KeyValuePair<TKey, TValue>>)_list).CopyTo(array, arrayIndex);

    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
    {
        if (!TryGetValue(item.Key, out var v) || !EqualityComparer<TValue>.Default.Equals(v, item.Value))
            return false;
        return Remove(item.Key);
    }

    // ── IObservableCollection<KeyValuePair<TKey, TValue>> ────────────────────

    ISynchronizedView<KeyValuePair<TKey, TValue>, TView>
        IObservableCollection<KeyValuePair<TKey, TValue>>.CreateView<TView>(
            Func<KeyValuePair<TKey, TValue>, TView> transform)
        => throw new NotImplementedException();
}
