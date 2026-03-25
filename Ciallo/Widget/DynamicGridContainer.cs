using System.Collections.Generic;
using Godot;
using R3;

namespace Ciallo.Widget;

/// <summary>
/// Like a regular GridContainer but supports:
/// - row height changed dynamically with MinRowHeight and MaxRowHeight
/// - scaling up/down children in a fixed aspect ratio
/// - signaling drag and drop
/// - single selection
/// </summary>
/// <remarks>
/// Assumes all children controls have the same natural aspect ratio.
///
/// When resizing, children scale uniformly. Once they would exceed MaxRowHeight the column count
/// increases (more columns → smaller items); once they would fall below MinRowHeight the column
/// count decreases (fewer columns → larger items).
///
/// When a child is dragged to a new position, <see cref="Moved"/> fires with
/// (sourceIndex, destinationIndex). Middle-of-row gaps map to the last slot of the row above /
/// first slot of the row below, matching the described drop semantics.
/// </remarks>
[GlobalClass, Tool]
public partial class DynamicGridContainer : Container
{
    public float MinRowHeight { get; set; } = 64;
    public float MaxRowHeight { get; set; } = 128f;

    public Subject<(int From, int To)> Moved { get; } = new();
    public int SelectedIndex { get; private set; } = -1;

    // ── drag state ─────────────────────────────────────────────────────────
    private Control _dragSource;
    private bool _isDragging;
    private Vector2 _dragStartGlobalPos;
    private int _dropSlot = -1; // insertion slot ∈ [0, childCount], -1 = none

    private const float DragThreshold = 8f;

    // ── visuals ────────────────────────────────────────────────────────────
    private readonly StrokeRect _selectionHighlight = new() // internal
    {
        Color = new(AppPreference.StrokeWireframeColor) { A = 1.0f },
        Visible = false,
    };
    private static readonly Color DropLineColor = new(1f, 1f, 1f, 0.9f);

    private int HSeparation => GetThemeConstant("h_separation", "GridContainer");
    private int VSeparation => GetThemeConstant("v_separation", "GridContainer");

    private readonly HashSet<Control> _hookedChildren = [];

    // ── public API ─────────────────────────────────────────────────────────
    public DynamicGridContainer()
    {
        AddChild(_selectionHighlight, false, InternalMode.Back);
    }

    public void Select(int index)
    {
        SelectedIndex = index;
        QueueSort();
    }

    // ── Godot overrides ────────────────────────────────────────────────────
    public override void _Notification(int what)
    {
        if (what == NotificationSortChildren)
            Resort();
        else if (what == NotificationChildOrderChanged)
            SetupChildInputHandlers();
    }

    public override void _Draw()
    {
        if (!_isDragging) return;

        var children = GetLayoutChildren();
        if (children.Count == 0) return;

        var (cols, itemSize) = CalcLayout(children);
        int hSep = HSeparation;
        int vSep = VSeparation;


        // Drop indicator: vertical line at the left edge of the drop column
        if (_dropSlot < 0 || _dropSlot > children.Count) return;
        var slotCell = SlotToRowCol(_dropSlot, children.Count, cols);
        float x = slotCell.X * (itemSize.X + hSep);
        float y = slotCell.Y * (itemSize.Y + vSep);
        DrawLine(new Vector2(x, y), new Vector2(x, y + itemSize.Y), DropLineColor, 2f);
    }

    // ── layout ─────────────────────────────────────────────────────────────
    private List<Control> GetLayoutChildren()
    {
        var result = new List<Control>();
        foreach (Node child in GetChildren())
            if (child is Control c && !c.TopLevel && c.Visible)
                result.Add(c);
        return result;
    }

    private (int Cols, Vector2 ItemSize) CalcLayout(List<Control> children)
    {
        float w = Size.X;
        float hSep = HSeparation;
        if (w <= 0f || children.Count == 0)
            return (1, new Vector2(Mathf.Max(w, 1f), MinRowHeight));

        // Assume children are identical here.
        var ms = children[0].GetCombinedMinimumSize();
        float nw = Mathf.Max(ms.X, 1f);
        float nh = Mathf.Max(ms.Y, 1f);
        float ar = nw / nh; // natural aspect ratio (width / height)

        // cols * iw + (cols - 1) * hSep = w  ⟹  iw = (w - (cols-1)*hSep) / cols
        // ih = iw / ar
        // Solving ih = MaxRowHeight / MinRowHeight for cols:
        //   cols = (w + hSep) / (ih * ar + hSep)
        int colsMin = Mathf.Max(1, Mathf.CeilToInt((w + hSep) / (MaxRowHeight * ar + hSep)));
        int colsMax = Mathf.Max(colsMin, Mathf.FloorToInt((w + hSep) / (MinRowHeight * ar + hSep)));
        int cols = Mathf.Clamp(colsMax, 1, children.Count);

        float iw = (w - (cols - 1) * hSep) / cols;
        float ih = iw / ar;
        return (cols, new(iw, ih));
    }

