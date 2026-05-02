using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.Command;

[CommandBuilder]
public class NewVectorFillLayerCmd : CommandBase
{
    public Entity CopyE { get; }

    public override void OnDeletedAsDo() => TargetE.Delete();

    public NewVectorFillLayerCmd(Entity copyE = default)
    {
        CopyE = copyE;
    }

    public override void BeforeFirstDo(Entity targetE)
    {
        // Data
        var layerNode = new LayerTreeNode();
        targetE.Add(layerNode);

        var commonSetting = CopyE.IsNull
            ? new CommonLayerSetting
            {
                Name = { Value = $"{"Vector fill layer".Tr()}" }
            }
            : CopyE.Get<CommonLayerSetting>().Clone();
        targetE.Add(commonSetting);

        var vectorFillLayerSetting = CopyE.IsNull
            ? new VectorFillLayerSetting()
            : CopyE.Get<VectorFillLayerSetting>().Clone();
        if (vectorFillLayerSetting.ReferenceLayers.Any(e => e.World != targetE.World))
            vectorFillLayerSetting.ReferenceLayers.Clear();
        targetE.Add(vectorFillLayerSetting);

        var arr = new Arrangement2D();
        targetE.Add(arr);
        var helper = new ArrangementSynchronizationHelper(arr, vectorFillLayerSetting.ReferenceLayers);
        targetE.Add(helper);

        // Others
        NewShapeLayerCmd.CreateNonDataComponents(targetE);

        // Overlay extra
        var overlayHolder = targetE.Get<OverlayHolder>();
        overlayHolder.Visible = false;
        overlayHolder.AddChild(new OverlayHolder()); // hold stroke overlay 
        overlayHolder.AddChild(new OverlayHolder()); // hold wireframe overlay
    }

    public override void Do(Entity targetE)
    {
        targetE.Get<ArrangementSynchronizationHelper>().Subscribe();
        targetE.Tag<ToSerializeTag>();
    }
    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
        targetE.Get<ArrangementSynchronizationHelper>().Unsubscribe();
    }
}

// Design this class to avoid observing the shape layers change after user deleting this vector fill layer.
public class ArrangementSynchronizationHelper
{
    private readonly Arrangement2D _arr;
    private readonly ObservableHashSet<Entity> _layerEs;
    private readonly ObservableDictionary<Entity, ImmutableArray<Vector2>> _shapePositions = [];
    public CompositeDisposable ArrangementSyncSubs;
    public CompositeDisposable ShapeTrackingSubs;

    public Dictionary<Entity, Rid> ShapeRids = [];

    public ArrangementSynchronizationHelper(Arrangement2D arr, ObservableHashSet<Entity> layerEs)
    {
        _arr = arr;
        _layerEs = layerEs;
        foreach (var layerE in layerEs)
        foreach (var shapeE in layerE.Get<LayerTreeNode>().Children)
        {
            _shapePositions[shapeE] = shapeE.Get<PolylineGeometry>().Positions.Value;
            var rid = arr.CreatePolyline();
            ShapeRids[shapeE] = rid;
            arr.SetPolylineWithSignal(rid, _shapePositions[shapeE]);
        }
    }

    public void Subscribe()
    {
        SubscribeShapeTracking();
        SubscribeArrangementSync();
    }

    public void Unsubscribe()
    {
        ArrangementSyncSubs?.Dispose();
        ShapeTrackingSubs?.Dispose();
    }

