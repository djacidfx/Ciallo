using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;
using R3;

namespace Ciallo.Misc;

public static class BindItemList
{
    public static CompositeDisposable BindValue<T>([NotNull] this ItemList list, [NotNull] IReadOnlyList<T> items, 
        [NotNull] ReactiveProperty<T> property, Func<T, string> toString = null)
    {
        if (list.SelectMode != ItemList.SelectModeEnum.Single) throw new ArgumentException("Must be single", nameof(list));
        list.Clear();
        foreach (var item in items)
            list.AddItem(toString != null ? toString(item) : item.ToString());
        
        var subs = new CompositeDisposable();
        property.Subscribe(value =>
        {
            list.DeselectAll();
            var idx = items.IndexOf(value);
            if (idx >= 0) list.Select(idx);
        }).AddTo(subs);

        list.SignalAsObservable<long>(ItemList.SignalName.ItemSelected)
            .Subscribe(index => property.Value = items[(int)index])
            .AddTo(subs);
        return subs;
    }
}