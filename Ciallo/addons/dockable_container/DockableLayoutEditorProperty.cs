#if TOOLS
using System.Collections.Generic;
using Godot;

namespace Ciallo.Widget.DockableContainer;

public partial class DockableLayoutEditorProperty : EditorProperty
{
    private readonly DockableContainer _container = new()
    {
        CloneLayoutOnReady = false,
        CustomMinimumSize = new Vector2(128, 256),
    };

    private readonly MenuButton _hiddenMenuButton = new()
    {
        Text = "Visible nodes",
    };

    private readonly List<Control> _previewControls = [];
    private PopupMenu _hiddenMenuPopup;
    private string[] _hiddenMenuList = [];
    private DockableLayout _previewLayout;
    private bool _updating;
    private bool _ignoreNextPreviewLayoutChanged;

    public DockableLayoutEditorProperty()
    {
        CustomMinimumSize = new Vector2(128, 256);

        AddChild(_hiddenMenuButton);
        AddFocusable(_hiddenMenuButton);

        _hiddenMenuPopup = _hiddenMenuButton.GetPopup();
        _hiddenMenuPopup.HideOnCheckableItemSelection = false;
        _hiddenMenuPopup.Connect(Popup.SignalName.AboutToPopup, new Callable(this, MethodName.OnHiddenMenuPopupAboutToShow));
        _hiddenMenuPopup.Connect(PopupMenu.SignalName.IdPressed, new Callable(this, MethodName.OnHiddenMenuIdPressed));

        AddChild(_container);
        SetBottomEditor(_container);
    }

    public override void _ExitTree()
    {
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

        _previewLayout = GetEditedLayout().Clone();
        RebuildPreviewControls();
        _ignoreNextPreviewLayoutChanged = true;
        _container.Layout = _previewLayout;
        _previewLayout.Connect(Resource.SignalName.Changed, new Callable(this, MethodName.OnPreviewLayoutChanged));

        _updating = false;
    }

    private DockableLayout GetEditedLayout() => (DockableLayout)GetEditedObject().Get(GetEditedProperty());

    private void RebuildPreviewControls()
    {
        foreach (var control in _previewControls)
        {
            if (control.GetParent() == _container)
                _container.RemoveChild(control);
            control.QueueFree();
        }
        _previewControls.Clear();

        foreach (string tabName in _previewLayout.GetNames())
        {
            var child = new Label
            {
                Name = tabName,
                Text = tabName,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ClipText = true,
            };
            _previewControls.Add(child);
            _container.AddChild(child);
        }
    }

    private void OnPreviewLayoutChanged()
    {
        if (_updating || _ignoreNextPreviewLayoutChanged)
        {
            _ignoreNextPreviewLayoutChanged = false;
            return;
        }
        EmitChanged(GetEditedProperty(), _previewLayout);
    }

    private void DisconnectPreviewLayoutChanged()
    {
        var callable = new Callable(this, MethodName.OnPreviewLayoutChanged);
        if (_previewLayout.IsConnected(Resource.SignalName.Changed, callable))
            _previewLayout.Disconnect(Resource.SignalName.Changed, callable);
    }

    private void OnHiddenMenuPopupAboutToShow()
    {
        _hiddenMenuPopup.Clear();
        _hiddenMenuList = _previewLayout.GetNames();
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
}
#endif
