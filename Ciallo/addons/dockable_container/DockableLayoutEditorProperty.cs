#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Ciallo.Widget;

public partial class DockableLayoutEditorProperty : EditorProperty, ISerializationListener
{
    private DockableContainer _container;
    private MenuButton _hiddenMenuButton;
    private PopupMenu _hiddenMenuPopup;
    private string[] _hiddenMenuList = [];
    private DockableLayout _previewLayout;
    private bool _updating;

    public DockableLayoutEditorProperty()
    {
        CustomMinimumSize = new Vector2(128, 256);
        if (GetChildCount(true) == 0)
            EnsureEditorControls();
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        EnsureEditorControls();
        if (_previewLayout != null)
            DockableSignalConnection.EnsureConnected(
                _previewLayout,
                Resource.SignalName.Changed,
                new Callable(this, MethodName.OnPreviewLayoutChanged)
            );
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        EnsureEditorControls();
        if (_previewLayout != null)
            DockableSignalConnection.EnsureConnected(
                _previewLayout,
                Resource.SignalName.Changed,
                new Callable(this, MethodName.OnPreviewLayoutChanged)
            );
    }

    public override void _ExitTree()
    {
        UnbindPopupSignals();
        if (_previewLayout != null)
            DisconnectPreviewLayoutChanged();
        base._ExitTree();
    }

    public override void _UpdateProperty()
    {
        if (_updating) return;

        _updating = true;

        if (_previewLayout != null)
            DisconnectPreviewLayoutChanged();

        // Keep inspector drag operations isolated until EmitChanged hands the new resource to Godot.
        _previewLayout = GetEditedLayout().Clone();
        RebuildPreviewControls();
        _container.Layout = _previewLayout;
        DockableSignalConnection.EnsureConnected(
            _previewLayout,
            Resource.SignalName.Changed,
            new Callable(this, MethodName.OnPreviewLayoutChanged)
        );

        _updating = false;
    }

    private DockableLayout GetEditedLayout() => (DockableLayout)GetEditedObject().Get(GetEditedProperty());

    private void RebuildPreviewControls()
    {
        var editedContainer = (DockableContainer)GetEditedObject();
        foreach (var control in _container.GetTabs())
        {
            _container.RemoveChild(control);
            control.QueueFree();
        }

        foreach (string tabName in _previewLayout.GetNames())
        {
            var source = editedContainer.GetNode<Control>(tabName);
            var child = new Label
            {
                Name = tabName,
                Text = source.HasMeta(DockableContainer.TitleMetadata)
                    ? source.GetMeta(DockableContainer.TitleMetadata).AsString()
                    : tabName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ClipText = true,
            };
            CopyMetadata(source, child, DockableContainer.TitleMetadata);
            CopyMetadata(source, child, DockableContainer.ExclusiveMetadata);
            _container.AddChild(child);
        }
    }

    private void OnPreviewLayoutChanged()
    {
        if (_updating) return;
        EmitChanged(GetEditedProperty(), _previewLayout);
    }

    private void DisconnectPreviewLayoutChanged()
    {
        DockableSignalConnection.Disconnect(
            _previewLayout,
            Resource.SignalName.Changed,
            new Callable(this, MethodName.OnPreviewLayoutChanged)
        );
    }

    private void EnsureEditorControls()
    {
        // A C# hard reload preserves native children but clears these managed references.
        foreach (Node child in GetChildren(true))
        {
            _hiddenMenuButton ??= child as MenuButton;
            _container ??= child as DockableContainer;
        }

        if (_hiddenMenuButton == null)
        {
            _hiddenMenuButton = new MenuButton();
            AddChild(_hiddenMenuButton);
            AddFocusable(_hiddenMenuButton);
        }
        _hiddenMenuButton.Name = "_visible_nodes";
        _hiddenMenuButton.Text = "Visible nodes";

        if (_container == null)
        {
            _container = new DockableContainer();
            AddChild(_container);
        }
        _container.Name = "_layout_preview";
        _container.CloneLayoutOnReady = false;
        _container.CustomMinimumSize = new Vector2(128, 256);
        SetBottomEditor(_container);

        _hiddenMenuPopup = _hiddenMenuButton.GetPopup();
        _hiddenMenuPopup.HideOnCheckableItemSelection = false;
        BindPopupSignals();
    }

    private void BindPopupSignals()
    {
        DockableSignalConnection.EnsureConnected(
            _hiddenMenuPopup,
            Popup.SignalName.AboutToPopup,
            new Callable(this, MethodName.OnHiddenMenuPopupAboutToShow)
        );
        DockableSignalConnection.EnsureConnected(
            _hiddenMenuPopup,
            PopupMenu.SignalName.IdPressed,
            new Callable(this, MethodName.OnHiddenMenuIdPressed)
        );
    }

    private void UnbindPopupSignals()
    {
        DockableSignalConnection.Disconnect(
            _hiddenMenuPopup,
            Popup.SignalName.AboutToPopup,
            new Callable(this, MethodName.OnHiddenMenuPopupAboutToShow)
        );
        DockableSignalConnection.Disconnect(
            _hiddenMenuPopup,
            PopupMenu.SignalName.IdPressed,
            new Callable(this, MethodName.OnHiddenMenuIdPressed)
        );
    }

    private void OnHiddenMenuPopupAboutToShow()
    {
        _hiddenMenuPopup.Clear();
        var visibleMenuNames = new List<string>();
        foreach (string tabName in _previewLayout.GetNames())
        {
            if (!IsExclusive(_container.GetNode<Control>(tabName)))
                visibleMenuNames.Add(tabName);
        }
        _hiddenMenuList = visibleMenuNames.ToArray();
        for (int i = 0; i < _hiddenMenuList.Length; i++)
        {
            string tabName = _hiddenMenuList[i];
            _hiddenMenuPopup.AddCheckItem(tabName, i);
            _hiddenMenuPopup.SetItemChecked(i, !_previewLayout.IsTabHidden(tabName));
        }
    }

    private void OnHiddenMenuIdPressed(long id)
    {
        string tabName = _hiddenMenuList[id];
        bool newHidden = !_previewLayout.IsTabHidden(tabName);
        _previewLayout.SetTabHidden(tabName, newHidden);
        _hiddenMenuPopup.SetItemChecked((int)id, !newHidden);
    }

    private static void CopyMetadata(Control source, Control target, string name)
    {
        if (source.HasMeta(name))
            target.SetMeta(name, source.GetMeta(name));
    }

    private static bool IsExclusive(Control control) =>
        control.HasMeta(DockableContainer.ExclusiveMetadata)
        && control.GetMeta(DockableContainer.ExclusiveMetadata).AsBool();
}
#endif
