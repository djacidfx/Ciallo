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
    public static void BindEnum<T>(this OptionButton button, [NotNull] ReactiveProperty<T> property) where T : Enum
    {
        var values = (T[])Enum.GetValues(typeof(T));
        button.BindValue(values.ToList(), property);
    }
    
    /// <summary>
    /// Take list items as the OptionButton items and two-way bind the selection to a ReactiveProperty. Will clean existing option items.
    /// If the current property value is not in the list, the option button will be unselected.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="items">The list options.</param>
    /// <param name="property"></param>
    /// <typeparam name="T">Use `ToString()` as item string.</typeparam>
    public static void BindValue<T>(this OptionButton button, List<T> items, [NotNull] ReactiveProperty<T> property)
    {
        button.Clear();
        foreach (var i in items)
        {
            button.AddItem(i.ToString());
        }
        
        // Set current value
        button.Selected = items.IndexOf(property.Value);
        
        // Bind
        button.ItemSelected += index =>
        {
            if (index < 0 || index >= items.Count)
                return;
            property.Value = items[(int)index];
        };
        var subscription = property.Subscribe(value => button.Selected = items.IndexOf(value));
        if (button.IsInsideTree())
        {
            subscription.AddTo(button);
        }
        else
        {
            button.SignalAsObservable(Node.SignalName.TreeEntered)
                .Take(1)
                .Subscribe(_ => subscription.AddTo(button));
        }
    }
}