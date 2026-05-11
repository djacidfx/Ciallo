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
    private LayerBlock _hoveredBlock;
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
            _hoveredBlock = block;
        };
        block.MouseExited += () =>
        {
            if (ReferenceEquals(_hoveredBlock, block))
                _hoveredBlock = null;
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

    private enum DropKind { None, FolderChild, Sibling }

    // ParentEntity : FolderChild → the folder (or document root) to insert into; Sibling → the shared parent
    // InsertIndex  : raw (pre-removal) insertion index; post-removal adjustment is done in MoveLayerCmd
    private readonly record struct DropTarget(
        DropKind Kind,
        Entity ParentEntity,
        int InsertIndex);

    /// <summary>
    /// Classify the current drag operation against <paramref name="draggedBlock"/>.
    /// Returns <see cref="DropKind.None"/> when the move should be ignored.
    /// Returns a pre-removal <see cref="DropTarget.InsertIndex"/>.
    /// </summary>
    private DropTarget ClassifyDrop(LayerBlock draggedBlock)
    {
        if (_hoveredBlock == null)
        {
            // Mouse inside the container but not over any block → child 0 of document root (visual bottom)
            if (this.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
            {
                var docE = AppDocumentManager.WorkingDocument.CurrentValue;
                return new(DropKind.FolderChild, docE, 0);
            }
            return new(DropKind.None, default, -1);
        }

        if (ReferenceEquals(_hoveredBlock, draggedBlock))
            return new(DropKind.None, default, -1);

        var draggedEntity = draggedBlock.LayerEntity;
        var hoverBlock = _hoveredBlock;
        var hoverEntity = hoverBlock.LayerEntity;
        var hoverTreeNode = hoverEntity.Get<LayerTreeNode>();
        var localPos = hoverBlock.GetLocalMousePosition();
        var size = hoverBlock.Size;

        // Guard: silently ignore if hoverEntity is draggedEntity itself or a descendant of draggedEntity
        var cursor = hoverEntity;
        while (!cursor.IsNull)
        {
            if (cursor == draggedEntity) return new(DropKind.None, default, -1);
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        // Folder child placement: lower 2/3 of the folder block
        if (hoverBlock.IsFolder && localPos.Y > size.Y / 3f)
            return new(DropKind.FolderChild, hoverEntity, hoverTreeNode.Children.Count);

        // Sibling placement — store raw insertIndex; post-removal adjustment is in MoveLayerCmd
        // Layers shown in reversed order: upper half of block = higher index = visually above
        var parentEntity = hoverTreeNode.ParentValue;
        int hoverIndex = hoverTreeNode.Index;
        int insertIndex = (hoverBlock.IsFolder || localPos.Y <= size.Y / 2f) ? hoverIndex + 1 : hoverIndex;

        return new(DropKind.Sibling, parentEntity, insertIndex);
    }

    private void OnDragStart(LayerBlock draggedBlock, InputEventMouseMotion motion)
    {
        _scrollAccum = 0f;
        DragLabel.Text = draggedBlock.LayerEntity.Get<CommonLayerSetting>().Name.Value;
        DragLabel.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);
        DragLabel.Visible = true;
    }

    private void OnDragging(LayerBlock draggedBlock, InputEventMouseMotion motion)
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

        var dropTarget = ClassifyDrop(draggedBlock);

        if (dropTarget.Kind == DropKind.None)
        {
            DropHinter.Visible = false;
            return;
        }

        if (dropTarget.Kind == DropKind.FolderChild)
        {
            if (!dropTarget.ParentEntity.IsDocument)
            {
                // Border framing the LabelLineEdit of the target folder block
                var labelLineEdit = dropTarget.ParentEntity.Get<LayerBlock>().LabelLineEdit;
                DropHinter.GlobalPosition = labelLineEdit.GlobalPosition;
                DropHinter.Size = labelLineEdit.Size;
            }
            else
            {
                // Root: line at the bottom edge, starting at DropdownArrow X of the bottommost child
                var refBlock = dropTarget.ParentEntity.Get<LayerTreeNode>().Children[0].Get<LayerBlock>();
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
            var parentChildren = dropTarget.ParentEntity.Get<LayerTreeNode>().Children;
            int insertIndex = dropTarget.InsertIndex;

            // InsertIndex < Count → line at the bottom of Children[insertIndex] (the item being pushed down)
            // InsertIndex == Count → line at the top of the topmost child (insert above all)
            LayerBlock refBlock;
            float lineGlobalY;
            if (insertIndex < parentChildren.Count)
            {
                refBlock = parentChildren[insertIndex].Get<LayerBlock>();
                lineGlobalY = refBlock.GlobalPosition.Y + refBlock.Size.Y;
            }
            else
            {
                refBlock = parentChildren[^1].Get<LayerBlock>();
                lineGlobalY = refBlock.GlobalPosition.Y;
            }

            // X start: LabelLineEdit of the parent folder; DropdownArrow of refBlock for document root
            float startX = !dropTarget.ParentEntity.IsDocument
                ? dropTarget.ParentEntity.Get<LayerBlock>().LabelLineEdit.GlobalPosition.X
                : refBlock.DropdownArrow.GlobalPosition.X;
            DropHinter.GlobalPosition = new Vector2(startX, lineGlobalY - DropHinter.Width / 2f);
            DropHinter.Size = new Vector2(refBlock.GlobalPosition.X + refBlock.Size.X - startX, DropHinter.Width);
            DropHinter.Visible = true;
        }
    }

    private void OnDragEnd(LayerBlock draggedBlock, InputEventMouseButton button)
    {
        DropHinter.Visible = false;
        DragLabel.Visible = false;
        _scrollSpeed = 0f;
        _scrollAccum = 0f;

        var dropTarget = ClassifyDrop(draggedBlock);
        _hoveredBlock = null;

        if (dropTarget.Kind == DropKind.None) return;

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var draggedEntity = draggedBlock.LayerEntity;

        // Convert raw InsertIndex to post-removal index expected by MoveLayer
        int insertIndex = dropTarget.InsertIndex;
        var draggedTreeNode = draggedEntity.Get<LayerTreeNode>();
        if (draggedTreeNode.ParentValue == dropTarget.ParentEntity && draggedTreeNode.Index < insertIndex)
            insertIndex--;

        new CommandBuilder(document).MoveLayer(draggedEntity, dropTarget.ParentEntity, insertIndex).Commit();
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