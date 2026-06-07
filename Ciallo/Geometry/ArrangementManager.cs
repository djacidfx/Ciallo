using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    private readonly Arrangement _arr = new();
    private IReadOnlyList<ShapeLayerPolylineIndex> _indexes;
    private readonly Dictionary<Entity, Rid> _shapeToRid = [];
    private readonly Dictionary<Rid, Entity> _ridToShape = [];
    private CompositeDisposable _subs;

    public IReadOnlyDictionary<Entity, Rid> ShapeToRid => _shapeToRid;
    public IReadOnlyDictionary<Rid, Entity> RidToShape => _ridToShape;
    public int Generation { get; private set; }

    public ArrangementManager()
    {
        // Empty arrangement is queryable from the start.
        _arrReady = new ReactiveProperty<Arrangement>(_arr);
    }

    /// <summary>
    /// Observe the given shape-layer polyline indexes and synchronize the arrangement with them.
    /// Would be burst called on project load 100+ times.
    /// </summary>
    /// <param name="indexes"></param>
    public void Observe(params ShapeLayerPolylineIndex[] indexes)
    {

        _indexes = indexes;
        Rebuild();
    }

    public void SyncModification()
    {
        _subs?.Dispose();
        var subs = _subs = new CompositeDisposable();

        foreach (var index in _indexes)
        {
            index.Polylines.ObserveDictionaryAdd().Subscribe(et =>
            {
                AddShape(et.Key, et.Value.Positions);
                Generation++;
            }).AddTo(subs);

            index.Polylines.ObserveDictionaryRemove().Subscribe(et =>
            {
                RemoveShape(et.Key);
                Generation++;
            }).AddTo(subs);

            index.Polylines.ObserveDictionaryReplace().Subscribe(et =>
            {
                _arr.SetPolyline(_shapeToRid[et.Key], et.NewValue.Positions);
                Generation++;
            }).AddTo(subs);

            index.Polylines.ObserveClear().Subscribe(_ =>
            {
                Clear();
                Generation++;
            }).AddTo(subs);
        }
    }

    public void DesyncModification()
    {
        _subs?.Dispose();
        _subs = null;
    }

    public Entity GetShape(Rid rid) => _ridToShape[rid];

    public void Dispose()
    {
        DesyncModification();
        _arrReady.Dispose();
        _arr.Dispose();
    }

    private void Rebuild()
    {
        Clear();
        foreach (var index in _indexes)
            foreach (var (shapeE, polyline) in index.Polylines)
                AddShape(shapeE, polyline.Positions);
        Generation++;
    }

    // Bypass ReactiveProperty's equality dedup by going through OnNext directly:
    // _arr is the same reference each time, but downstream consumers must still be re-triggered.
    private void NotifyReady() => _arrReady.OnNext(_arr);
    private void NotifyNotReady() => _arrReady.OnNext(null);

    private void AddShape(Entity shapeE, ImmutableArray<Vector2> positions)
    {
        var rid = _arr.CreatePolyline();
        _shapeToRid[shapeE] = rid;
        _ridToShape[rid] = shapeE;
        _arr.SetPolyline(rid, positions);
    }

    private void RemoveShape(Entity shapeE)
    {
        var rid = _shapeToRid[shapeE];
        _arr.RemovePolyline(rid);
        _shapeToRid.Remove(shapeE);
        _ridToShape.Remove(rid);
    }

    private void Clear()
    {
        foreach (var rid in _shapeToRid.Values)
            _arr.RemovePolyline(rid);
        _shapeToRid.Clear();
        _ridToShape.Clear();
    }
}
