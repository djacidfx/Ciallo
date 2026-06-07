using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        var arr = new Arrangement().AddTo(targetE);
        targetE.Add(arr);
        var helper = new ArrangementSynchronizationHelper(
            arr,
            [.. vectorFillLayerSetting.ReferenceLayers.Select(e => e.Get<ShapeLayerPolylineIndex>())]);
        targetE.Add(helper);

        // Others
        NewShapeLayerCmd.CreateNonDataComponents(targetE);

        var boundedAreaView = new Polygon2D
        {
            Name = "BoundedArea",
            Antialiased = true,
            Visible = false,
        };
        targetE.AddNode(boundedAreaView);
        targetE.Get<ShapeLayerView>().AddChild(boundedAreaView, false, Node.InternalMode.Front);
        AppPreference.VectorFillLayerBoundedAreaColor
            .Merge(arr.StructureChanged.Select(_ => AppPreference.VectorFillLayerBoundedAreaColor.Value))
            .ThrottleLastFrame(1)
            .Subscribe(color => boundedAreaView.SetBoundedArea(arr, color))
            .AddTo(targetE);
        // Intentionally not set owner for boundedAreaView, so won't participate in exportation.

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
