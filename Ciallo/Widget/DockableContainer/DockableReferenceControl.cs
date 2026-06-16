using Godot;

namespace Ciallo.Widget.DockableContainer;

[Tool, GlobalClass]
public partial class DockableReferenceControl : Container
{
    private Control _referenceTo;

    public Control ReferenceTo
    {
        get => _referenceTo;
        set
        {
            if (_referenceTo == value) return;

            if (IsInstanceValid(_referenceTo))
            {
                _referenceTo.Renamed -= OnReferenceToRenamed;
                _referenceTo.MinimumSizeChanged -= UpdateMinimumSize;
            }

            _referenceTo = value;
            EmitSignal(SignalName.MinimumSizeChanged);

            if (!IsInstanceValid(_referenceTo)) return;
            _referenceTo.Renamed += OnReferenceToRenamed;
            _referenceTo.MinimumSizeChanged += UpdateMinimumSize;
            _referenceTo.Visible = Visible;
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
            _referenceTo.Visible = Visible;
        else if (what == NotificationTransformChanged && _referenceTo != null)
            RepositionReference();
    }

    public override Vector2 _GetMinimumSize() => _referenceTo?.GetCombinedMinimumSize() ?? Vector2.Zero;

    private void RepositionReference()
    {
        _referenceTo.GlobalPosition = GlobalPosition;
        _referenceTo.Size = Size;
    }

    private void OnReferenceToRenamed()
    {
        Name = _referenceTo.Name;
    }
}