    private void SubscribeArrangementSync()
    {
        var subs = ArrangementSyncSubs = new();
        _shapePositions.ObserveDictionaryAdd().Subscribe(et =>
        {
            var rid = _arr.CreatePolyline();
            ShapeRids[et.Key] = rid;
            _arr.SetPolylineWithSignal(rid, et.Value);
        }).AddTo(subs);

        _shapePositions.ObserveDictionaryRemove().Subscribe(et =>
        {
            _arr.RemovePolylineWithSignal(ShapeRids[et.Key]);
            ShapeRids.Remove(et.Key);
        }).AddTo(subs);

        _shapePositions.ObserveDictionaryReplace().Subscribe(et =>
        {
            _arr.SetPolylineWithSignal(ShapeRids[et.Key], et.NewValue);
        }).AddTo(subs);

        _shapePositions.ObserveClear().Subscribe(_ =>
        {
            foreach (var (_, rid) in ShapeRids)
                _arr.RemovePolylineWithSignal(rid);
            ShapeRids.Clear();
        }).AddTo(subs);
    }

    /// <summary>
    /// Subscribe to keep _shapePositions up to date. Does not repopulate existing entries.
    /// </summary>
    private void SubscribeShapeTracking()
    {
        var subs = ShapeTrackingSubs = new();

        // Per-layer subscriptions keyed by layer entity
        var layerSubs = new Dictionary<Entity, CompositeDisposable>();

        // Attach to existing layers
        foreach (var layerE in _layerEs)
            SubscribeLayer(layerE);

        // Watch layer set changes
        _layerEs.ObserveAdd()
            .Select(et => et.Value)
            .Subscribe(layerE =>
            {
                // Populate existing shapes before subscribing, so .Skip(1) in SubscribeLayer is correct
                foreach (var shapeE in layerE.Get<LayerTreeNode>().Children)
                    _shapePositions[shapeE] = shapeE.Get<PolylineGeometry>().Positions.Value;
                SubscribeLayer(layerE);
            })
            .AddTo(subs);

        _layerEs.ObserveRemove()
            .Select(et => et.Value)
            .Subscribe(UnsubscribeLayer)
            .AddTo(subs);

        // Dispose all layer subs when the outer subs is disposed
        subs.Add(Disposable.Create(() =>
        {
            foreach (var d in layerSubs.Values)
                d.Dispose();
            layerSubs.Clear();
        }));

        void UnsubscribeLayer(Entity layerE)
        {
            if (!layerSubs.TryGetValue(layerE, out var layerDisposables)) return;
            layerDisposables.Dispose();
            layerSubs.Remove(layerE);
            foreach (var shapeE in layerE.Get<LayerTreeNode>().Children)
                _shapePositions.Remove(shapeE);
        }

        void SubscribeLayer(Entity layerE)
        {
            var layerDisposables = new CompositeDisposable();
            layerSubs[layerE] = layerDisposables;

            var layerNode = layerE.Get<LayerTreeNode>();
            var shapeSubs = new Dictionary<Entity, IDisposable>();

            // _shapePositions already has correct values for existing shapes — skip current emission, watch future changes only
            foreach (var shapeE in layerNode.Children)
                shapeSubs[shapeE] = shapeE.Get<PolylineGeometry>().Positions
                    .Skip(1)
                    .Subscribe(p => _shapePositions[shapeE] = p);

            // New shapes entering: subscribe from first emission to populate dict
            layerNode.ObserveAddChild()
                .Select(et => et.Value)
                .Subscribe(shapeE =>
                {
                    shapeSubs[shapeE] = shapeE.Get<PolylineGeometry>().Positions
                        .Subscribe(p => _shapePositions[shapeE] = p);
                }).AddTo(layerDisposables);

            // Shapes leaving: dispose per-shape subscription and remove from dict
            layerNode.ObserveRemoveChild()
                .Select(et => et.Value)
                .Subscribe(shapeE =>
                {
                    if (shapeSubs.Remove(shapeE, out var d))
                        d.Dispose();
                    _shapePositions.Remove(shapeE);
                })
                .AddTo(layerDisposables);

            // Dispose all remaining per-shape subscriptions when the layer is unsubscribed
            layerDisposables.Add(Disposable.Create(() =>
            {
                foreach (var d in shapeSubs.Values)
                    d.Dispose();
                shapeSubs.Clear();
            }));
        }
    }
}