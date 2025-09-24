using Godot;

namespace Ciallo.NodeControl;

public partial class BrushPanel : AcceptDialog
{
    public ItemList BrushSelector;
    public Container PropertiesHolder;
    public Button Add;
    public Button Remove;
    public Button Copy;
    public Button Reset;
    public Button Top;
    public Button Up;
    public Button Down;
    public Button Bottom;

    public override void _EnterTree()
    {
        GetOkButton().Visible = false;
        BrushSelector = GetNode<ItemList>("%BrushSelector");
        PropertiesHolder = GetNode<Container>("%PropertiesHolder");
        Add = GetNode<Button>("%Add");
        Remove = GetNode<Button>("%Remove");
        Copy = GetNode<Button>("%Copy");
        Reset = GetNode<Button>("%Reset");
        Top = GetNode<Button>("%Top");
        Up = GetNode<Button>("%Up");
        Down = GetNode<Button>("%Down");
        Bottom = GetNode<Button>("%Bottom");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey)
        {
            GetParent().GetViewport().PushInput(@event);
        }
    }
}
