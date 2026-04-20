using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

public partial class AutoloadGuiControl : Node
{
    public override void _Ready()
    {
        AppDocumentManager.LoadedDocuments.ObserveAdd().Select(et => et.Value).Subscribe(document =>
        {
            // Layer tree control
            var layerPanel = GetTree().GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().Single();
            layerPanel.CreateAdd(document);

            // Paint panel
            var paintPanelContainer = GetTree().GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().Single();
            var paintPanel = paintPanelContainer.CreateAddPaintPanel(document);

            // World view
            var worldView = paintPanel.GetNode<WorldView>("%WorldView");
            document.Add(worldView);

            // World overlay
            var worldOverlay = paintPanel.GetNode<WorldOverlay>("%WorldOverlay");
            document.Add(worldOverlay);
            document.Add<OverlayHolder>(worldOverlay);

            // World body
            var worldBody = paintPanel.GetNode<WorldBody>("%WorldBody");
            document.Add(worldBody);
            document.Add<BodyHolder>(worldBody);

            // SubViewport holder (a dummy node for debugging efficiently)
            var subViewportHolder = new SubViewportHolder();
            paintPanel.AddChild(subViewportHolder);
            document.Add(subViewportHolder);

            // Document stroke brush editor
            var editor = StrokeBrushEditor.New(document);
            editor.Hide();
            paintPanel.AddChild(editor);
            document.Add(editor);
        }).AddTo(this);


        AppDocumentManager.LoadedDocuments.ObserveRemove().Select(et => et.Value).Subscribe(document =>
        {
            // View, overlay... are the children of paint panel

            // Paint panel
            var paintPanelContainer = GetTree().GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().Single();
            paintPanelContainer.RemoveFreePaintPanel(document);

            // Layer tree control
            var layerPanel = GetTree().GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().Single();
            layerPanel.RemoveFree(document);
        }).AddTo(this);
    }
}