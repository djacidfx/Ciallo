using Godot;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using R3;

namespace Ciallo.NodeControl;

public partial class BrushPanel : AcceptDialog
{
    public ItemList BrushSelector;
    public Container PropertiesHolder;

    public override void _Ready()
    {
        GetOkButton().Visible = false;
        var view = AppBrushLibrary.Brushes.CreateWritableView(setting =>  setting.Name);
        view.AddTo(this);
        BrushSelector = GetNode<ItemList>("%BrushSelector");
        BrushSelector.BindValue(view, AppBrushLibrary.CurrentBrush);
        
        PropertiesHolder = GetNode<Container>("%PropertiesHolder");

        foreach (var brush in AppBrushLibrary.Brushes)
        {
            var propertyBox = new PropertyContainer();
            brush.DrawProperty(propertyBox);
            propertyBox.VisibleIf(AppBrushLibrary.CurrentBrush, brush);
            PropertiesHolder.AddChild(propertyBox);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            GetParent().GetViewport().PushInput(@event);
        }
    }
}
