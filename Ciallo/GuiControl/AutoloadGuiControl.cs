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
            // Layer tree panel
            var layerPanel = LayerPanel.New();
            GetTree().GetNodesInGroup("UncategorizedControl")
                .OfType<LayerPanelContainer>().Single().AddChild(layerPanel);
            document.AddNode(layerPanel);

            // Paint panel
            var paintPanel = PaintPanel.New();
            GetTree().GetNodesInGroup("UncategorizedControl")
                .OfType<PaintPanelContainer>().Single().AddChild(paintPanel);
            document.Add(paintPanel);

            var worldView = paintPanel.GetNode<WorldView>("%WorldView");
            document.Add(worldView);
            document.Add<FolderLayerView>(worldView); // Add component as FolderLayerView

            var worldOverlay = paintPanel.GetNode<WorldOverlay>("%WorldOverlay");
            document.Add(worldOverlay);
            document.Add<OverlayHolder>(worldOverlay);

            var worldBody = paintPanel.GetNode<WorldBody>("%WorldBody");
            document.Add(worldBody);
            document.Add<BodyHolder>(worldBody);

            // Timeline panel
            var timelinePanel = TimelinePanel.New();
            GetTree().GetNodesInGroup("UncategorizedControl")
                .OfType<TimelinePanelContainer>().Single().AddChild(timelinePanel);
            timelinePanel
                .BindTimeline(document.Get<TimelineSetting>())
                .BindPlayhead(document.Get<SelectionManager>().CurrentFrame);
            document.AddNode(timelinePanel);

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
            // Must free instantly not queue free. Otherwise, panel could potentially get a one-frame mouse movement after closing document.
            document.Get<PaintPanel>().Free();
        }).AddTo(this);
    }
}