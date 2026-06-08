using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo;
using Ciallo.Data;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Geometry;

// Owns an Arrangement and synchronizes it with a fixed set of shape-layer polyline indexes.
public class ArrangementManager : IDisposable
{
    // The currently queryable Arrangement, or null if not ready (e.g. mid-rebuild on a worker thread).
    // Subscribers must handle the null case. Same reference is re-emitted on every mutation.
    private readonly ReactiveProperty<Arrangement> _arrReady;
    public ReadOnlyReactiveProperty<Arrangement> ArrReady => _arrReady;

    private Arrangement _arr = new();
    private IReadOnlyList<ShapeLayerPolylineIndex> _indexes;
    private CompositeDisposable _subs;

    public ArrangementManager()
    {
        // Empty arrangement is queryable from the start.
        _arrReady = new ReactiveProperty<Arrangement>(_arr);
    }

    /// <summary>
    /// Observe the given shape-layer polyline indexes and synchronize the arrangement with them.
    /// Would be burst called 100+ times on project load. Need asynchronous concurrency.
    /// </summary>
    /// <remarks>
    /// Would be called multiple times, clean up previous subscriptions and start observing the new set of indexes.
    /// </remarks>
    public void Observe(params ShapeLayerPolylineIndex[] indexes)
    {
        _indexes = indexes;
        Rebuild();
    }

    public void SyncModification()
    {
        if (_indexes == null)
            return;
        _subs?.Dispose();
        var subs = _subs = new CompositeDisposable();

        foreach (var index in _indexes)
        {
            index.Polylines.ObserveDictionaryAdd().Subscribe(et =>
            {
                AddShape(et.Key, et.Value.Positions);
            }).AddTo(subs);

            index.Polylines.ObserveDictionaryRemove().Subscribe(et =>
            {
                RemoveShape(et.Key);
            }).AddTo(subs);

            index.Polylines.ObserveDictionaryReplace().Subscribe(et =>
            {
                _arr.SetPolyline(et.Key.PackedValue, et.NewValue.Positions);
            }).AddTo(subs);

            index.Polylines.ObserveClear().Subscribe(_ =>
            {
                Clear();
            }).AddTo(subs);
        }
    }

    public void DesyncModification()
    {
        _subs?.Dispose();
        _subs = null;
    }

    public void Dispose()
    {
        DesyncModification();
        _arrReady.Dispose();
        _arr.Dispose();
    }

    private void Rebuild()
    {
        NotifyNotReady();
        foreach (var index in _indexes)
            foreach (var (shapeE, polyline) in index.Polylines)
                AddShape(shapeE, polyline.Positions);
        NotifyReady();
    }

    // Bypass ReactiveProperty's equality dedup by going through OnNext directly:
    // _arr is the same reference each time, but downstream consumers must still be re-triggered.
    private void NotifyReady() => _arrReady.OnNext(_arr);
    private void NotifyNotReady() => _arrReady.OnNext(null);

    private void AddShape(Entity shapeE, ImmutableArray<Vector2> positions)
    {
        long id = shapeE.PackedValue;
        _arr.CreatePolyline(id);
        _arr.SetPolyline(id, positions);
    }

    private void RemoveShape(Entity shapeE)
    {
        _arr.RemovePolyline(shapeE.PackedValue);
    }

    private void Clear()
    {
        _arr.Dispose();
        _arr = new Arrangement();
    }
}
