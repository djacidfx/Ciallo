using System;
using System.Collections.Generic;
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
    private Control _mouseHoveringLayer;

    public override void _Ready()
    {
        // Free previews in the Godot editor.
        TreeRoot.QueueFreeChildren();
        _workingLayerButtonGroup.Pressed += button =>
        {
            var layerControl = (Control)button.GetOwner();
            var document = AppDocumentManager.WorkingDocument.CurrentValue;
            var layerE = document.Get<LayerTreeNode>().Children[layerControl.GetIndex()];
            new CommandBuilder(layerE).SetWorkingLayer().Commit();
        };
    }

    public void Create(Entity layerE)
    {
        var layerBlock = CreateBlock(layerE);
        layerE.AddNode(layerBlock);
    }

    public void Insert(Entity layerE, int index)
    {
        var layerBlock = layerE.Get<LayerBlock>();
        TreeRoot.InsertNodeAt(layerBlock, index);
    }

    private LayerBlock CreateBlock(Entity e)
    {
        var commonSetting = e.Get<CommonLayerSetting>();
        var subs = new CompositeDisposable().AddTo(e);
        var cmdM = e.Document.Get<CommandManager>();

        var block = LayerBlock.New();
        block.WorkingButton.ButtonGroup = _workingLayerButtonGroup;
        block.VisibleButton
            .BindBool(commonSetting.IsVisible, out var sub0)
            .RegisterUndo(cmdM);
        var lineEdit = block.GetNode<LabelLineEdit>("%LabelLineEdit")
            .BindString(commonSetting.Name, out var sub1)
            .RegisterUndo(cmdM);
        sub0.AddTo(subs);
        sub1.AddTo(subs);

        block.MouseEntered += () => _mouseHoveringLayer = block;
        block.MouseExited += () => _mouseHoveringLayer = null;

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

    public void Move(IReadOnlyList<int> src, IReadOnlyList<int> dst)
    {
        int srcIdx = src[0];
        int dstIdx = dst[0];
        TreeRoot.MoveChild(TreeRoot.GetChild(srcIdx), dstIdx);
    }

    public void Remove(Entity layerE)
    {
        var layerBlock = layerE.Get<LayerBlock>();
        if (layerBlock.GetParent() != null)
            layerBlock.RemoveFromParent(); // necessary to avoid index error
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

        var sep = size.Y / 2; // separation on whether the drop target is above or below the hovering layer.
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

        var srcIndex = srcLayer.GetIndex();
        var dstIndex = _mouseHoveringLayer.GetIndex();
        if (srcIndex < dstIndex) dstIndex--; // account for the removal of the source layer.

        var locPos = _mouseHoveringLayer.GetLocalMousePosition();
        var size = _mouseHoveringLayer.Size;
        if (locPos.Y <= size.Y / 2) dstIndex++; // insert after the hovering layer.

        new CommandBuilder(AppDocumentManager.WorkingDocument.CurrentValue)
            .MoveLayer([srcIndex], [dstIndex]).Commit();
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