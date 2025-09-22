using System;
using R3;

namespace Ciallo.Misc;

public static class ReactivePropertyExtension
{
    public static ReactivePropertyView<TView> CreateView<T, TView>(this ReactiveProperty<T> property, Func<T, TView> transform)
    {
        var view =  new ReactivePropertyView<TView>();
        property.Subscribe(value => view.Value = transform(value)).AddTo(view);
        return view;
    }

    public static void AddTo<T>(this IDisposable disposable, ReactivePropertyView<T> view)
    {
        view.AddDisposable(disposable);
    }
}

public class ReactivePropertyView<T> : ReactiveProperty<T>
{
    // ReSharper disable once CollectionNeverQueried.Global
    public CompositeDisposable ExtraDisposable { get; } = new();

    public override void Dispose()
    {
        base.Dispose();
        ExtraDisposable.Dispose();
    }
    
    public void AddDisposable(IDisposable disposable)
    {
        ExtraDisposable.Add(disposable);
    }
}