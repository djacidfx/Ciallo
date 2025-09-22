using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Misc;

// Note: DeselectAll does not emit ItemSelected signal
// Shen: being lazy here, not implement output disposable version
public static class BindItemList
{
    // Fix item list
    public static void BindValue<T>(this ItemList control, [NotNull] IReadOnlyList<T> items, 
        [NotNull] ReactiveProperty<T> property, Func<T, string> toString = null)
    {
        if (control.SelectMode != ItemList.SelectModeEnum.Single) throw new ArgumentException("List must be single selectable", nameof(control));
        control.Clear();
        foreach (var item in items)
            control.AddItem(toString != null ? toString(item) : item.ToString());
        
        var subs = new CompositeDisposable();
        property.Subscribe(value =>
        {
            var idx = items.IndexOf(value);
            control.Select(idx);
        }).AddTo(subs);

        control.SignalAsObservable<long>(ItemList.SignalName.ItemSelected)
            .Subscribe(idx => property.Value = items[(int)idx])
            .AddTo(subs);
        subs.AddTo(control);
    }
    
    // Dynamic list view created from R3.ObservableList
    public static void BindValue<T>(this ItemList control, IWritableSynchronizedView<T,ReactiveProperty<string>> view,
        [NotNull] ReactiveProperty<T> property)
    {
        if (control.SelectMode != ItemList.SelectModeEnum.Single) throw new ArgumentException("List must be single selectable", nameof(control));
        control.Clear();
        
        var subs = new CompositeDisposable();
        foreach (var viewProperty in view)
        {
            control.AddItem(viewProperty.Value);
            viewProperty.Subscribe(s =>
            {
                using var list = view.ToViewList();
                var idx = list.IndexOf(viewProperty);
                if (idx != -1) control.SetItemText(idx, s);
            }).AddTo(subs);
        }
        
        view.ObserveChanged().Subscribe(e =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    control.AddItem(e.NewItem.View.Value);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    control.RemoveItem(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    control.SetItemText(e.NewStartingIndex, e.NewItem.View.Value);
                    break;
                case NotifyCollectionChangedAction.Move:
                    control.MoveItem(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    control.Clear();
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }).AddTo(subs);
        
        property.Subscribe(value =>
        {
            int idx = -1;
            for(int i = 0; i < view.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(view.GetAt(i).Value, value))
                {
                    idx = i;
                    break;
                }
            }
            if(idx != -1) control.Select(idx);
            else control.DeselectAll(); // .Select(-1) gives error
        }).AddTo(subs);
        
        control.SignalAsObservable<long>(ItemList.SignalName.ItemSelected)
            .Subscribe(idx => property.Value = view.GetAt((int)idx).Value)
            .AddTo(subs);
        
        subs.AddTo(control);
    }
}