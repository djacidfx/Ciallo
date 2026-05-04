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
/// - Root is a "implicit folder"
/// - Godot's nodes hierarchy is entirely identical to layer Entity's LayerTreeNode Component hierarchy.
/// Prefer use Godot's node hierarchy to get index here. It is cached and O(1) operation.
/// </remarks>
[SceneTree(root: "Root"), Instantiable]
public partial class LayerContainer : ScrollContainer
{
    private readonly ButtonGroup _workingLayerButtonGroup = new();

    private bool _isDragging = false;
    private LayerBlock _mouseHoveringLayer;
    private float _scrollSpeed = 0f;
    private float _scrollAccum = 0f;

    private const float ScrollZone = 50f; // px from edge that triggers scroll
    private const float MaxScrollSpeed = 280f; // px per second at full speed

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

    public override void _Process(double delta)
    {
        if (!_isDragging || _scrollSpeed == 0f) return;
        _scrollAccum += _scrollSpeed * (float)delta;
        int step = (int)_scrollAccum;
        if (step != 0)
        {
            ScrollVertical += step;
            _scrollAccum -= step;
        }
    }

    public void Create(Entity layerE)
    {
        var layerBlock = CreateBlock(layerE);
        layerE.AddNode(layerBlock);
        if (layerE.Has<FolderLayerSetting>())
        {
            var dropdownButton = layerBlock.DropdownArrow;
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
        else
        {
            layerBlock.DropdownArrow.Visible = false;
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

    // DstE    : FolderChild → folder entity (or document root); Sibling → parent entity
    // DstIdx  : raw (pre-removal) insertion index; post-removal adjustment is done in MoveLayerCmd
    private readonly record struct DropTarget(
        DropKind Kind,
        Entity DstE,
        int DstIdx);

    /// <summary>
    /// Classify the current drag operation against <paramref name="srcLayer"/>.
    /// Returns <see cref="DropKind.Silent"/> when the move should be ignored.
    /// Return pre-removal index
    /// </summary>
    private DropTarget ClassifyDrop(LayerBlock srcLayer)
    {
        if (_mouseHoveringLayer == null)
        {
            // Mouse inside the container but not over any block → child 0 of document root (visual bottom)
            if (this.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
            {
                var docE = AppDocumentManager.WorkingDocument.CurrentValue;
                return new(DropKind.FolderChild, docE, 0);
            }
            return new(DropKind.Silent, default, -1);
        }

        if (ReferenceEquals(_mouseHoveringLayer, srcLayer))
            return new(DropKind.Silent, default, -1);

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
            if (cursor == srcE) return new(DropKind.Silent, default, -1);
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        // Folder child placement: lower 2/3 of the folder block
        if (hoverBlock.IsFolder && locPos.Y > size.Y / 3f)
            return new(DropKind.FolderChild, hoverE, hoverNode.Children.Count);

        // Sibling placement — store raw desiredFinalIdx; post-removal adjustment is in MoveLayerCmd
        // Layers shown in reversed order: upper half of block = higher index = visually above
        var dstParentE = hoverNode.ParentValue;
        int hoverIdx = hoverNode.Index;
        int desiredFinalIdx = (hoverBlock.IsFolder || locPos.Y <= size.Y / 2f) ? hoverIdx + 1 : hoverIdx;

        return new(DropKind.Sibling, dstParentE, desiredFinalIdx);
    }

    private void OnDragStart(LayerBlock srcLayer, InputEventMouseMotion motion)
    {
        _scrollAccum = 0f;
        DragLabel.Text = srcLayer.LayerEntity.Get<CommonLayerSetting>().Name.Value;
        DragLabel.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);
        DragLabel.Visible = true;
    }

    private void OnDragging(LayerBlock srcLayer, InputEventMouseMotion motion)
    {
        DragLabel.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);

        var rect = GetGlobalRect();
        float mouseY = motion.GlobalPosition.Y;
        float distFromTop = mouseY - rect.Position.Y;
        float distFromBottom = rect.End.Y - mouseY;
        if (distFromTop < ScrollZone)
            _scrollSpeed = -MaxScrollSpeed * (1f - distFromTop / ScrollZone);
        else if (distFromBottom < ScrollZone)
            _scrollSpeed = MaxScrollSpeed * (1f - distFromBottom / ScrollZone);
        else
            _scrollSpeed = 0f;

        var drop = ClassifyDrop(srcLayer);

        if (drop.Kind == DropKind.Silent)
        {
            DropHinter.Visible = false;
            return;
        }

        if (drop.Kind == DropKind.FolderChild)
        {
            if (!drop.DstE.IsDocument)
            {
                // Border framing the LabelLineEdit of the target folder block
                var labelLineEdit = drop.DstE.Get<LayerBlock>().LabelLineEdit;
                DropHinter.GlobalPosition = labelLineEdit.GlobalPosition;
                DropHinter.Size = labelLineEdit.Size;
            }
            else
            {
                // Root: line at the bottom edge, starting at DropdownArrow X of the bottommost child
                var refBlock = drop.DstE.Get<LayerTreeNode>().Children[0].Get<LayerBlock>();
                float startX = refBlock.DropdownArrow.GlobalPosition.X;
                float lineY = RootContainer.GlobalPosition.Y + RootContainer.Size.Y;
                DropHinter.GlobalPosition = new Vector2(startX, lineY - DropHinter.Width / 2f);
                DropHinter.Size = new Vector2(refBlock.GlobalPosition.X + refBlock.Size.X - startX, DropHinter.Width);
            }
            DropHinter.Visible = true;
            return;
        }

        // Sibling: horizontal line at the insertion boundary
        {
            var dstChildren = drop.DstE.Get<LayerTreeNode>().Children;
            int dstIdx = drop.DstIdx;

            // DstIdx < Count → line at the bottom of Children[dstIdx] (the item being pushed down)
            // DstIdx == Count → line at the top of the topmost child (insert above all)
            LayerBlock refBlock;
            float lineGlobalY;
            if (dstIdx < dstChildren.Count)
            {
                refBlock = dstChildren[dstIdx].Get<LayerBlock>();
                lineGlobalY = refBlock.GlobalPosition.Y + refBlock.Size.Y;
            }
            else
            {
                refBlock = dstChildren[^1].Get<LayerBlock>();
                lineGlobalY = refBlock.GlobalPosition.Y;
            }

            // X start: LabelLineEdit of the parent folder; DropdownArrow of refBlock for document root
            float startX = !drop.DstE.IsDocument
                ? drop.DstE.Get<LayerBlock>().LabelLineEdit.GlobalPosition.X
                : refBlock.DropdownArrow.GlobalPosition.X;
            DropHinter.GlobalPosition = new Vector2(startX, lineGlobalY - DropHinter.Width / 2f);
            DropHinter.Size = new Vector2(refBlock.GlobalPosition.X + refBlock.Size.X - startX, DropHinter.Width);
            DropHinter.Visible = true;
        }
    }

    private void OnDragEnd(LayerBlock srcLayer, InputEventMouseButton button)
    {
        DropHinter.Visible = false;
        DragLabel.Visible = false;
        _scrollSpeed = 0f;
        _scrollAccum = 0f;

        var drop = ClassifyDrop(srcLayer);
        _mouseHoveringLayer = null;

        if (drop.Kind == DropKind.Silent) return;

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var srcE = srcLayer.LayerEntity;

        // Convert raw DstIdx to post-removal index expected by MoveLayer
        int dstIdx = drop.DstIdx;
        var srcNode = srcE.Get<LayerTreeNode>();
        if (srcNode.ParentValue == drop.DstE && srcNode.Index < dstIdx)
            dstIdx--;

        new CommandBuilder(document).MoveLayer(srcE, drop.DstE, dstIdx).Commit();
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