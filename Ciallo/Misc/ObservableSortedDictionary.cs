// Shen is not capable to maintain this, just pray Mr. sonnet 4.6 is all-knowing

#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ObservableCollections;

public partial class ObservableSortedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyObservableDictionary<TKey, TValue>
    where TKey : notnull
{
    readonly SortedDictionary<TKey, TValue> _dictionary;
    public object SyncRoot { get; } = new object();

    public ObservableSortedDictionary()
    {
        _dictionary = new SortedDictionary<TKey, TValue>();
    }

    public ObservableSortedDictionary(IComparer<TKey>? comparer)
    {
        _dictionary = new SortedDictionary<TKey, TValue>(comparer);
    }

    public ObservableSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        : this(collection, null) { }

    public ObservableSortedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IComparer<TKey>? comparer)
    {
        _dictionary = new SortedDictionary<TKey, TValue>(comparer);
        foreach (var item in collection)
            _dictionary.Add(item.Key, item.Value);
    }

    public event NotifyCollectionChangedEventHandler<KeyValuePair<TKey, TValue>>? CollectionChanged;

    public TValue this[TKey key]
    {
        get
        {
            lock (SyncRoot)
                return _dictionary[key];
        }
        set
        {
            lock (SyncRoot)
            {
                if (_dictionary.TryGetValue(key, out var oldValue))
                {
                    _dictionary[key] = value;
                    CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Replace(
                        new KeyValuePair<TKey, TValue>(key, value),
                        new KeyValuePair<TKey, TValue>(key, oldValue!),
                        -1, -1));
                }
                else
                {
                    Add(key, value);
                }
            }
        }
    }

    ICollection<TKey> IDictionary<TKey, TValue>.Keys
    {
        get
        {
            lock (SyncRoot) return _dictionary.Keys;
        }
    }

    ICollection<TValue> IDictionary<TKey, TValue>.Values
    {
        get
        {
            lock (SyncRoot) return _dictionary.Values;
        }
    }

    public int Count
    {
        get
        {
            lock (SyncRoot) return _dictionary.Count;
        }
    }

    public bool IsReadOnly => false;

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
    {
        get
        {
            lock (SyncRoot) return _dictionary.Keys;
        }
    }

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
    {
        get
        {
            lock (SyncRoot) return _dictionary.Values;
        }
    }

    public IComparer<TKey> Comparer
    {
        get
        {
            lock (SyncRoot) return _dictionary.Comparer;
        }
    }

    public void Add(TKey key, TValue value)
    {
        lock (SyncRoot)
        {
            _dictionary.Add(key, value);
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Add(
                new KeyValuePair<TKey, TValue>(key, value), -1));
        }
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        lock (SyncRoot)
        {
            _dictionary.Clear();
            CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Reset());
        }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
        lock (SyncRoot)
            return ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).Contains(item);
    }

    public bool ContainsKey(TKey key)
    {
        lock (SyncRoot)
            return _dictionary.ContainsKey(key);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        lock (SyncRoot)
            ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
    }

    public bool Remove(TKey key)
    {
        lock (SyncRoot)
        {
            if (_dictionary.Remove(key, out var value))
            {
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(
                    new KeyValuePair<TKey, TValue>(key, value), -1));
                return true;
            }
            return false;
        }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item)
    {
        lock (SyncRoot)
        {
            if (_dictionary.TryGetValue(item.Key, out var value) &&
                EqualityComparer<TValue>.Default.Equals(value, item.Value))
            {
                _dictionary.Remove(item.Key);
                CollectionChanged?.Invoke(NotifyCollectionChangedEventArgs<KeyValuePair<TKey, TValue>>.Remove(
                    new KeyValuePair<TKey, TValue>(item.Key, value), -1));
                return true;
            }
            return false;
        }
    }

#pragma warning disable CS8767
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767
    {
        lock (SyncRoot)
            return _dictionary.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        lock (SyncRoot)
        {
            foreach (var item in _dictionary)
                yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}