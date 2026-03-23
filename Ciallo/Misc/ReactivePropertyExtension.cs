using System;
using System.Collections.Generic;
using System.Numerics;
using R3;

namespace Ciallo;

public static class ReactivePropertyExtension
{
    /// <summary>
    /// Create a ReactiveProperty two-way binded to the original one through the given selectors.
    /// Commonly used for unit conversion.
    /// </summary>
    /// <returns></returns>
    public static ReactiveProperty<TTarget> Project<TSource, TTarget>(
        this ReactiveProperty<TSource> source,
        Func<TSource, TTarget> readSelector,
        Func<TTarget, TSource> writeSelector,
        out CompositeDisposable subs)
    {
        subs = new();
        var view = new ReactiveProperty<TTarget>(readSelector(source.Value));
        view.Subscribe(v => source.Value = writeSelector(v)).AddTo(subs);
        source.Subscribe(v => view.Value = readSelector(v)).AddTo(subs);
        return view;
    }

    public static ReactiveProperty<T> ProjectFloatingNumber<T>(
        this ReactiveProperty<T> source,
        Func<T, T> readSelector,
        Func<T, T> writeSelector,
        out CompositeDisposable subs) where T : IFloatingPoint<T>
    {
        subs = new();
        var view = new ReactiveProperty<T>(readSelector(source.Value), FloatComparer<T>.Instance);
        view.Subscribe(v => source.Value = writeSelector(v)).AddTo(subs);
        source.Subscribe(v => view.Value = readSelector(v)).AddTo(subs);
        return view;
    }

    /// <summary>
    /// Project inner ReactiveProperty outside, similiar to Switch() operator
    /// </summary>
    public static ReactiveProperty<T> Flatten<T>(this Observable<ReactiveProperty<T>> outer, out CompositeDisposable subs)
    {
        var result = new ReactiveProperty<T>();
        subs = new();

        IDisposable innerSub = null;
        ReactiveProperty<T> currentInner = null;

        outer.Subscribe(inner =>
        {
            innerSub?.Dispose();
            currentInner = inner;
            result.Value = inner.Value;
            innerSub = inner.Subscribe(v => result.Value = v);
        }).AddTo(subs);

        result.Subscribe(v =>
        {
            currentInner?.Value = v;
        }).AddTo(subs);

        Disposable.Create(() => innerSub?.Dispose()).AddTo(subs);

        return result;
    }
}

public class FloatComparer<T> : IEqualityComparer<T> where T : IFloatingPoint<T>
{
    public static FloatComparer<T> Instance { get; } = new();

    private readonly T _tolerance = T.CreateChecked(1e-5);

    /// <summary>
    /// Returns true if the absolute difference between x and y is within the configured tolerance.
    /// </summary>
    public bool Equals(T x, T y)
    {
        // Use the static Abs operator from IFloatingPoint<T>
        return T.Abs(x - y) <= _tolerance;
    }

    public int GetHashCode(T obj)
    {
        return obj.GetHashCode();
    }
}