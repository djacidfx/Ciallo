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
    private LayerBlock _mouseHoveringLayer;

    public override void _Ready()
    {
        // Free previews in the Godot editor.
        RootContainer.QueueFreeChildren();
        DropHinter.MouseFilter = MouseFilterEnum.Ignore;

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

    private enum DropKind { Silent, FolderChild, Sibling }

    // HoverBlock  : visual block being hovered over
    // DstE        : FolderChild → folder entity; Sibling → parent entity of insertion point
    // DstIdx      : post-removal index passed to MoveLayer
    // InsertAbove : Sibling only — true = line drawn at top edge of hoverBlock in screen space
    private readonly record struct DropTarget(
        DropKind Kind,
        LayerBlock HoverBlock,
        Entity DstE,
        int DstIdx,
        bool InsertAbove = false);

    /// <summary>
    /// Classify the current drag operation against <paramref name="srcLayer"/>.
    /// Returns <see cref="DropKind.Silent"/> when the move should be ignored.
    /// </summary>
    private DropTarget ClassifyDrop(LayerBlock srcLayer)
    {
        if (_mouseHoveringLayer == null || ReferenceEquals(_mouseHoveringLayer, srcLayer))
            return new(DropKind.Silent, null, default, -1);

        var srcE = srcLayer.LayerEntity;
        var hoverBlock = _mouseHoveringLayer;
        var hoverE = hoverBlock.LayerEntity;
        var hoverNode = hoverE.Get<LayerTreeNode>();
        var locPos = hoverBlock.GetLocalMousePosition();
        var size = hoverBlock.Size;

        // Guard: silently ignore if hoverE is srcE itself or a descendant of srcE
        var cursor = hoverE;
        while (!cursor.IsNull)
        {
            if (cursor == srcE) return new(DropKind.Silent, null, default, -1);
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        // Folder child placement: lower 2/3 of the folder block
        if (hoverBlock.IsFolder && locPos.Y > size.Y / 3f)
        {
            int dstIdx = hoverNode.Children.Count;
            // Post-removal adjustment: srcE already a direct child → MoveChild needs dstIdx < Count
            var srcNode = srcE.Get<LayerTreeNode>();
            if (srcNode.ParentValue == hoverE && srcNode.Index < dstIdx)
                dstIdx--;
            return new(DropKind.FolderChild, hoverBlock, hoverE, dstIdx);
        }

        // Sibling placement
        // Layers shown in reversed order: upper half of block = higher index = visually above
        var dstParentE = hoverNode.ParentValue;
        int hoverIdx = hoverNode.Index;
        bool insertAbove = hoverBlock.IsFolder || locPos.Y <= size.Y / 2f;
        int desiredFinalIdx = insertAbove ? hoverIdx + 1 : hoverIdx;

        int siblingDstIdx = desiredFinalIdx;
        var srcNodeSibling = srcE.Get<LayerTreeNode>();
        if (srcNodeSibling.ParentValue == dstParentE && srcNodeSibling.Index < desiredFinalIdx)
            siblingDstIdx--;

        return new(DropKind.Sibling, hoverBlock, dstParentE, siblingDstIdx, insertAbove);
    }

    private void OnDragStart(LayerBlock srcLayer, InputEventMouseMotion motion)
    {
        DragLabel.Text = srcLayer.LayerEntity.Get<CommonLayerSetting>().Name.Value;
        DragLabel.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);
        DragLabel.Visible = true;
    }

    private void OnDragging(LayerBlock srcLayer, InputEventMouseMotion motion)
    {
        DragLabel.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);

        var drop = ClassifyDrop(srcLayer);

        if (drop.Kind == DropKind.Silent)
        {
            DropHinter.Visible = false;
            return;
        }

        if (drop.Kind == DropKind.FolderChild)
        {
            DropHinter.GlobalPosition = drop.HoverBlock.GlobalPosition;
            DropHinter.Size = drop.HoverBlock.Size;
            DropHinter.Visible = true;
            return;
        }

        // Sibling: horizontal line at the insertion edge, respecting indent
        var hoverBlock = drop.HoverBlock;
        float lineGlobalY = drop.InsertAbove
            ? hoverBlock.GlobalPosition.Y
            : hoverBlock.GlobalPosition.Y + hoverBlock.Size.Y;
        var indent = hoverBlock.Indent;
        float indentOffset = indent.Count * indent.Width;
        DropHinter.GlobalPosition = new Vector2(
            hoverBlock.GlobalPosition.X + indentOffset,
            lineGlobalY - DropHinter.Width / 2f);
        DropHinter.Size = new Vector2(hoverBlock.Size.X - indentOffset, DropHinter.Width);
        DropHinter.Visible = true;
    }

    private void OnDragEnd(LayerBlock srcLayer, InputEventMouseButton button)
    {
        DropHinter.Visible = false;
        DragLabel.Visible = false;

        var drop = ClassifyDrop(srcLayer);
        _mouseHoveringLayer = null;

        if (drop.Kind == DropKind.Silent) return;

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var srcE = srcLayer.LayerEntity;
        new CommandBuilder(document).MoveLayer(srcE, drop.DstE, drop.DstIdx).Commit();
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