    private void Resort()
    {
        var children = GetLayoutChildren();
        if (children.Count == 0)
        {
            CustomMinimumSize = Vector2.Zero;
            _selectionHighlight.Visible = false;
            QueueRedraw();
            return;
        }

        var (cols, itemSize) = CalcLayout(children);
        int hSep = HSeparation;
        int vSep = VSeparation;
        for (int i = 0; i < children.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;
            FitChildInRect(children[i], new Rect2(
                col * (itemSize.X + hSep),
                row * (itemSize.Y + vSep),
                itemSize.X, itemSize.Y));
        }

        if (SelectedIndex >= 0 && SelectedIndex < children.Count)
        {
            _selectionHighlight.Visible = true;
            FitChildInRect(_selectionHighlight, ItemRect(SelectedIndex, children.Count, cols, itemSize, hSep, vSep));
        }
        else
        {
            _selectionHighlight.Visible = false;
        }

        int rows = Mathf.CeilToInt((float)children.Count / cols);
        CustomMinimumSize = new Vector2(0f, rows * itemSize.Y + (rows - 1) * vSep);
        QueueRedraw();
    }

    // ── input ──────────────────────────────────────────────────────────────
    private void SetupChildInputHandlers()
    {
        _hookedChildren.RemoveWhere(c => !IsInstanceValid(c) || c.GetParent() != this);

        foreach (Node child in GetChildren())
        {
            if (child is not Control c || c.TopLevel || _hookedChildren.Contains(c)) continue;
            _hookedChildren.Add(c);
            var captured = c;
            c.SignalAsObservable<InputEvent>(Control.SignalName.GuiInput)
                .Subscribe(et => OnChildGuiInput(captured, et))
                .AddTo(c);
        }
    }

    private void OnChildGuiInput(Control child, InputEvent ev)
    {
        switch (ev)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb when mb.Pressed:
                _dragSource = child;
                _dragStartGlobalPos = mb.GlobalPosition;
                _isDragging = false;
                Select(GetLayoutChildren().IndexOf(child));
                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb
                when !mb.Pressed && ReferenceEquals(_dragSource, child):
                FinishDrag(mb.GlobalPosition - GlobalPosition);
                break;

            case InputEventMouseMotion motion
                when ReferenceEquals(_dragSource, child) && motion.ButtonMask == MouseButtonMask.Left:
                if (!_isDragging)
                {
                    if (motion.GlobalPosition.DistanceTo(_dragStartGlobalPos) <= DragThreshold) return;
                    _isDragging = true;
                }
                _dropSlot = ComputeDropSlot(motion.GlobalPosition - GlobalPosition);
                QueueRedraw();
                break;
        }
    }

    private void FinishDrag(Vector2 localPos)
    {
        if (_isDragging && _dragSource != null && IsInstanceValid(_dragSource))
        {
            var children = GetLayoutChildren();
            int srcIdx = children.IndexOf(_dragSource);
            int slot = ComputeDropSlot(localPos);
            int dstIdx = slot > srcIdx ? slot - 1 : slot;
            if (srcIdx >= 0 && dstIdx != srcIdx)
            {
                Moved.OnNext((srcIdx, dstIdx));
            }
        }
        _dragSource = null;
        _isDragging = false;
        _dropSlot = -1;
        QueueRedraw();
    }

    private int ComputeDropSlot(Vector2 localPos)
    {
        var children = GetLayoutChildren();
        if (children.Count == 0) return 0;

        var (cols, itemSize) = CalcLayout(children);
        int hSep = HSeparation;
        int vSep = VSeparation;
        int rows = Mathf.CeilToInt((float)children.Count / cols);

        int row = Mathf.Clamp(Mathf.FloorToInt(localPos.Y / (itemSize.Y + vSep)), 0, rows - 1);
        int rowStart = row * cols;
        int rowItemCnt = Mathf.Min(cols, children.Count - rowStart);

        // Nearest gap within the row: 0 = before first item, rowItemCnt = after last item in the row.
        int gapInRow = Mathf.Clamp(Mathf.RoundToInt(localPos.X / (itemSize.X + hSep)), 0, rowItemCnt);
        return Mathf.Min(rowStart + gapInRow, children.Count);
    }

    // ── helpers ────────────────────────────────────────────────────────────
    private static Rect2 ItemRect(int slotIdx, int count, int cols, Vector2 itemSize, int hSep, int vSep)
    {
        var cell = SlotToRowCol(slotIdx, count, cols);
        return new Rect2(cell.X * (itemSize.X + hSep), cell.Y * (itemSize.Y + vSep), itemSize.X, itemSize.Y);
    }

    /// <summary>Maps an insertion slot index to a (col, row) cell coordinate for drawing.</summary>
    private static Vector2I SlotToRowCol(int slot, int count, int cols)
    {
        if (slot >= count)
        {
            int last = count - 1;
            int r = last / cols;
            int c = last % cols + 1;
            if (c >= cols)
            {
                c = 0;
                r++;
            }
            return new(c, r);
        }
        return new(slot % cols, slot / cols);
    }
}