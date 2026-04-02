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
        var syncDict = CreateSyncShapeDictionary(vectorFillLayerSetting.ReferenceLayers, out var subs);
        subs.AddTo(targetE);
        ArrangementBind(arr, syncDict).AddTo(targetE);

        // Others
        NewShapeLayerCmd.ShapeLayerNonDataCreation(targetE);

        // Overlay extra
        var overlayHolder = targetE.Get<OverlayHolder>();
        overlayHolder.Visible = false;
        overlayHolder.AddChild(new OverlayHolder()); // hold stroke overlay 
        overlayHolder.AddChild(new OverlayHolder()); // hold wireframe overlay
    }

    public override void Do(Entity targetE)
    {
        targetE.Tag<ToSerializeTag>();
    }
    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
    }

    public static CompositeDisposable ArrangementBind(Arrangement2D arr, ObservableDictionary<Entity, ImmutableArray<Vector2>> syncDict)
    {
        var subs = new CompositeDisposable();
        var polylineRidMap = new Dictionary<Entity, Rid>();

        // Initial population
        foreach (var (e, positions) in syncDict)
        {
            var rid = arr.CreatePolyline();
            polylineRidMap[e] = rid;
            arr.SetPolyline(rid, positions);
        }

        syncDict.ObserveDictionaryAdd().Subscribe(et =>
        {
            var rid = arr.CreatePolyline();
            polylineRidMap[et.Key] = rid;
            arr.SetPolyline(rid, et.Value);
        }).AddTo(subs);

        syncDict.ObserveDictionaryRemove().Subscribe(et =>
        {
            arr.RemovePolyline(polylineRidMap[et.Key]);
            polylineRidMap.Remove(et.Key);
        }).AddTo(subs);

        syncDict.ObserveDictionaryReplace().Subscribe(et =>
        {
            arr.SetPolyline(polylineRidMap[et.Key], et.NewValue);
        }).AddTo(subs);

        syncDict.ObserveClear().Subscribe(_ =>
        {
            foreach (var (_, rid) in polylineRidMap)
                arr.RemovePolyline(rid);
            polylineRidMap.Clear();
        }).AddTo(subs);

        return subs;
    }

    /// <summary>
    /// Create a sync dictionary to keep track of all shapes under layerEs
    /// </summary>
    /// <remarks>
    /// When setting entity's PolylineGeometry.Positions, this sync dict should replace element accordingly.
    /// When any LayerTreeNode children entity is added/removed, the corresponding entry in sync dictionary should also be added/removed.
    /// </remarks>
    public static ObservableDictionary<Entity, ImmutableArray<Vector2>> CreateSyncShapeDictionary(
        ObservableHashSet<Entity> layerEs, out CompositeDisposable subs)
    {
        ObservableDictionary<Entity, ImmutableArray<Vector2>> result = [];
        subs = new();

        // Per-layer subscriptions keyed by layer entity
        var layerSubs = new Dictionary<Entity, CompositeDisposable>();

        // Populate existing layers
        foreach (var layerE in layerEs)
            SubscribeLayer(layerE);

        // Watch layer set changes
        layerEs.ObserveAdd()
            .Select(et => et.Value)
            .Subscribe(SubscribeLayer)
            .AddTo(subs);

        layerEs.ObserveRemove()
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

        return result;

        void UnsubscribeLayer(Entity layerE)
        {
            if (!layerSubs.TryGetValue(layerE, out var layerDisposables)) return;
            layerDisposables.Dispose();
            layerSubs.Remove(layerE);
            // Remove all shapes that belonged to this layer
            foreach (var shapeE in layerE.Get<LayerTreeNode>().Children)
                result.Remove(shapeE);
        }

        void SubscribeLayer(Entity layerE)
        {
            var layerDisposables = new CompositeDisposable();
            layerSubs[layerE] = layerDisposables;

            var layerNode = layerE.Get<LayerTreeNode>();

            // Populate existing children
            foreach (var shapeE in layerNode.Children)
            {
                var positions = shapeE.Get<PolylineGeometry>().Positions;
                positions.Subscribe(p => result[shapeE] = p).AddTo(layerDisposables);
            }

            // Watch future children entering this layer
            layerNode.ObserveAddChild()
                .Select(et => et.Value)
                .Subscribe(shapeE =>
                {
                    var positions = shapeE.Get<PolylineGeometry>().Positions;
                    positions.Subscribe(p => result[shapeE] = p).AddTo(layerDisposables);
                }).AddTo(layerDisposables);

            // Watch children exiting this layer
            layerNode.ObserveRemoveChild()
                .Select(et => et.Value)
                .Subscribe(shapeE => result.Remove(shapeE))
                .AddTo(layerDisposables);
        }
    }
}