using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Geometry;
using Frent;
using Godot;

namespace Ciallo.GuiControl;

[SceneTree(root: "Root"), Instantiable]
public partial class LayerAction : Control
{
    public Entity Document;

    public void Init(Entity document)
    {
        Document = document;
        var sm = Document.Get<SelectionManager>();
        Root.ConvertToShape
            .VisibleIf(sm.WorkingLayer, e => e.TryHas<VectorFillLayerSetting>())
            .AddTo(Document);
    }

    public override void _Ready()
    {
        Root.NewLayer.Pressed += OnNewShapeLayer;
        Root.NewFolder.Pressed += OnNewFolderLayer;
        Root.RemoveLayer.Pressed += OnRemoveLayer;
        Root.NewImage.Pressed += OnNewImage;
        Root.ConvertToShape.Pressed += OnConvertToShape;
    }

    public void OnNewShapeLayer()
    {
        var (parentE, index) = GetNewLayerInsertPosition();
        new CommandBuilder(Document.World.Create())
            .NewShapeLayer()
            .AddToLayerTree(parentE, index)
            .SetWorkingLayer()
            .Commit();
    }

    public void OnNewFolderLayer()
    {
        var (parentE, index) = GetNewLayerInsertPosition();
        new CommandBuilder(Document.World.Create())
            .NewFolderLayer()
            .AddToLayerTree(parentE, index)
            .SetWorkingLayer()
            .Commit();
    }

    public void OnRemoveLayer()
    {
        var document = Document;
        var currentLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        if (currentLayerE.IsNull) return;

        var workingLayerE = document.Get<SelectionManager>().WorkingLayer.Value;
        var root = document.Get<LayerTreeNode>();
        var workingLayerPath = root.FindPathTo(workingLayerE);
        var nextLayerPath = root.GetNextFocusPathAfterDeletion(workingLayerPath);
        var nextLayerE = nextLayerPath.IsEmpty ? document : root.GetDescendant(nextLayerPath);

        new CommandBuilder(nextLayerE)
            .SetWorkingLayer()
            .SetTarget(currentLayerE)
            .RemoveFromLayerTree()
            .DeleteLayer()
            .Commit();
    }

    public void OnNewImage()
    {
        if (AppDocumentManager.WorkingDocument.Value.IsNull) return;
        Root.FileDialog.Popup();
    }

    public void OnImageFileSelected(string path)
    {
        Image image;
        try
        {
            image = Image.LoadFromFile(path);
        }
        catch
        {
            return;
        }
        if (image == null) return;
        var (parentE, index) = GetNewLayerInsertPosition();
        new CommandBuilder(Document.World.Create())
            .NewImageLayer(image)
            .AddToLayerTree(parentE, index)
            .Commit();
    }

    public void OnConvertToShape()
    {
        var workingLayerE = Document.Get<SelectionManager>().WorkingLayer.Value;
        var arr = workingLayerE.Get<Arrangement2D>();
        var layerNode = workingLayerE.Get<LayerTreeNode>();
        var parentE = layerNode.ParentValue;
        var index = layerNode.Index;

        var markers = layerNode.Children.ToList(); // snapshot
        var shapeLayerE = workingLayerE.World.Create();
        var cmd = new CommandBuilder();

        // 1. Create new ShapeLayer at the same position
        var originalName = workingLayerE.Get<CommonLayerSetting>().Name.Value;
        cmd.SetTarget(shapeLayerE)
            .NewShapeLayer()
            .AddToLayerTree(parentE, index)
            .SetProperty(e => e.Get<CommonLayerSetting>().Name, originalName + " Converted");

        // 2. Convert each VectorMarker to a FilledPolygon inside the new ShapeLayer
        foreach (var markerE in markers)
        {
            var markerPos = markerE.Get<PolylineGeometry>().Positions.Value[0];
            var brushE = markerE.Get<VectorFillMarkerSetting>().BrushE.Value;

            var faceRid = arr.Query(markerPos);
            if (!faceRid.IsValid) continue;

            var facePolygons = arr.GetFacePolygons(faceRid);
            if (facePolygons.Count == 0) continue;

            if (arr.IsUnboundedFace(faceRid))
            {
                // Each hole of the unbounded face becomes a separate FilledPolygon
                foreach (var hole in facePolygons)
                    AddFilledPolygon(cmd, shapeLayerE, hole, brushE);
            }
            else
            {
                // Bounded face (possibly with holes) → one FilledPolygon
                IReadOnlyList<Vector2> polygon = facePolygons.Count == 1
                    ? facePolygons.Single()
                    : facePolygons.ConnectHoles();
                AddFilledPolygon(cmd, shapeLayerE, polygon, brushE);
            }
        }

        // 3. Set working layer to new ShapeLayer, then remove and delete the VectorFillLayer
        cmd.SetTarget(shapeLayerE)
            .SetWorkingLayer()
            .SetTarget(workingLayerE)
            .RemoveFromLayerTree()
            .DeleteLayer();

        cmd.Commit();
        return;

        void AddFilledPolygon(CommandBuilder builder, Entity targetLayerE,
            IReadOnlyList<Vector2> polygon, Entity brushE)
        {
            ImmutableArray<Vector2> positions = [..polygon, polygon[0]];
            int n = positions.Length;
            ImmutableArray<float> ones = [..Enumerable.Repeat(1.0f, n)];
            ImmutableArray<Vector2> zeros = [..Enumerable.Repeat(Vector2.Zero, n)];

            builder.SetTarget(targetLayerE.World.Create())
                .NewFilledPolygon()
                .AddToLayerTree(targetLayerE)
                .SetPolylineGeometry(positions, ones, ones, zeros)
                .SetProperty(e => e.Get<FilledPolygonSetting>().BrushE, brushE);
        }
    }

    /// <summary>
    /// Returns the parent entity and insertion index for a new layer based on the current working layer.
    /// Folder working layer → last child (visual top). Regular layer → sibling above. No selection → append to root.
    /// </summary>
    private (Entity parentE, int index) GetNewLayerInsertPosition()
    {
        var workingLayerE = Document.Get<SelectionManager>().WorkingLayer.Value;
        if (workingLayerE.IsNull || workingLayerE.IsDocument)
            return (AppDocumentManager.WorkingDocument.Value, -1);

        if (workingLayerE.Has<FolderLayerSetting>())
            return (workingLayerE, -1); // -1 resolves to Children.Count = last child = visual top

        // Regular layer: insert as sibling just above in screen (higher index in reversed display)
        var layerNode = workingLayerE.Get<LayerTreeNode>();
        return (layerNode.ParentValue, layerNode.Index + 1);
    }
}