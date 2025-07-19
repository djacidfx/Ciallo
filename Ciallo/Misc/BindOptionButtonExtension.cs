using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using R3;
using Humanizer;

namespace Ciallo.Misc;

/// <summary>
/// The control commonly used to display an enum in the editor.
/// </summary>

public static class BindOptionButtonExtension
{
    /// <summary>
    /// Add enum members to the OptionButton and two-way bind it to a ReactiveProperty. Will clean existing option items.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="property"></param>
    /// <typeparam name="T">Must be enum type.</typeparam>
    public static void BindEnum<T>(this OptionButton button, ReactiveProperty<T> property) where T : Enum
    {
        button.Clear();
        var memberNames = Enum.GetNames(typeof(T));
        var values = Enum.GetValues(typeof(T));

        foreach (var name in memberNames)
        {
            button.AddItem(name.Humanize());
        }
        
        // Set current value
        button.Selected = Array.IndexOf(values, property.Value);
        
        // Bind
        button.ItemSelected += index =>
        {
            if (index < 0 || index >= values.Length)
                return;
            property.Value = (T)values.GetValue(index)!;
        };

        var subscription = property.Subscribe(value =>
        {
            button.Selected = Array.IndexOf(values, value);
        });
        button.TreeEntered += () =>
        {
            subscription.AddTo(button);
        };
    }
    
    /// <summary>
    /// Add list items to the OptionButton and two-way bind the selection to a ReactiveProperty. Will clean existing option items.
    /// If the current property value is not in the list, the option button will be unselected.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="items">The list options.</param>
    /// <param name="property"></param>
    /// <typeparam name="T">Use `ToString()` as item string.</typeparam>
    public static void BindValue<T>(this OptionButton button, List<T> items, ReactiveProperty<T> property)
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
            button.TreeEntered += () => subscription.AddTo(button);
        }
    }
}