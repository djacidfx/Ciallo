using System;
using System.Linq;
using Ciallo.Command;
using Ciallo.Data;
using Ciallo.Widget;
using Frent;
using Godot;
using R3;

namespace Ciallo.GuiControl;

/// <summary>
/// Manage the layer UI controls. Also hold layer properties.
/// One instance per document.
/// </summary>
[SceneTree(root: "Root"), Instantiable]
public partial class LayerContainer : Container
{
    private readonly ButtonGroup _workingLayerButtonGroup = new();

    private bool _isDragging = false;
    private Control _visibleDropHintLine;
    private LayerBlock _mouseHoveringLayer;

    public override void _Ready()
    {
        // Free previews in the Godot editor.
        RootContainer.QueueFreeChildren();
        _workingLayerButtonGroup.Pressed += button =>
        {
            var layerBlock = (LayerBlock)button.GetOwner();
            new CommandBuilder(layerBlock.LayerEntity).SetWorkingLayer().Commit();
        };
    }

    public void Create(Entity layerE)
    {
        var layerBlock = CreateBlock(layerE);
        layerE.AddNode(layerBlock);
        if (layerE.Has<FolderLayerSetting>())
        {
            CheckBox dropdownButton = layerBlock.DropdownArrow;
            var isExpandedProperty = layerE.Get<FolderLayerSetting>().IsExpanded;
            dropdownButton.Visible = true;
            dropdownButton.BindBool(isExpandedProperty, out var sub);
            sub.AddTo(layerE);

            var container = new LayerFolderContainer();
            container.Title = layerBlock;
            container.ObserveIsExpanded(isExpandedProperty, out var sub1);
            sub1.AddTo(layerE);
            layerE.AddNode(container);
        }
    }


    private LayerBlock CreateBlock(Entity e)
    {
        var commonSetting = e.Get<CommonLayerSetting>();
        var subs = new CompositeDisposable().AddTo(e);
        var cmdM = e.Document.Get<CommandManager>();

        var block = LayerBlock.New();
        block.WorkingButton.ButtonGroup = _workingLayerButtonGroup;
        block.VisibleButton
            .BindBool(commonSetting.IsVisible, out var sub0);
        var lineEdit = block.GetNode<LabelLineEdit>("%LabelLineEdit")
            .BindString(commonSetting.Name, out var sub1)
            .RegisterUndo(cmdM);
        sub0.AddTo(subs);
        sub1.AddTo(subs);

        block.MouseEntered += () => _mouseHoveringLayer = block;
        block.MouseExited += () =>
        {
            if (ReferenceEquals(_mouseHoveringLayer, block))
                _mouseHoveringLayer = null;
        };

        var guiInput = lineEdit
            .SignalAsObservable<InputEvent>(Control.SignalName.GuiInput)
            .Where(_ => !lineEdit.IsEditing());
        var leftMouse = guiInput
            .OfType<InputEvent, InputEventMouseButton>()
            .Where(button => button.ButtonIndex == MouseButton.Left);

        // Single click without dragging or double click
        var singleClickObs = leftMouse
            .Where(button => button.IsPressed() || button.IsReleased())
            .Chunk(TimeSpan.FromMilliseconds(200))
            .Where(xs => xs.Length == 2 && xs.First().IsPressed() && xs.Last().IsReleased())
            .Select(xs => xs.First());
        singleClickObs.Subscribe(_ => block.WorkingButton.SetPressed(true)).AddTo(subs);

        // Drag
        var mouseState = leftMouse.ToReadOnlyReactiveProperty();
        var dragStart = guiInput
            // The most recent left mouse is clicked and not release.
            .Where(_ => mouseState.CurrentValue?.IsPressed() == true)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left)
            // mouse motion distance is larger than the value in pixels.
            .Where(motion => motion.GlobalPosition.DistanceTo(mouseState.CurrentValue.GlobalPosition) > 20)
            .Where(_ => !_isDragging);
        dragStart.Subscribe(motion =>
        {
            _isDragging = true;
            OnDragStart(block, motion);
        }).AddTo(subs);

        var dragging = guiInput
            .Where(_ => _isDragging)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left);
        dragging.Subscribe(motion => OnDragging(block, motion)).AddTo(subs);

        var dragEnd = leftMouse
            .Where(button => _isDragging && button.IsReleased());
        dragEnd.Subscribe(button =>
        {
            _isDragging = false;
            OnDragEnd(block, button);
        }).AddTo(subs);

        return block;
    }


    private void OnDragStart(LayerBlock srcLayer, InputEventMouseMotion motion) { }

    private void OnDragging(LayerBlock _, InputEventMouseMotion e)
    {
        if (_mouseHoveringLayer == null)
        {
            _visibleDropHintLine?.SetVisible(false);
            _visibleDropHintLine = null;
            return;
        }

        var locPos = _mouseHoveringLayer.GetLocalMousePosition();
        var size = _mouseHoveringLayer.Size;

        var sep = size.Y / 2;
        var hintToShow = _mouseHoveringLayer.GetNode<HSeparator>(locPos.Y < sep ? "%AboveHint" : "%BelowHint");
        if (_visibleDropHintLine == hintToShow) return;
        if (_visibleDropHintLine != null) _visibleDropHintLine.Visible = false;
        hintToShow.Visible = true;
        _visibleDropHintLine = hintToShow;
    }

    private void OnDragEnd(LayerBlock srcLayer, InputEventMouseButton button)
    {
        // Note: Layers is shown in reversed order, so the index logic is inverted.
        // Drag hint
        if (_visibleDropHintLine != null) _visibleDropHintLine.Visible = false;
        _visibleDropHintLine = null;

        // Move layer
        if (_mouseHoveringLayer == null || ReferenceEquals(_mouseHoveringLayer, srcLayer))
        {
            _mouseHoveringLayer = null;
            return;
        }

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var root = document.Get<LayerTreeNode>();
        var srcPath = root.FindPathTo(srcLayer.LayerEntity);
        var dstPath = root.FindPathTo(_mouseHoveringLayer.LayerEntity);

        // Only support same-parent drag
        if (!srcPath.Take(srcPath.Length - 1).SequenceEqual(dstPath.Take(dstPath.Length - 1)))
        {
            _mouseHoveringLayer = null;
            return;
        }

        int srcIdx = srcPath[^1];
        int dstIdx = dstPath[^1];
        if (srcIdx < dstIdx) dstIdx--; // post-removal coordinates

        var locPos = _mouseHoveringLayer.GetLocalMousePosition();
        var size = _mouseHoveringLayer.Size;
        if (locPos.Y <= size.Y / 2) dstIdx++; // insert above hovered layer (reverse order)

        var parentPath = srcPath.Take(srcPath.Length - 1).ToArray();
        var dstFullPath = parentPath.Append(dstIdx).ToArray();

        new CommandBuilder(document)
            .MoveLayer(srcPath, dstFullPath).Commit();
    }

    public void SetWorkingLayerNoSignal(Entity layerE)
    {
        _workingLayerButtonGroup.GetPressedButton()?.SetPressedNoSignal(false);
        if (layerE.IsNull || layerE.IsDocument) return;
        var layerControl = layerE.Get<LayerBlock>();
        var activeButton = layerControl.WorkingButton;
        // Warning note: button group will not be updated by `SetPressedNoSignal`.
        activeButton.SetPressedNoSignal(true);
    }
}