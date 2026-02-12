using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Ciallo;

public static class ReadOnlyListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> self, T target, IEqualityComparer<T> comparer = null)
    {
        comparer ??= EqualityComparer<T>.Default;
        foreach (var (i, element) in self.Index())
        {
            if (comparer.Equals(element, target)) return i;
        }

        return -1;
    }
}