using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Godot;
using R3;
using ObservableCollections;

namespace Ciallo.Misc;

public static class BindOptionButton
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
        button.BindValue(values, property);
    }

    /// <summary>
    /// Take list items as the OptionButton items and two-way bind the selection to a ReactiveProperty. Will clean existing option items.
    /// If the current property value is not in the list, the option button will be unselected.
    /// </summary>
    /// <param name="button"></param>
    /// <param name="items">The list options.</param>
    /// <param name="property"></param>
    /// <param name="toString"></param>
    /// <param name="subs"></param>
    /// <typeparam name="T">Use `ToString()` as item string.</typeparam>
    public static void BindValue<T>(this OptionButton button, IReadOnlyList<T> items,
        [NotNull] ReactiveProperty<T> property, Func<T, string> toString, out CompositeDisposable subs)
    {
        if(button.AllowReselect) throw new ArgumentException("AllowReselect must be false.");
        button.Clear();
        foreach (var item in items)
            button.AddItem(toString(item));
        
        // Bind
        subs = new();
        property.Subscribe(value => button.Selected = items.IndexOf(value)).AddTo(subs);
        button.OnItemSelectedAsObservable().Subscribe(index =>
        {
            if (index != -1) property.Value = items[(int)index];
            if (index == -1) property.Value = default;
        }).AddTo(subs);
    }
    
    public static void BindValue<T>(this OptionButton button, IReadOnlyList<T> items,
        [NotNull] ReactiveProperty<T> property, Func<T, string> toString = null)
    {
        toString ??= v => v.ToString();
        BindValue(button, items, property, toString, out var subs);
        subs.AddTo(button);
    }
    
    public static void BindValue<T>(this OptionButton button,
        IWritableSynchronizedView<T, ReactiveProperty<string>> view,
        [NotNull] ReactiveProperty<T> property,
        out CompositeDisposable subs)
    {
        if (button.AllowReselect) throw new ArgumentException("AllowReselect must be false.", nameof(button));
        button.Clear();

        subs = new();

        // Initialize with existing view items
        foreach (var viewProperty in view)
        {
            button.AddItem(viewProperty.Value);
            viewProperty.Subscribe(s =>
            {
                using var list = view.ToViewList();
                var idx = list.IndexOf(viewProperty);
                if (idx != -1) button.SetItemText(idx, s);
            }).AddTo(subs);
        }

        // Observe collection changes
        view.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    button.AddItem(e.NewItem.View.Value);
                    button.MoveItem(button.GetItemCount() - 1, e.NewStartingIndex);
                    if(EqualityComparer<T>.Default.Equals(e.NewItem.Value, property.Value))
                        button.Select(e.NewStartingIndex); // Necessary after clearing the control (in Reset)
                    break;
                case NotifyCollectionChangedAction.Remove:
                    button.RemoveItem(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    button.SetItemText(e.NewStartingIndex, e.NewItem.View.Value);
                    break;
                case NotifyCollectionChangedAction.Move:
                    button.MoveItem(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    button.Clear();
                    break;
                default: throw new ("Unreachable");
            }
        }).AddTo(subs);

        // Bind property changes to selection
        property.Subscribe(value =>
        {
            var idx = -1;
            for (int i = 0; i < view.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(view.GetAt(i).Value, value))
                {
                    idx = i;
                    break;
                }
            }
            button.Selected = idx;
        }).AddTo(subs);

        // Bind selection changes to property
        button.OnItemSelectedAsObservable().Subscribe(index =>
        {
            property.Value = index == -1 ? default : view.GetAt((int)index).Value;
        }).AddTo(subs);
    }

    public static void BindValue<T>(this OptionButton button,
        IWritableSynchronizedView<T, ReactiveProperty<string>> view,
        [NotNull] ReactiveProperty<T> property)
    {
        BindValue(button, view, property, out var subs);
        subs.AddTo(button);
    }
    
    // Pitfall: OptionButton lacks MoveItem, so we need to rebuild items on Move
    public static void MoveItem(this OptionButton button, int from, int to)
    {
        var count = button.GetItemCount();
        var texts = new List<string>(count);
        for (int i = 0; i < count; i++)
            texts.Add(button.GetItemText(i));
        var selected = button.Selected;
        var movedText = texts[from];
        texts.RemoveAt(from);
        texts.Insert(to, movedText);
        button.Clear();
        foreach (var t in texts)
            button.AddItem(t);
        // Restore selection
        if (selected == from)
            button.Selected = to;
        else if (from < to && selected > from && selected <= to)
            button.Selected = selected - 1;
        else if (from > to && selected >= to && selected < from)
            button.Selected = selected + 1;
        else
            button.Selected = selected;
    }
}