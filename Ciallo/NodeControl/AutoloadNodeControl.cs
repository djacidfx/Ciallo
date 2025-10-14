using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class AutoloadNodeControl : Node
{
    public override void _Ready()
    {
        AppWorldManager.LoadedWorlds.ObserveAdd().Subscribe(et =>
        {
            var document = et.Value.Document();
            
            // Layer tree control
            var layerPanel = GetTree().GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().Single();
            layerPanel.CreateAddLayerContainer(document);
            
            // Paint panel
            var paintPanelContainer = GetTree().GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().Single();
            var paintPanel = paintPanelContainer.CreateAddPaintPanel(document);
            
            // World view
            var worldView = paintPanel.GetNode<WorldView>("%WorldView");
            document.Set(worldView);
            
            // World overlay
            var worldOverlay = paintPanel.GetNode<WorldOverlay>("%WorldOverlay");
            document.Set(worldOverlay);
            
            // Document brush editor
            var panel = BrushPanel.Instantiate();
            panel.Title = "Brush in document";
            panel.Visible = false;
            panel.PopupWindow = true; // Hint user this is different from the brush library panel
            panel.Exclusive = false; // Allow propagating input (redo/undo mainly) to main window
            document.Set(panel);
            ((SceneTree)Engine.GetMainLoop()).GetCurrentScene().AddChild(panel);
            // Hide controls for being lazy
            panel.BrushPreviewContainer.Visible = false; 
            panel.Operators.Visible = false;
            // Bind to document brush settings
            var bm = document.Get<BrushManager>();
            panel.BindBrushSetting(bm.Brushes, e => e.Get<BrushSetting>());
            
            // World button manager
            var worldButtonManager = paintPanel.GetNode<WorldButtonManager>("%WorldButtonManager");
            document.Set(worldButtonManager);
        }).AddTo(this);
        
        AppWorldManager.LoadedWorlds.ObserveRemove().Subscribe(et =>
        {
            var document = et.Value.Document();
            // Document brush editor
            document.Get<BrushPanel>().QueueFree();
            document.Remove<BrushPanel>();
            
            // View and overlay are contained in paint panel
            
            // Paint panel
            var paintPanelContainer = GetTree().GetNodesInGroup("UncategorizedControl").OfType<PaintPanelContainer>().Single();
            paintPanelContainer.RemoveFreePaintPanel(document);
            
            // Layer tree control
            var layerPanel = GetTree().GetNodesInGroup("UncategorizedControl").OfType<LayerPanel>().Single();
            layerPanel.RemoveFreeLayerContainer(document);
        }).AddTo(this);
    }
}