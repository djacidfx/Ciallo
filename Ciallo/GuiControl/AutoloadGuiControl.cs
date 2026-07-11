using System.Linq;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

public partial class AutoloadGuiControl : Node
{
    public override void _Ready()
    {
        AppDocumentManager.WorkingDocument.Pairwise().Subscribe(pair =>
        {
            var previousDocument = pair.Previous;
            var document = pair.Current;
            if (!previousDocument.IsNull)
            {
                // View, overlay... are the children of paint panel
                // Must queue free to avoid subscriptions access disposed nodes.
                // Disable process since panel could potentially get a one-frame mouse movement after closing document.
                previousDocument.Get<PaintPanel>().SetProcessInput(false);
                previousDocument.Get<TimelinePanel>().SetProcessInput(false);
            }

            if (document.IsNull) return;

            // Window title
            document.Get<CommandManager>().DocumentModified
                .CombineLatest(document.Get<DocumentSetting>().Name, (modified, name) => (modified, name))
                .Subscribe(v =>
                {
                    string prepend = v.modified ? "(*)" : "";
                    DisplayServer.WindowSetTitle($"{prepend + v.name} - Ciallo");
                }).AddTo(document);

            // Layer tree panel
            var layerPanel = LayerPanel.New();
            GetTree().GetNodesInGroup("UncategorizedControl")
                .OfType<LayerPanelContainer>().Single().AddChild(layerPanel);
            document.AddNode(layerPanel);

            // Paint panel
            var paintPanel = PaintPanel.New();
            GetTree().GetNodesInGroup("UncategorizedControl")
                .OfType<PaintPanelContainer>().Single().AddChild(paintPanel);
            document.AddNode(paintPanel);

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
            var timelinePanel = TimelinePanel.New(document);
            GetTree().GetNodesInGroup("UncategorizedControl")
                .OfType<TimelinePanelContainer>().Single().AddChild(timelinePanel);
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
    }
}
