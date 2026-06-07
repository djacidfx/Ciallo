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
    private readonly Arrangement _arr = new();
    private readonly IReadOnlyList<ShapeLayerPolylineIndex> _indexes;
    private readonly Dictionary<Entity, Rid> _shapeToRid = [];
    private readonly Dictionary<Rid, Entity> _ridToShape = [];
    private CompositeDisposable _subs;

    public Arrangement Arr => _arr;
    public IReadOnlyDictionary<Entity, Rid> ShapeToRid => _shapeToRid;
    public IReadOnlyDictionary<Rid, Entity> RidToShape => _ridToShape;
    public int Generation { get; private set; }

    public ArrangementManager(params ShapeLayerPolylineIndex[] indexes)
    {
        _indexes = indexes;
        Rebuild();
    }

    public void Subscribe()
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

    public void Unsubscribe()
    {
        _subs?.Dispose();
        _subs = null;
    }

    public Entity GetShape(Rid rid) => _ridToShape[rid];

    public void Dispose()
    {
        Unsubscribe();
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
