using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using Godot;
using ObservableCollections;
using R3;

namespace Ciallo.NodeControl;

public partial class ToolPropertyPanel : Container
{
    public VBoxContainer PropertyHolder;

    partial class DocumentToolPropertyContainer : VBoxContainer;

    public override void _Ready()
    {
        PropertyHolder = GetNode<VBoxContainer>("%PropertiesHolder");
        PropertyHolder.QueueFreeChildren();

        AppWorldManager.LoadedWorlds.ObserveAdd().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var holder = new DocumentToolPropertyContainer()
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            holder.VisibleIf(AppWorldManager.WorkingDocument, document);
            document.Add(holder);
            PropertyHolder.AddChild(holder);

            var toolManager = document.Get<ToolButtonPanel>();
            foreach (var tool in toolManager.GetAllTools<ToolButtonBase>())
            {
                var container = new PropertyContainer();
                container.VisibleIf(toolManager.ActiveTool, tool);

                container.QueueFreeChildren();
                tool.DrawProperty(container);

                PropertyHolder.AddChild(container);
            }
        }).AddTo(this);

        AppWorldManager.LoadedWorlds.ObserveRemove().Select(et => et.Value.Document()).Subscribe(document =>
        {
            var box = document.Get<DocumentToolPropertyContainer>();
            document.Remove<DocumentToolPropertyContainer>();
            box.QueueFree();
        }).AddTo(this);
    }
}