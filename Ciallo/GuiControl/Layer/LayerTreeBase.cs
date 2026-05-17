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
/// Abstract base for LayerTree and TrackHeaderTree.
/// Shares all drag-drop, block-binding, and scroll logic.
/// Concrete subclasses differ only in the Frent component types they read/write
/// (<see cref="GetWrapper"/>, <see cref="GetBlock"/>) and whether the dropdown arrow
/// is shown for CelFolders (<see cref="ShouldShowDropdownArrow"/>).
/// </summary>
public abstract partial class LayerTreeBase : ScrollContainer
{
    protected readonly ButtonGroup WorkingLayerButtonGroup = new();

    protected bool IsDragging;
    protected LayerBlock HoveredBlock;
    protected float ScrollSpeed;
    protected float ScrollAccum;
    /// <summary>
    /// Cached once per drag in <see cref="OnDragStart"/>.
    /// True when the dragged layer's subtree contains a CelFolder.
    /// </summary>
    protected bool DraggedSubtreeHasCelFolder;

    protected const float ScrollZone = 50f;
    protected const float MaxScrollSpeed = 280f;

    // Node refs — populated by subclass _Ready via InitBase()
    private Container _root;
    private StrokeRect _hinter;
    private Label _label;

    protected void InitBase()
    {
        _root = GetNode<Container>("%RootContainer");
        _hinter = GetNode<StrokeRect>("%DropHinter");
        _label = GetNode<Label>("%DragLabel");

        _root.QueueFreeChildren();
        _hinter.MouseFilter = MouseFilterEnum.Ignore;

        WorkingLayerButtonGroup.Pressed += button =>
        {
            var block = (LayerBlock)button.GetOwner();
            new CommandBuilder(block.LayerEntity)
            .SetWorkingLayer()
            .CommitToLatest();
        };
    }

    public override void _Process(double delta)
    {
        if (!IsDragging || ScrollSpeed == 0f) return;
        ScrollAccum += ScrollSpeed * (float)delta;
        int step = (int)ScrollAccum;
        if (step != 0)
        {
            ScrollVertical += step;
            ScrollAccum -= step;
        }
    }

    // ── Abstract factory methods ────────────────────────────────────────────

    /// <summary>Returns the <see cref="LayerWrapper"/> component stored on <paramref name="e"/>.</summary>
    protected abstract LayerWrapper GetWrapper(Entity e);

    /// <summary>Returns the <see cref="LayerBlock"/> component stored on <paramref name="e"/>.</summary>
    protected abstract LayerBlock GetBlock(Entity e);

    /// <summary>Whether the dropdown arrow should be shown for <paramref name="e"/>.</summary>
    protected virtual bool ShouldShowDropdownArrow(Entity e) => e.Has<FolderLayerSetting>();

    // ── Block initialisation ────────────────────────────────────────────────

    protected void InitBlock(Entity e)
    {
        var commonSetting = e.Get<CommonLayerSetting>();
        var cmdM = e.Document.Get<CommandManager>();

        var subs = new CompositeDisposable();
        subs.AddTo(e);

        var wrapper = GetWrapper(e);
        var block = GetBlock(e);
        block.WorkingButton.ButtonGroup = WorkingLayerButtonGroup;
        block.VisibleButton
            .BindBool(commonSetting.IsVisible, subs)
            .RegisterUndo(cmdM, true);
        var lineEdit = block.LabelLineEdit
            .BindString(commonSetting.Name, subs)
            .RegisterUndo(cmdM);

        block.MouseEntered += () => HoveredBlock = block;
        block.MouseExited += () =>
        {
            if (ReferenceEquals(HoveredBlock, block))
                HoveredBlock = null;
        };

        if (ShouldShowDropdownArrow(e))
        {
            block.DropdownArrow.Visible = true;
            var property = e.Get<FolderLayerSetting>().IsExpanded;
            block.DropdownArrow
                .BindBool(property, subs)
                .RegisterUndo(cmdM, true);
            wrapper.ObserveIsExpanded(property, subs);
        }
        else
        {
            block.DropdownArrow.Visible = false;
            // No Folded binding — wrapper stays permanently collapsed so its children
            // (which are CelFolder cels shown as timeline track rows) are never visible here.
            wrapper.IsExpanded = false;
        }

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
        singleClickObs.Subscribe(_ => block.WorkingButton.SetPressed(true)).AddTo(e);

        // Drag
        var mouseState = leftMouse.ToReadOnlyReactiveProperty();
        var dragStart = guiInput
            .Where(_ => mouseState.CurrentValue?.IsPressed() == true)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left)
            .Where(motion => motion.GlobalPosition.DistanceTo(mouseState.CurrentValue.GlobalPosition) > 20)
            .Where(_ => !IsDragging);
        dragStart.Subscribe(motion =>
        {
            IsDragging = true;
            OnDragStart(block, motion);
        }).AddTo(e);

