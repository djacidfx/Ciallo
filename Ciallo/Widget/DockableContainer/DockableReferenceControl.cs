using Godot;

namespace Ciallo.Widget;

[Tool]
public partial class DockableReferenceControl : Container, ISerializationListener
{
    // TabContainer needs child controls, but managed panels must remain children of DockableContainer.
    // This proxy mirrors a panel's geometry, visibility, and minimum size without reparenting it.
    public Control ReferenceTo
    {
        get;
        set
        {
            var minimumSizeChanged = new Callable(this, MethodName.OnReferenceToMinimumSizeChanged);
            if (field == value)
            {
                // Reload deserialization can assign the same native object after connections are restored.
                DisconnectLegacyRenameSignal(value);
                DockableSignalConnection.EnsureConnected(value, Control.SignalName.MinimumSizeChanged, minimumSizeChanged);
                return;
            }

            Control previousReference = field;
            field = value;
            DisconnectLegacyRenameSignal(previousReference);
            DisconnectLegacyRenameSignal(field);
            DockableSignalConnection.Rebind(
                previousReference,
                field,
                Control.SignalName.MinimumSizeChanged,
                minimumSizeChanged
            );
            UpdateMinimumSize();

            if (!IsInstanceValid(field)) return;
            SetVisibleIfChanged(field, Visible);
            RepositionReference();
            Name = field.Name;
        }
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        DisconnectLegacyRenameSignal(ReferenceTo);
        DockableSignalConnection.EnsureConnected(
            ReferenceTo,
            Control.SignalName.MinimumSizeChanged,
            new Callable(this, MethodName.OnReferenceToMinimumSizeChanged)
        );
        UpdateMinimumSize();

        if (!IsInstanceValid(ReferenceTo)) return;
        SetVisibleIfChanged(ReferenceTo, Visible);
        RepositionReference();
        Name = ReferenceTo.Name;
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
        if (what == NotificationVisibilityChanged && ReferenceTo != null)
            SetVisibleIfChanged(ReferenceTo, Visible);
        else if (what == NotificationTransformChanged && ReferenceTo != null)
            RepositionReference();
    }

    public override Vector2 _GetMinimumSize() => ReferenceTo?.GetCombinedMinimumSize() ?? Vector2.Zero;

    private void RepositionReference()
    {
        if (!ReferenceTo.GlobalPosition.IsEqualApprox(GlobalPosition))
            ReferenceTo.GlobalPosition = GlobalPosition;
        if (!ReferenceTo.Size.IsEqualApprox(Size))
            ReferenceTo.SetSize(Size);
    }

    private void OnReferenceToMinimumSizeChanged() => UpdateMinimumSize();

    private void DisconnectLegacyRenameSignal(Control source)
    {
        // Renames are now tracked once by DockableContainer through SceneTree.NodeRenamed.
        DockableSignalConnection.Disconnect(
            source,
            Node.SignalName.Renamed,
            new Callable(this, "OnReferenceToRenamed")
        );
    }

    private static void SetVisibleIfChanged(CanvasItem item, bool visible)
    {
        if (item.Visible == visible) return;
        item.Visible = visible;
    }
}
