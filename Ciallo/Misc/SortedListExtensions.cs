#nullable enable
using System;
using System.Collections.Generic;

namespace ObservableCollections;

public static class SortedListExtensions
{
    /// <summary>
    /// Returns the 0-based index of <paramref name="key"/>, or the bitwise complement of the
    /// insertion point if not found. Uses the same result shape as <see cref="Array.BinarySearch(Array, object)"/>.
    /// </summary>
    public static int BinarySearchIndex<TKey, TValue>(this SortedList<TKey, TValue> source, TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        IList<TKey> keys = source.Keys;
        IComparer<TKey> comparer = source.Comparer;
        int lo = 0;
        int hi = keys.Count - 1;

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

    /// <summary>Returns the index of the largest key less than or equal to <paramref name="key"/>, or <c>-1</c> if none exists.</summary>
    public static int FloorIndex<TKey, TValue>(this SortedList<TKey, TValue> source, TKey key)
        where TKey : notnull
    {
        int idx = source.BinarySearchIndex(key);
        if (idx >= 0) return idx;
        int ins = ~idx;
        return ins > 0 ? ins - 1 : -1;
    }

    /// <summary>Returns the index of the smallest key greater than or equal to <paramref name="key"/>, or <c>-1</c> if none exists.</summary>
    public static int CeilingIndex<TKey, TValue>(this SortedList<TKey, TValue> source, TKey key)
        where TKey : notnull
    {
        int idx = source.BinarySearchIndex(key);
        if (idx >= 0) return idx;
        int ins = ~idx;
        return ins < source.Count ? ins : -1;
    }

    /// <summary>
    /// Returns the index of the nearest key to <paramref name="key"/>: prefers the floor,
    /// falling back to the ceiling. Returns <c>-1</c> when the collection is empty.
    /// </summary>
    public static int FindNearestIndex<TKey, TValue>(this SortedList<TKey, TValue> source, TKey key)
        where TKey : notnull
    {
        int floorIndex = source.FloorIndex(key);
        return floorIndex >= 0 ? floorIndex : source.CeilingIndex(key);
    }

    public static TKey GetKeyAtIndex<TKey, TValue>(this SortedList<TKey, TValue> source, int index)
        where TKey : notnull
        => source.Keys[index];

    public static TValue GetValueAtIndex<TKey, TValue>(this SortedList<TKey, TValue> source, int index)
        where TKey : notnull
        => source.Values[index];
}
