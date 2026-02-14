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
            layerPanel.CreateAddLayerContainer(document);

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

            // Document brush editor
            var panel = BrushPanel.Instantiate();
            panel.Title = "Brush in document";
            panel.Visible = false;
            panel.PopupWindow = true; // Hint user this is different from the brush library panel
            panel.Exclusive = false; // Allow propagating input (redo/undo mainly) to main window
            document.Add(panel);
            ((SceneTree)Engine.GetMainLoop()).GetCurrentScene().AddChild(panel);
            // Hide controls for being lazy
            panel.BrushPreviewContainer.Visible = false;
            panel.Operators.Visible = false;
            // Bind to document brush settings
            var bm = document.Get<BrushManager>();
            panel.BindBrushSetting(bm.Brushes, e => e.Get<BrushSetting>());
        }).AddTo(this);


        AppDocumentManager.LoadedDocuments.ObserveRemove().Select(et => et.Value).Subscribe(document =>
        {
            // Document brush editor
            document.Get<BrushPanel>().QueueFree();
            document.Remove<BrushPanel>();

            // View, overlay and body are the children of paint panel

            // Paint panel
            var paintPanelContainer = GetTree().GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().Single();
            paintPanelContainer.RemoveFreePaintPanel(document);

            // Layer tree control
            var layerPanel = GetTree().GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().Single();
            layerPanel.RemoveFreeLayerContainer(document);
        }).AddTo(this);
    }
}