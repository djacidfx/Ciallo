using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;
using R3;
using Humanizer;

namespace Ciallo.Misc;

public static class BindOptionButtonExtension
{
    
    /// <summary>
    /// Take enum members as OptionButton items and two-way bind the ReactiveProperty. Will clean existing option items.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="property"></param>
    /// <typeparam name="T">Must be enum type.</typeparam>
    public static CompositeDisposable BindEnum<T>(this OptionButton button, [NotNull] ReactiveProperty<T> property) where T : Enum
    {
        var values = (T[])Enum.GetValues(typeof(T));
        return button.BindValue(values, property);
    }

    /// <summary>
    /// Take list items as the OptionButton items and two-way bind the selection to a ReactiveProperty. Will clean existing option items.
    /// If the current property value is not in the list, the option button will be unselected.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="items">The list options.</param>
    /// <param name="property"></param>
    /// <typeparam name="T">Use `ToString()` as item string.</typeparam>
    public static CompositeDisposable BindValue<T>(this OptionButton button, IReadOnlyList<T> items,
        [NotNull] ReactiveProperty<T> property)
    {
        if(button.AllowReselect) throw new ArgumentException("AllowReselect must be false.");
        button.Clear();
        foreach (var i in items)
        {
            button.AddItem(i.ToString());
        }
        
        // Bind
        var subs = new CompositeDisposable();
        property.Subscribe(value => button.Selected = items.IndexOf(value)).AddTo(subs);
        button.OnItemSelectedAsObservable().Subscribe(index => property.Value = items[(int)index]).AddTo(subs);
        return subs;
    }
}