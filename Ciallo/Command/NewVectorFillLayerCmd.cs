using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using ObservableCollections;
using R3;
using Godot;

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

        var manager = new ArrangementManager().AddTo(targetE);
        vectorFillLayerSetting.ReferenceLayers.ObserveChanged().Subscribe(_ =>
        {
            var refLayers = vectorFillLayerSetting.ReferenceLayers;
            manager.Observe([.. refLayers.Select(e => e.Get<ShapeLayerPolylineIndex>())]);
        }).AddTo(targetE);
        targetE.Add(manager);

        // Others
        NewShapeLayerCmd.CreateNonDataComponents(targetE);

        // Bounded area view
        var boundedAreaView = new Polygon2D
        {
            Name = "BoundedArea",
            Antialiased = true,
        };
        targetE.AddNode(boundedAreaView);
        targetE.Get<ShapeLayerView>().AddChild(boundedAreaView, false, Node.InternalMode.Front);
        // Color & visibility — independent of arrangement state.
        AppPreference.VectorFillLayerBoundedAreaColor.Subscribe(color =>
        {
            if (!color.HasValue)
            {
                boundedAreaView.Visible = false;
                return;
            }
            boundedAreaView.Visible = true;
            boundedAreaView.Color = color.Value;
        }).AddTo(targetE);

        // Shape — ArrReady emits whenever the arrangement is settled and safe to query.
        // null means mid-rebuild; keep the last frame's triangles to avoid flicker.
        manager.ArrReady.Subscribe(arr =>
        {
            if (arr == null) return;
            boundedAreaView.SetTriangleResult(arr.GetTrianglesFromFace(arr.GetUnboundedFace()));
        }).AddTo(targetE);
        // Intentionally not set owner for boundedAreaView, so won't participate in exportation.

        // Overlay extra
        var overlayHolder = targetE.Get<OverlayHolder>();
        overlayHolder.Visible = false;
        overlayHolder.AddChild(new OverlayHolder()); // hold stroke overlay 
        overlayHolder.AddChild(new OverlayHolder()); // hold wireframe overlay
    }

    public override void Do(Entity targetE)
    {
        targetE.Get<ArrangementManager>().SyncModification();
        targetE.Tag<ToSerializeTag>();
    }
    public override void Undo(Entity targetE)
    {
        targetE.Detach<ToSerializeTag>();
        targetE.Get<ArrangementManager>().DesyncModification();
    }
}
