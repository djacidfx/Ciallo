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
/// <remarks>
/// If being contained by a ScrollContainer, remember to avoid scrollbar oscillation by setting the ScrollContainer.
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

    // ── cached layout state (set in Resort) ────────────────────────────────
    private int _cols;
    private Vector2 _itemSize;
    private int _childCount;
    private int _hSep;
    private int _vSep;

    // ── visuals ────────────────────────────────────────────────────────────
    private readonly StrokeRect _selectionHighlight = new() // internal
    {
        Color = new(AppPreference.StrokeWireframeColor) { A = 1.0f },
        MouseFilter = MouseFilterEnum.Ignore,
        Visible = false,
    };
    private readonly ColorRect _dropHintLine = new()
    {
        Color = new(1f, 1f, 1f, 0.9f),
        MouseFilter = MouseFilterEnum.Ignore,
        Visible = false,
    };

    private int HSeparation => GetThemeConstant("h_separation", "GridContainer");
    private int VSeparation => GetThemeConstant("v_separation", "GridContainer");

    // ── public API ─────────────────────────────────────────────────────────
    public DynamicGridContainer()
    {
        MouseFilter = MouseFilterEnum.Stop;
        AddChild(_selectionHighlight, false, InternalMode.Back);
        AddChild(_dropHintLine, false, InternalMode.Back);
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
            EnsureChildMouseFilters();
    }

    private void UpdateDropHintLine()
    {
        if (!_isDragging || _dropSlot < 0 || _dropSlot > _childCount)
        {
            _dropHintLine.Visible = false;
            return;
        }
        const float lineWidth = 2f;
        var slotCell = SlotToRowCol(_dropSlot);

        if (_cols == 1)
        {
            // Single-column: horizontal line centred in the vertical separator gap
            float yCentre = slotCell.Y * (_itemSize.Y + _vSep) - _vSep / 2f;
            _dropHintLine.Position = new Vector2(0f, Mathf.Max(0f, yCentre - lineWidth / 2f));
            _dropHintLine.Size = new Vector2(_itemSize.X, lineWidth);
        }
        else
        {
            // Multi-column: vertical line centred in the horizontal separator gap
            float xCentre = slotCell.X * (_itemSize.X + _hSep) - _hSep / 2f;
            _dropHintLine.Position = new Vector2(Mathf.Max(0f, xCentre - lineWidth / 2f), slotCell.Y * (_itemSize.Y + _vSep));
            _dropHintLine.Size = new Vector2(lineWidth, _itemSize.Y);
        }
        _dropHintLine.Visible = true;
    }

    // ── layout ─────────────────────────────────────────────────────────────
    private List<Control> GetLayoutChildren()
    {
        var result = new List<Control>();
        foreach (Node child in GetChildren())
            if (child is Control { TopLevel: false, Visible: true } c)
                result.Add(c);
        return result;
    }

    private (int Cols, Vector2 ItemSize) CalcLayout(List<Control> children)
    {
        float w = Size.X;
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
        int colsMin = Mathf.Max(1, Mathf.CeilToInt((w + _hSep) / (MaxRowHeight * ar + _hSep)));
        int colsMax = Mathf.Max(colsMin, Mathf.FloorToInt((w + _hSep) / (MinRowHeight * ar + _hSep)));
        int cols = Mathf.Clamp(colsMax, 1, children.Count);

        float iw = (w - (cols - 1) * _hSep) / cols;
        float ih = iw / ar;
        return (cols, new(iw, ih));
    }

    private void Resort()
    {
        _hSep = HSeparation;
        _vSep = VSeparation;
        var children = GetLayoutChildren();
        if (children.Count == 0)
        {
            _cols = 0;
            _itemSize = Vector2.Zero;
            _childCount = 0;
            CustomMinimumSize = Vector2.Zero;
            _selectionHighlight.Visible = false;
            return;
        }

        (_cols, _itemSize) = CalcLayout(children);
        _childCount = children.Count;

        for (int i = 0; i < children.Count; i++)
        {
            int col = i % _cols;
            int row = i / _cols;
            FitChildInRect(children[i], new Rect2(
                col * (_itemSize.X + _hSep),
                row * (_itemSize.Y + _vSep),
                _itemSize.X, _itemSize.Y));
        }

        if (SelectedIndex >= 0 && SelectedIndex < children.Count)
        {
            _selectionHighlight.Visible = true;
            FitChildInRect(_selectionHighlight, ItemRect(SelectedIndex));
        }
        else
        {
            _selectionHighlight.Visible = false;
        }

        int rows = Mathf.CeilToInt((float)_childCount / _cols);
        CustomMinimumSize = new Vector2(0f, rows * _itemSize.Y + (rows - 1) * _vSep);
    }

    // ── input ──────────────────────────────────────────────────────────────
    private void EnsureChildMouseFilters()
    {
        foreach (Node child in GetChildren())
            if (child is Control c && !c.TopLevel)
                c.MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _GuiInput(InputEvent et)
    {
        switch (et)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb when mb.IsPressed():
            {
                int idx = HitTestIndex(mb.Position);
                if (idx < 0) return;
                _dragSource = GetLayoutChildren()[idx];
                _dragStartGlobalPos = mb.GlobalPosition;
                _isDragging = false;
                Select(idx);
                AcceptEvent();
                break;
            }
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb when mb.IsReleased():
                FinishDrag(mb.Position);
                AcceptEvent();
                break;

            case InputEventMouseMotion { ButtonMask: MouseButtonMask.Left } motion when _dragSource != null:
                if (!_isDragging)
                {
                    if (motion.GlobalPosition.DistanceTo(_dragStartGlobalPos) <= DragThreshold) return;
                    _isDragging = true;
                }
                _dropSlot = ComputeDropSlot(motion.Position);
                UpdateDropHintLine();
                AcceptEvent();
                break;
        }
    }

    private int HitTestIndex(Vector2 localPos)
    {
        if (_childCount == 0) return -1;

        int col = Mathf.FloorToInt(localPos.X / (_itemSize.X + _hSep));
        int row = Mathf.FloorToInt(localPos.Y / (_itemSize.Y + _vSep));
        if (col < 0 || row < 0 || col >= _cols) return -1;

        // Reject clicks that land inside the separator gap
        float localX = localPos.X - col * (_itemSize.X + _hSep);
        float localY = localPos.Y - row * (_itemSize.Y + _vSep);
        if (localX > _itemSize.X || localY > _itemSize.Y) return -1;

        int idx = row * _cols + col;
        return idx >= _childCount ? -1 : idx;
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
        UpdateDropHintLine();
    }

    private int ComputeDropSlot(Vector2 localPos)
    {
        if (_childCount == 0) return 0;

        if (_cols == 1)
        {
            // Single-column: drop positions are above/below each element; use nearest row boundary by Y.
            return Mathf.Clamp(Mathf.RoundToInt(localPos.Y / (_itemSize.Y + _vSep)), 0, _childCount);
        }

        int rows = Mathf.CeilToInt((float)_childCount / _cols);
        int row = Mathf.Clamp(Mathf.FloorToInt(localPos.Y / (_itemSize.Y + _vSep)), 0, rows - 1);
        int rowStart = row * _cols;
        int rowItemCnt = Mathf.Min(_cols, _childCount - rowStart);

        // Nearest gap within the row: 0 = before first item, rowItemCnt = after last item in the row.
        int gapInRow = Mathf.Clamp(Mathf.RoundToInt(localPos.X / (_itemSize.X + _hSep)), 0, rowItemCnt);
        return Mathf.Min(rowStart + gapInRow, _childCount);
    }

    private Rect2 ItemRect(int slotIdx)
    {
        var cell = SlotToRowCol(slotIdx);
        return new Rect2(cell.X * (_itemSize.X + _hSep), cell.Y * (_itemSize.Y + _vSep), _itemSize.X, _itemSize.Y);
    }

    /// <summary>Maps an insertion slot index to a (col, row) cell coordinate for drawing.</summary>
    private Vector2I SlotToRowCol(int slot)
    {
        if (slot >= _childCount)
        {
            int last = _childCount - 1;
            int r = last / _cols;
            int c = last % _cols + 1;
            if (c >= _cols)
            {
                c = 0;
                r++;
            }
            return new(c, r);
        }
        return new(slot % _cols, slot / _cols);
    }
}