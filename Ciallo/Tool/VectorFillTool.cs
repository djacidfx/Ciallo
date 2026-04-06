using System.Linq;
using System.Runtime.InteropServices;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;
using Godot.Collections;

namespace Ciallo.Tool;

[RegisterTool(ToolButton.VectorFill)]
public class VectorFillTool : StateMachineToolBase
{
    public readonly VectorFillHover Hover = new();
    public readonly PaintVectorFillMarkerInteractor Left = new();
    public readonly HintUserErrorMessageDummy Message = new("No brush selected");

    protected override void ConfigureStateMachine()
    {
        ConfigureInitial(Hover)
            .PermitDynamic(Press(MouseButton.Left), () =>
            {
                var e = Document.Get<SelectionManager>().WorkingVectorFillBrush.Value;
                return e.IsDyingOrDead ? Message : Left;
            });
        Configure(Left)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
        Configure(Message)
            .Permit(Release(MouseButton.Left), Hover)
            .Permit(Press(AppActions.CancelInteraction), Hover)
            .Permit(Press(AppActions.ConfirmInteraction), Hover);
    }

    public override bool CanHandleLayer(params Entity[] layerEs)
    {
        if (layerEs.Length != 1) return false;
        var e = layerEs.Single();
        return e.Has<VectorFillLayerSetting>();
    }

    public override void OnActivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
            WorkingLayer.Get<OverlayHolder>().Visible = true;
    }

    public override void OnDeactivated()
    {
        if (WorkingLayer.Has<VectorFillLayerSetting>())
            WorkingLayer.Get<OverlayHolder>().Visible = false;
    }
}

public static class Polygon2DExtension
{
    public static void SetPolygonWithQueryResult(this Polygon2D node, Arrangement2D arr, Vector2 point)
    {
        var faceRid = arr.Query(point);
        var polygons = arr.GetFacePolygons(faceRid);
        if (!faceRid.IsValid || polygons.Count == 0)
        {
            node.Polygon = null;
            node.Polygons = null;
            return;
        }
        if (arr.IsUnboundedFace(faceRid))
        {
            var holes = polygons;
            node.Polygon = holes.SelectMany(p => p).ToArray();
            Array holeIndices = [];
            int currentStartIndex = 0;
            foreach (var hole in holes)
            {
                int[] index = [..Enumerable.Range(currentStartIndex, hole.Length)];
                holeIndices.Add(index);
                currentStartIndex += hole.Length;
            }
            node.Polygons = holeIndices;
        }
        else
        {
            var polygonWithHoles = polygons;
            if (polygonWithHoles.Count == 1)
            {
                node.SetPolygon(polygonWithHoles.Single());
                node.Polygons = null;
            }
            else
            {
                var simplyConnectedPolygon = polygonWithHoles.ConnectHoles();
                node.SetPolygon(CollectionsMarshal.AsSpan(simplyConnectedPolygon));
                node.Polygons = null;
            }
        }
    }

    public static void Clear(this Polygon2D node)
    {
        node.Polygon = null;
        node.Polygons = null;
    }
}