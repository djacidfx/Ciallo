using Godot;

namespace Ciallo.NodeControl;

public partial class BrushPanel : AcceptDialog
{
    public ItemList BrushSelector;
    public Container PropertiesHolder;

    public override void _EnterTree()
    {
        GetOkButton().Visible = false;
        BrushSelector = GetNode<ItemList>("%BrushSelector");
        // var view = AppBrushLibrary.Brushes.CreateWritableView(setting =>  setting.Name);
        // view.AddTo(this);
        // BrushSelector.BindValue(view, AppBrushLibrary.CurrentBrush);
        
        PropertiesHolder = GetNode<Container>("%PropertiesHolder");

        // foreach (var brush in AppBrushLibrary.Brushes)
        // {
        //     var propertyBox = new PropertyContainer();
        //     brush.DrawProperty(propertyBox);
        //     propertyBox.VisibleIf(AppBrushLibrary.CurrentBrush, brush);
        //     PropertiesHolder.AddChild(propertyBox);
        // }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            GetParent().GetViewport().PushInput(@event);
        }
    }
}
