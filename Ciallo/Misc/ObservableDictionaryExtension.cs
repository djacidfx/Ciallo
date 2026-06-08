using System.Collections.Generic;
using System.Linq;
using ObservableCollections;

public static class ObservableDictionaryExtension
{
    extension<TKey, TValue>(ObservableDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        public IEnumerable<TKey> Keys => source.Select(pair => pair.Key);
    }
}
