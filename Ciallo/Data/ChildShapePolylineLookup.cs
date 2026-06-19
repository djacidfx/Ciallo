using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Ciallo.Geometry;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

public readonly record struct IndexedPolyline(ImmutableArray<Vector2> Positions, Rect2 Bounds);

public class ChildShapePolylineLookup
{
    private readonly ObservableDictionary<Entity, IndexedPolyline> _polylines = [];
    public IReadOnlyObservableDictionary<Entity, IndexedPolyline> Polylines => _polylines;
    public int Generation { get; private set; }

    private readonly Entity _layerE;
    private CompositeDisposable _subs;

    public ChildShapePolylineLookup(Entity layerE)
    {
        _layerE = layerE;
        Rebuild();
    }

    public void Subscribe()
    {
        _subs?.Dispose();
        var subs = _subs = new CompositeDisposable();
        var layerNode = _layerE.Get<LayerTreeNode>();
        var shapeSubs = new Dictionary<Entity, IDisposable>();

        foreach (var shapeE in layerNode.Children)
            shapeSubs[shapeE] = SubscribeShape(shapeE);

        layerNode.ObserveAddChild()
            .Select(et => et.Value)
            .Subscribe(shapeE =>
            {
                _polylines[shapeE] = CreateIndexedPolyline(shapeE.Get<PolylineGeometry>().Positions.Value);
                Generation++;
                shapeSubs[shapeE] = SubscribeShape(shapeE);
            })
            .AddTo(subs);

        layerNode.ObserveRemoveChild()
            .Select(et => et.Value)
            .Subscribe(shapeE =>
            {
                shapeSubs.Remove(shapeE, out var shapeSub);
                shapeSub?.Dispose();
                _polylines.Remove(shapeE);
                Generation++;
            })
            .AddTo(subs);

        subs.Add(Disposable.Create(() =>
        {
            foreach (var sub in shapeSubs.Values)
                sub.Dispose();
        }));
    }

    public void Unsubscribe()
    {
        _subs?.Dispose();
        _subs = null;
    }

    private IDisposable SubscribeShape(Entity shapeE)
    {
        return shapeE.Get<PolylineGeometry>().Positions
            .Skip(1)
            .Subscribe(p =>
            {
                _polylines[shapeE] = CreateIndexedPolyline(p);
                Generation++;
            });
    }

    private void Rebuild()
    {
        _polylines.Clear();
        foreach (var shapeE in _layerE.Get<LayerTreeNode>().Children)
            _polylines[shapeE] = CreateIndexedPolyline(shapeE.Get<PolylineGeometry>().Positions.Value);
        Generation++;
    }

    private static IndexedPolyline CreateIndexedPolyline(ImmutableArray<Vector2> positions)
    {
        return new(positions, positions.Length == 0 ? new Rect2() : positions.GetBoundingBox());
    }
}
