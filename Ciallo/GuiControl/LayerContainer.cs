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
/// <remarks>
/// Design of node hierarchy:
/// Godot's nodes hierarchy is entirely identical to layer Entity's LayerTreeNode Component hierarchy.
/// Prefer use Godot's node hierarchy to get index here. It is cached and O(1) operation.
/// </remarks>
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

        block.MouseEntered += () =>
        {
            if (!ReferenceEquals(_mouseHoveringLayer, block))
                _mouseHoveringLayer = block;
        };
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
        // Drag hint cleanup
        if (_visibleDropHintLine != null) _visibleDropHintLine.Visible = false;
        _visibleDropHintLine = null;

        if (_mouseHoveringLayer == null || ReferenceEquals(_mouseHoveringLayer, srcLayer))
        {
            _mouseHoveringLayer = null;
            return;
        }

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var srcE = srcLayer.LayerEntity;
        var hoverBlock = _mouseHoveringLayer;
        _mouseHoveringLayer = null;

        var hoverE = hoverBlock.LayerEntity;
        var hoverNode = hoverE.Get<LayerTreeNode>();
        var locPos = hoverBlock.GetLocalMousePosition();
        var size = hoverBlock.Size;

        // Guard: silently ignore if hoverE is srcE itself or a descendant of srcE
        var cursor = hoverE;
        while (!cursor.IsNull)
        {
            if (cursor == srcE) return;
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        // Folder's child placement
        // lower 2/3 → insert as last child (visual top)
        if (hoverBlock.IsFolder && locPos.Y > size.Y / 3f)
        {
            new CommandBuilder(document).MoveLayer(srcE, hoverE, hoverNode.Children.Count).Commit();
            return;
        }

        // Sibling placement
        // Layers shown in reversed order: upper part of block = higher index = above in screen
        // Non-folder: upper half → above, lower half → below
        // Folder top 1/3: always insert above (top of block is closest to what's above in screen)
        var dstParentE = hoverNode.ParentValue;
        int hoverIdx = hoverNode.Index;
        bool insertAbove = hoverBlock.IsFolder || locPos.Y <= size.Y / 2f;
        int desiredFinalIdx = insertAbove ? hoverIdx + 1 : hoverIdx;

        // Convert to post-removal coordinates when src and dst share the same parent
        int dstIdx = desiredFinalIdx;
        var srcNode = srcE.Get<LayerTreeNode>();
        if (srcNode.ParentValue == dstParentE && srcNode.Index < desiredFinalIdx)
            dstIdx--;

        new CommandBuilder(document)
            .MoveLayer(srcE, dstParentE, dstIdx)
            .Commit();
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