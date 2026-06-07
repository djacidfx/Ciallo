using System;
using System.Collections.Immutable;
using Frent;
using Ciallo.Geometry;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

public readonly record struct IndexedPolyline(ImmutableArray<Vector2> Positions, Rect2 Bounds);

public class ShapeLayerPolylineIndex
{
    public readonly ObservableDictionary<Entity, IndexedPolyline> Polylines = [];
    public int Generation { get; private set; }

    private readonly Entity _layerE;
    private CompositeDisposable _subs;

    public ShapeLayerPolylineIndex(Entity layerE)
    {
        _layerE = layerE;
        Rebuild();
    }

    public void Subscribe()
    {
        _subs?.Dispose();
        var subs = _subs = new CompositeDisposable();
        var layerNode = _layerE.Get<LayerTreeNode>();
        var shapeSubs = new System.Collections.Generic.Dictionary<Entity, IDisposable>();

        foreach (var shapeE in layerNode.Children)
            shapeSubs[shapeE] = SubscribeShape(shapeE, skipCurrent: true);

        layerNode.ObserveAddChild()
            .Select(et => et.Value)
            .Subscribe(shapeE =>
            {
                Polylines[shapeE] = CreateIndexedPolyline(shapeE.Get<PolylineGeometry>().Positions.Value);
                Generation++;
                shapeSubs[shapeE] = SubscribeShape(shapeE, skipCurrent: true);
            })
            .AddTo(subs);

        layerNode.ObserveRemoveChild()
            .Select(et => et.Value)
            .Subscribe(shapeE =>
            {
                shapeSubs.Remove(shapeE, out var shapeSub);
                shapeSub?.Dispose();
                Polylines.Remove(shapeE);
                Generation++;
            })
            .AddTo(subs);

        subs.Add(Disposable.Create(() =>
        {
            foreach (var sub in shapeSubs.Values)
                sub.Dispose();
            shapeSubs.Clear();
        }));
    }

    public void Unsubscribe()
    {
        _subs?.Dispose();
        _subs = null;
    }

    private IDisposable SubscribeShape(Entity shapeE, bool skipCurrent)
    {
        var observable = shapeE.Get<PolylineGeometry>().Positions.AsObservable();
        if (skipCurrent)
            observable = observable.Skip(1);
        return observable.Subscribe(p =>
        {
            Polylines[shapeE] = CreateIndexedPolyline(p);
            Generation++;
        });
    }

    private void Rebuild()
    {
        Polylines.Clear();
        foreach (var shapeE in _layerE.Get<LayerTreeNode>().Children)
            Polylines[shapeE] = CreateIndexedPolyline(shapeE.Get<PolylineGeometry>().Positions.Value);
        Generation++;
    }

    private static IndexedPolyline CreateIndexedPolyline(ImmutableArray<Vector2> positions)
    {
        return new(positions, positions.Length == 0 ? new Rect2() : positions.GetBoundingBox());
    }
}