        var dragging = guiInput
            .Where(_ => IsDragging)
            .OfType<InputEvent, InputEventMouseMotion>()
            .Where(motion => motion.ButtonMask == MouseButtonMask.Left);
        dragging.Subscribe(motion => OnDragging(block, motion)).AddTo(e);

        var dragEnd = leftMouse
            .Where(button => IsDragging && button.IsReleased());
        dragEnd.Subscribe(button =>
        {
            IsDragging = false;
            OnDragEnd(block, button);
        }).AddTo(e);
    }

    // ── Drop classification ─────────────────────────────────────────────────

    private enum DropKind { None, FolderChild, Sibling }

    private readonly record struct DropTarget(
        DropKind Kind = DropKind.None,
        Entity ParentEntity = default,
        int InsertIndex = -1);

    private DropTarget ClassifyDrop(LayerBlock draggedBlock)
    {
        if (HoveredBlock == null)
        {
            if (this.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
            {
                var docE = AppDocumentManager.WorkingDocument.CurrentValue;
                return new(DropKind.FolderChild, docE, 0);
            }
            return default;
        }

        if (ReferenceEquals(HoveredBlock, draggedBlock))
            return default;

        var draggedEntity = draggedBlock.LayerEntity;
        var hoverBlock = HoveredBlock;
        var hoverEntity = hoverBlock.LayerEntity;
        var hoverTreeNode = hoverEntity.Get<LayerTreeNode>();
        var localPos = hoverBlock.GetLocalMousePosition();
        var size = hoverBlock.Size;

        var cursor = hoverEntity;
        while (!cursor.IsNull)
        {
            if (cursor == draggedEntity) return new(DropKind.None, default, -1);
            cursor = cursor.Get<LayerTreeNode>().ParentValue;
        }

        if (DraggedSubtreeHasCelFolder && hoverBlock.Wrapper.IsBeingCeled)
            return default;

        if (hoverBlock.IsFolder && localPos.Y > size.Y / 3f)
        {
            if (DraggedSubtreeHasCelFolder && hoverBlock.IsCelFolder)
                return default;
            return new(DropKind.FolderChild, hoverEntity, 0);
        }

        var parentEntity = hoverTreeNode.ParentValue;
        int hoverIndex = hoverTreeNode.Index;
        int insertIndex = (hoverBlock.IsFolder || localPos.Y <= size.Y / 2f) ? hoverIndex + 1 : hoverIndex;

        return new(DropKind.Sibling, parentEntity, insertIndex);
    }

    // ── Drag handlers ───────────────────────────────────────────────────────

    private void OnDragStart(LayerBlock draggedBlock, InputEventMouseMotion motion)
    {
        ScrollAccum = 0f;
        DraggedSubtreeHasCelFolder = draggedBlock.Wrapper.HasCelFolderInSubtree();
        _label.Text = draggedBlock.LayerEntity.Get<CommonLayerSetting>().Name.Value;
        _label.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);
        _label.Visible = true;
    }

    private void OnDragging(LayerBlock draggedBlock, InputEventMouseMotion motion)
    {
        _label.GlobalPosition = motion.GlobalPosition + new Vector2(16f, -8f);

        var rect = GetGlobalRect();
        float mouseY = motion.GlobalPosition.Y;
        float distFromTop = mouseY - rect.Position.Y;
        float distFromBottom = rect.End.Y - mouseY;
        if (distFromTop < ScrollZone)
            ScrollSpeed = -MaxScrollSpeed * (1f - distFromTop / ScrollZone);
        else if (distFromBottom < ScrollZone)
            ScrollSpeed = MaxScrollSpeed * (1f - distFromBottom / ScrollZone);
        else
            ScrollSpeed = 0f;

        var dropTarget = ClassifyDrop(draggedBlock);

        if (dropTarget.Kind == DropKind.None)
        {
            _hinter.Visible = false;
            return;
        }

        if (dropTarget.Kind == DropKind.FolderChild)
        {
            if (!dropTarget.ParentEntity.IsDocument)
            {
                var labelLineEdit = GetBlock(dropTarget.ParentEntity).LabelLineEdit;
                _hinter.GlobalPosition = labelLineEdit.GlobalPosition;
                _hinter.Size = labelLineEdit.Size;
            }
            else
            {
                var refBlock = GetBlock(dropTarget.ParentEntity.Get<LayerTreeNode>().Children[0]);
                float startX = refBlock.DropdownArrow.GlobalPosition.X;
                float lineY = _root.GlobalPosition.Y + _root.Size.Y;
                _hinter.GlobalPosition = new Vector2(startX, lineY - _hinter.Width / 2f);
                _hinter.Size = new Vector2(refBlock.GlobalPosition.X + refBlock.Size.X - startX, _hinter.Width);
            }
            _hinter.Visible = true;
            return;
        }

        // Sibling: horizontal line at the insertion boundary
        {
            var parentChildren = dropTarget.ParentEntity.Get<LayerTreeNode>().Children;
            int insertIndex = dropTarget.InsertIndex;

            LayerBlock refBlock;
            float lineGlobalY;
            if (insertIndex < parentChildren.Count)
            {
                refBlock = GetBlock(parentChildren[insertIndex]);
                lineGlobalY = refBlock.GlobalPosition.Y + refBlock.Size.Y;
            }
            else
            {
                refBlock = GetBlock(parentChildren[^1]);
                lineGlobalY = refBlock.GlobalPosition.Y;
            }

            float startX = !dropTarget.ParentEntity.IsDocument
                ? GetBlock(dropTarget.ParentEntity).LabelLineEdit.GlobalPosition.X
                : refBlock.DropdownArrow.GlobalPosition.X;
            _hinter.GlobalPosition = new Vector2(startX, lineGlobalY - _hinter.Width / 2f);
            _hinter.Size = new Vector2(refBlock.GlobalPosition.X + refBlock.Size.X - startX, _hinter.Width);
            _hinter.Visible = true;
        }
    }

    private void OnDragEnd(LayerBlock draggedBlock, InputEventMouseButton button)
    {
        _hinter.Visible = false;
        _label.Visible = false;
        ScrollSpeed = 0f;
        ScrollAccum = 0f;

        var dropTarget = ClassifyDrop(draggedBlock);
        HoveredBlock = null;

        if (dropTarget.Kind == DropKind.None) return;

        var document = AppDocumentManager.WorkingDocument.CurrentValue;
        var draggedEntity = draggedBlock.LayerEntity;

        int insertIndex = dropTarget.InsertIndex;
        var draggedTreeNode = draggedEntity.Get<LayerTreeNode>();
        if (draggedTreeNode.ParentValue == dropTarget.ParentEntity && draggedTreeNode.Index < insertIndex)
            insertIndex--;

        new CommandBuilder(document)
            .MoveLayer(draggedEntity, dropTarget.ParentEntity, insertIndex)
            .Commit();
    }

    public void SetWorkingLayerNoSignal(Entity layerE)
    {
        WorkingLayerButtonGroup.GetPressedButton()?.SetPressedNoSignal(false);
        if (layerE.IsNull || layerE.IsDocument) return;
        var block = GetBlock(layerE);
        // Warning note: button group will not be updated by SetPressedNoSignal.
        block.WorkingButton.SetPressedNoSignal(true);
    }
}
