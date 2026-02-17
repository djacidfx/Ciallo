using Ciallo.Data;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.GuiControl;

public partial class ToolPropertyPanel : Container
{
    public VBoxContainer PropertyHolder;

    partial class DocumentToolPropertyContainer : VBoxContainer;

    public override void _Ready()
    {
        PropertyHolder = GetNode<VBoxContainer>("%PropertiesHolder");
        PropertyHolder.QueueFreeChildren();

        AppDocumentManager.LoadedDocuments.ObserveAdd().Select(et => et.Value).Subscribe(document =>
        {
            var holder = new DocumentToolPropertyContainer()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            }.VisibleIf(AppDocumentManager.WorkingDocument, document);
            document.Add(holder);
            PropertyHolder.AddChild(holder);

            var toolManager = document.Get<ToolManager>();
            foreach (var tool in toolManager.Tools)
            {
                var container = new PropertyContainer(document);
                container.VisibleIf(toolManager.ActiveTool, tool);
                container.QueueFreeChildren();
                tool.DrawProperty(container);

                PropertyHolder.AddChild(container);
            }
        }).AddTo(this);

        AppDocumentManager.LoadedDocuments.ObserveRemove().Select(et => et.Value).Subscribe(document =>
        {
            document.Get<DocumentToolPropertyContainer>().QueueFree();
            document.Remove<DocumentToolPropertyContainer>();
        }).AddTo(this);
    }
}