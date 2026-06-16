using Godot;

namespace Ciallo.Widget.DockableContainer;

[Tool]
public partial class DockableReferenceControl : Container
{
    private Control _referenceTo;

    public Control ReferenceTo
    {
        get => _referenceTo;
        set
        {
            if (_referenceTo == value) return;

            // Reparenting real tabs into TabContainer makes them disappear from DockableContainer's saved children.
            if (IsInstanceValid(_referenceTo))
            {
                var renamed = new Callable(this, MethodName.OnReferenceToRenamed);
                var minimumSizeChanged = new Callable(this, MethodName.OnReferenceToMinimumSizeChanged);
                if (_referenceTo.IsConnected(Node.SignalName.Renamed, renamed))
                    _referenceTo.Disconnect(Node.SignalName.Renamed, renamed);
                if (_referenceTo.IsConnected(Control.SignalName.MinimumSizeChanged, minimumSizeChanged))
                    _referenceTo.Disconnect(Control.SignalName.MinimumSizeChanged, minimumSizeChanged);
            }

            _referenceTo = value;
            EmitSignal(SignalName.MinimumSizeChanged);

            if (!IsInstanceValid(_referenceTo)) return;
            // _referenceTo.Renamed += ... can reload as: "delegate_handle.value is null".
            _referenceTo.Connect(Node.SignalName.Renamed, new Callable(this, MethodName.OnReferenceToRenamed));
            _referenceTo.Connect(Control.SignalName.MinimumSizeChanged, new Callable(this, MethodName.OnReferenceToMinimumSizeChanged));
            SetVisibleIfChanged(_referenceTo, Visible);
            RepositionReference();
            OnReferenceToRenamed();
        }
    }

    public override void _Ready()
    {
        base._Ready();
        MouseFilter = MouseFilterEnum.Ignore;
        SetNotifyTransform(true);
    }

    public override void _Notification(int what)
    {
        base._Notification(what);
        if (what == NotificationVisibilityChanged && _referenceTo != null)
            SetVisibleIfChanged(_referenceTo, Visible);
        else if (what == NotificationTransformChanged && _referenceTo != null)
            RepositionReference();
    }

    public override Vector2 _GetMinimumSize() => _referenceTo?.GetCombinedMinimumSize() ?? Vector2.Zero;

    private void RepositionReference()
    {
        if (!_referenceTo.GlobalPosition.IsEqualApprox(GlobalPosition))
            _referenceTo.GlobalPosition = GlobalPosition;
        if (!_referenceTo.Size.IsEqualApprox(Size))
            _referenceTo.Size = Size;
    }

    private void OnReferenceToRenamed()
    {
        if (Name == _referenceTo.Name) return;
        Name = _referenceTo.Name;
    }

    private void OnReferenceToMinimumSizeChanged() => UpdateMinimumSize();

    private static void SetVisibleIfChanged(CanvasItem item, bool visible)
    {
        if (item.Visible == visible) return;
        item.Visible = visible;
    }
}
