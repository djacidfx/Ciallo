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
    public static ReactiveProperty<TTarget> Project<TSource, TTarget>(
        this ReactiveProperty<TSource> source,
        Func<TSource, TTarget> readSelector,
        Func<TTarget, TSource> writeSelector)
    {
        var result = new ReactiveProperty<TTarget>(readSelector(source.Value));
        var sub = source.Subscribe(v => result.Value = readSelector(v));
        result.Subscribe(v => source.Value = writeSelector(v), _ => sub.Dispose());
        return result;
    }

    /// <summary>
    /// Project inner ReactiveProperty outside, similiar to Switch() operator.
    /// </summary>
    /// <remarks>
    /// Binding's subscription is managed by returned ReactiveProperty. Dispose it to dispose the subscription.
    /// </remarks>
    public static ReactiveProperty<T> Flatten<T>(this Observable<ReactiveProperty<T>> outer)
    {
        var result = new ReactiveProperty<T>();

        IDisposable innerSub = null;
        ReactiveProperty<T> currentInner = null;

        var outerSub = outer.Subscribe(inner =>
        {
            innerSub?.Dispose();
            currentInner = inner;
            result.Value = inner is null ? default : inner.Value;
            innerSub = inner?.Subscribe(v => result.Value = v);
        });

        result.Subscribe(v =>
            {
                currentInner?.Value = v;
            },
            _ =>
            {
                innerSub?.Dispose();
                outerSub.Dispose();
            });

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

public static class R3OperatorExtension
{
    /// <summary>
    /// Prepend a T default value to the observable. Works indentical to Prepend(or StartWith) operator but doesn't require its given value.
    /// </summary>
    public static Observable<T> PrependDefault<T>(this Observable<T> source)
    {
        return source.Prepend(default(T));
    }
}
