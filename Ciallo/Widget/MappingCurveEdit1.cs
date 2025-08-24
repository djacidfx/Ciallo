using System;
using System.Collections.Generic;
using Ciallo.Misc;
using Ciallo.Geometry;
using Godot;

namespace Ciallo.Widget;

/// <summary>
/// The godot editor's curve edit control at runtime.
/// </summary>
[Tool, GlobalClass]
public partial class MappingCurveEdit1 : Control
{
    public override void _Ready()
    {
        _curve = new PolyBezier();
        _curve.AddPoint(Vector2.Zero, new(-0.1f, 0), new(0.1f, 0));
        _curve.AddPoint(Vector2.One * 0.25f, new(-0.1f, 0), new(0.1f, 0));
        _curve.AddPoint(Vector2.One * 0.5f, new(-0.1f, 0), new(0.1f, 0));
        _curve.AddPoint(Vector2.One * 0.75f, new(-0.1f, 0), new(0.1f, 0));
        _curve.AddPoint(Vector2.One, new(-0.1f, 0), new(0.1f, 0));
        _CurveChanged();
    }

    [Export] public float MinDomain = 0.0f;
    [Export] public float MaxDomain = 1.0f;
    [Export] public float MinValue = 0.0f;
    [Export] public float MaxValue = 1.0f;
    public float DomainRange => MaxDomain - MinDomain;
    public float ValueRange => MaxValue - MinValue;

    private const float AspectRatio = 6f / 13f;
    private const float LineWidth = 0.5f;
    private const int StepSize = 2; // Number of pixels between plot points.

    private PolyBezier _curve;
    private Transform2D _worldToView;
    private int _selectedIndex = -1;
    private int _hoveredIndex = -1;
    private TangentIndex _selectedTangentIndex = TangentIndex.None;
    private TangentIndex _hoveredTangentIndex = TangentIndex.None;

    private const int BasePointRadius = 4;
    private const int BaseHoverRadius = 10;
    private const int BaseTangentRadius = 3;
    private const int BaseTangentHoverRadius = 8;
    private const int BaseTangentLength = 36;

    private int _pointRadius = BasePointRadius;
    private int _hoverRadius = BaseHoverRadius;
    private int _tangentRadius = BaseTangentRadius;
    private int _tangentHoverRadius = BaseTangentHoverRadius;
    private int _tangentLength = BaseTangentLength;

    private GrabMode _grabbing = GrabMode.None;
    private Vector2 _initialGrabPos;
    private int _initialGrabIndex = -1;
    private float _initialGrabLeftTangent = 0;
    private float _initialGrabRightTangent = 0;

    private bool _snapEnabled = false;
    private int _snapCount = 10;

    public enum PresetId
    {
        Constant = 0,
        Linear,
        EaseIn,
        EaseOut,
        Smoothstep,
        Count
    }

    public enum TangentIndex
    {
        None = -1,
        Left = 0,
        Right = 1
    }

    private enum GrabMode
    {
        None,
        Add,
        Move
    }

    public MappingCurveEdit1()
    {
        FocusMode = FocusModeEnum.All;
        ClipContents = true;
    }

    public PolyBezier Curve
    {
        get => _curve;
        set
        {
            if (_curve == value)
                return;

            _curve = value;
            _CurveChanged();
        }
    }

    public override Vector2 _GetMinimumSize()
    {
        return new Vector2(64, Mathf.Max(135, Size.X * AspectRatio)) * 1.0f; // EDSCALE approximated as 1.0
    }

    // public void UsePreset(PresetId presetId)
    // {
    //     if (_curve == null || presetId < 0 || presetId >= PresetId.Count)
    //         return;
    //
    //     // Note: Undo/redo not supported in C# without editor-specific APIs, so we modify the curve directly
    //     _curve.Clear();
    //
    //     float minY = MinValue;
    //     float maxY = MaxValue;
    //     float minX = MinDomain;
    //     float maxX = MaxDomain;
    //
    //     switch (presetId)
    //     {
    //         case PresetId.Constant:
    //             _curve.AddPoint(new Vector2(minX, (minY + maxY) / 2.0f));
    //             _curve.AddPoint(new Vector2(maxX, (minY + maxY) / 2.0f));
    //             _curve.SetPointRightMode(0, Curve.TangentMode.Linear);
    //             _curve.SetPointLeftMode(1, Curve.TangentMode.Linear);
    //             break;
    //
    //         case PresetId.Linear:
    //             _curve.AddPoint(new Vector2(minX, minY));
    //             _curve.AddPoint(new Vector2(maxX, maxY));
    //             _curve.SetPointRightMode(0, Curve.TangentMode.Linear);
    //             _curve.SetPointLeftMode(1, Curve.TangentMode.Linear);
    //             break;
    //
    //         case PresetId.EaseIn:
    //             _curve.AddPoint(new Vector2(minX, minY));
    //             _curve.AddPoint(new Vector2(maxX, maxY), ValueRange / DomainRange * 1.4f, 0);
    //             break;
    //
    //         case PresetId.EaseOut:
    //             _curve.AddPoint(new Vector2(minX, minY), 0, ValueRange / DomainRange * 1.4f);
    //             _curve.AddPoint(new Vector2(maxX, maxY));
    //             break;
    //
    //         case PresetId.Smoothstep:
    //             _curve.AddPoint(new Vector2(minX, minY));
    //             _curve.AddPoint(new Vector2(maxX, maxY));
    //             break;
    //     }
    //
    //     SetSelectedIndex(-1);
    // }

    public override void _GuiInput(InputEvent @event)
    {
        if (_curve == null)
            return;
        
        if (@event is InputEventKey { Pressed: true } keyEvent)
        {
            if (keyEvent.Keycode == Key.Delete)
            {
                if (_selectedTangentIndex != TangentIndex.None)
                {
                    // ToggleLinear(_selectedIndex, _selectedTangentIndex);
                }
                else if (_selectedIndex != -1)
                {
                    if (_grabbing == GrabMode.Add)
                    {
                        _curve.RemovePoint(_selectedIndex);
                        SetSelectedIndex(-1);
                    }
                    else
                    {
                        RemovePoint(_selectedIndex);
                    }
                    _grabbing = GrabMode.None;
                    _hoveredIndex = -1;
                    _hoveredTangentIndex = TangentIndex.None;
                }
                AcceptEvent();
            }

            if (keyEvent.Keycode == Key.Shift || keyEvent.Keycode == Key.Alt)
            {
                QueueRedraw();
            }
        }

        if (@event is InputEventMouseButton { Pressed: true } mouseButton)
        {
            Vector2 mpos = mouseButton.Position;

            if (mouseButton.ButtonIndex is MouseButton.Right or MouseButton.Middle)
            {
                // Cancel any ongoing grab operation.
                if (mouseButton.ButtonIndex is MouseButton.Right && _grabbing == GrabMode.Move)
                {
                    // _curve.SetPointValue(_selectedIndex, _initialGrabPos.Y);
                    // _curve.SetPointOffset(_selectedIndex, _initialGrabPos.X);
                    // SetSelectedIndex(_initialGrabIndex);
                    _curve.SetPointPosition(_selectedIndex, _initialGrabPos);
                    SetSelectedIndex(_initialGrabIndex);
                    _hoveredIndex = GetPointAt(mpos);
                    _grabbing = GrabMode.None;
                }
                else
                {
                    _selectedTangentIndex = GetTangentAt(mpos);
                    if (_selectedTangentIndex != TangentIndex.None)
                    {
                        // ToggleLinear(_selectedIndex, _selectedTangentIndex);
                    }
                    else
                    {
                        int pointToRemove = GetPointAt(mpos);
                        if (pointToRemove == -1)
                        {
                            SetSelectedIndex(-1);
                        }
                        else
                        {
                            if (_grabbing == GrabMode.Add)
                            {
                                _curve.RemovePoint(pointToRemove);
                                SetSelectedIndex(-1);
                            }
                            else
                            {
                                RemovePoint(pointToRemove);
                            }
                            _hoveredIndex = GetPointAt(mpos);
                            _grabbing = GrabMode.None;
                        }
                    }
                }
            }

            // Selecting or creating points.
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (_grabbing == GrabMode.None)
                {
                    _selectedTangentIndex = GetTangentAt(mpos);
                    if (_selectedTangentIndex == TangentIndex.None)
                    {
                        SetSelectedIndex(GetPointAt(mpos));
                    }
                    QueueRedraw();
                }

                if (_selectedIndex != -1)
                {
                    _grabbing = GrabMode.Move;
                    _initialGrabPos = _curve.GetPointPosition(_selectedIndex);
                    _initialGrabIndex = _selectedIndex;
                    if (_selectedIndex > 0)
                        _initialGrabLeftTangent = _curve.GetPointInTangent(_selectedIndex);
                    if (_selectedIndex < _curve.Count - 1)
                        _initialGrabRightTangent = _curve.GetPointOutTangent(_selectedIndex);
                }
                else if (_grabbing == GrabMode.None)
                {
                    Vector2 newPos = GetWorldPos(mpos).Clamp(
                        new Vector2(MinDomain, MinValue),
                        new Vector2(MaxDomain, MaxValue)
                    );
                    if (_snapEnabled || mouseButton.IsCommandOrControlPressed())
                    {
                        newPos.X = Mathf.Snapped(newPos.X - MinDomain, DomainRange / _snapCount) + MinDomain;
                        newPos.Y = Mathf.Snapped(newPos.Y - MinValue, ValueRange / _snapCount) + MinValue;
                    }

                    newPos.X = GetOffsetWithoutCollision(_selectedIndex, newPos.X, mpos.X >= GetViewPos(newPos).X);

                    // int newIdx = _curve.AddPoint(newPos); //Suppose to be addPointNoSignal, potential bug
                    // SetSelectedIndex(newIdx);
                    _curve.AddPoint(newPos, new(-0.1f, 0), new(0.1f, 0));
                    SetSelectedIndex(_curve.Count - 1);
                    _grabbing = GrabMode.Add;
                    _initialGrabPos = newPos;
                }
            }
        }

        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false })
        {
            if (_selectedTangentIndex != TangentIndex.None)
            {
                if (_selectedIndex == 0)
                {
                    SetPointOutTangent(_selectedIndex, _curve.GetPointOutTangent(_selectedIndex));
                }
                else if (_selectedIndex == _curve.Count - 1)
                {
                    SetPointInTangent(_selectedIndex, _curve.GetPointInTangent(_selectedIndex));
                }
                else
                {
                    SetPointTangents(_selectedIndex, _curve.GetPointInTangent(_selectedIndex), _curve.GetPointOutTangent(_selectedIndex));
                }
                _grabbing = GrabMode.None;
            }
            else if (_grabbing == GrabMode.Move)
            {
                SetPointPosition(_selectedIndex, _curve.GetPointPosition(_selectedIndex));
                _grabbing = GrabMode.None;
            }
            else if (_grabbing == GrabMode.Add)
            {
                Vector2 newPos = _curve.GetPointPosition(_selectedIndex);
                _curve.RemovePoint(_selectedIndex);
                AddPoint(newPos);
                _grabbing = GrabMode.None;
            }
            QueueRedraw();
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            Vector2 mpos = mouseMotion.Position;

            if (_grabbing != GrabMode.None && _curve != null)
            {
                if (_selectedIndex != -1)
                {
                    if (_selectedTangentIndex == TangentIndex.None)
                    {
                        Vector2 newPos = GetWorldPos(mpos).Clamp(
                            new Vector2(MinDomain, MinValue),
                            new Vector2(MaxDomain, MaxValue)
                        );

                        if (_snapEnabled || mouseMotion.IsCommandOrControlPressed())
                        {
                            newPos.X = Mathf.Snapped(newPos.X - MinDomain, DomainRange / _snapCount) + MinDomain;
                            newPos.Y = Mathf.Snapped(newPos.Y - MinValue, ValueRange / _snapCount) + MinValue;
                        }

                        if (mouseMotion.IsAltPressed())
                        {
                            Vector2 initialMpos = GetViewPos(_initialGrabPos);
                            if (Mathf.Abs(mpos.X - initialMpos.X) > Mathf.Abs(mpos.Y - initialMpos.Y))
                                newPos.Y = _initialGrabPos.Y;
                            else
                                newPos.X = _initialGrabPos.X;
                        }

                        if (mouseMotion.IsAltPressed())
                        {
                            float prevPointOffset = _selectedIndex > 0 ? _curve.GetPointPosition(_selectedIndex - 1).X + 0.00001f : MinDomain;
                            float nextPointOffset = _selectedIndex < _curve.Count - 1 ? _curve.GetPointPosition(_selectedIndex + 1).X - 0.00001f : MaxDomain;
                            newPos.X = Mathf.Clamp(newPos.X, prevPointOffset, nextPointOffset);
                        }

                        newPos.X = GetOffsetWithoutCollision(_selectedIndex, newPos.X, mpos.X >= GetViewPos(newPos).X);

                        // int i = _curve.SetPointOffset(_selectedIndex, newPos.X);
                        // _hoveredIndex = i;
                        // SetSelectedIndex(i);

                        // newPos.Y = Mathf.Clamp(newPos.Y, MinValue, MaxValue);
                        // _curve.SetPointValue(_selectedIndex, newPos.Y);
                        newPos.Y = Mathf.Clamp(newPos.Y, MinValue, MaxValue);
                        _curve.SetPointPosition(_selectedIndex, newPos);
                    }
                    else
                    {
                        Vector2 newPos = _curve.GetPointPosition(_selectedIndex);
                        Vector2 controlPos = GetWorldPos(mpos);

                        Vector2 dir = (controlPos - newPos).Normalized();
                        float tangent = dir.Y / (dir.X > 0 ? Mathf.Max(dir.X, 0.00001f) : Mathf.Min(dir.X, -0.00001f));

                        _hoveredTangentIndex = _selectedTangentIndex;

                        if (_selectedTangentIndex == TangentIndex.Left)
                        {
                            _curve.SetPointInTangent(_selectedIndex, tangent);
                            // if (_selectedIndex != _curve.Count - 1 && _curve.GetPointRightMode(_selectedIndex) != Curve.TangentMode.Linear)
                            // {
                            //     _curve.SetPointOutTangent(_selectedIndex, mouseMotion.IsAltPressed() ? _initialGrabRightTangent : tangent);
                            // }
                        }
                        else
                        {
                            _curve.SetPointOutTangent(_selectedIndex, tangent);
                            // if (_selectedIndex != 0 && _curve.GetPointLeftMode(_selectedIndex) != Curve.TangentMode.Linear)
                            // {
                            //     _curve.SetPointInTangent(_selectedIndex, mouseMotion.IsAltPressed() ? _initialGrabLeftTangent : tangent);
                            // }
                        }
                    }
                }
            }
            else
            {
                _hoveredIndex = GetPointAt(mpos);
                _hoveredTangentIndex = GetTangentAt(mpos);
                QueueRedraw();
            }
        }
        
        _CurveChanged();
    }

    private void _CurveChanged()
    {
        QueueRedraw();
        if (_selectedIndex >= _curve.Count)
            SetSelectedIndex(-1);
    }

    private int GetPointAt(Vector2 pos)
    {
        if (_curve == null)
            return -1;

        Rect2 hoverRect = new Rect2(pos, Vector2.Zero).Grow(_hoverRadius);
        int closestIdx = -1;
        float closestDistSquared = _hoverRadius * _hoverRadius * 2;

        for (int i = 0; i < _curve.Count; i++)
        {
            Vector2 p = GetViewPos(_curve.GetPointPosition(i));
            if (hoverRect.HasPoint(p) && p.DistanceSquaredTo(pos) < closestDistSquared)
            {
                closestDistSquared = p.DistanceSquaredTo(pos);
                closestIdx = i;
            }
        }

        return closestIdx;
    }

    private TangentIndex GetTangentAt(Vector2 pos)
    {
        if (_curve == null || _selectedIndex < 0)
            return TangentIndex.None;

        Rect2 hoverRect = new Rect2(pos, Vector2.Zero).Grow(_tangentHoverRadius);

        if (_selectedIndex != 0)
        {
            Vector2 controlPos = GetTangentViewPos(_selectedIndex, TangentIndex.Left);
            if (hoverRect.HasPoint(controlPos))
                return TangentIndex.Left;
        }

        if (_selectedIndex != _curve.Count - 1)
        {
            Vector2 controlPos = GetTangentViewPos(_selectedIndex, TangentIndex.Right);
            if (hoverRect.HasPoint(controlPos))
                return TangentIndex.Right;
        }

        return TangentIndex.None;
    }

    private float GetOffsetWithoutCollision(int currentIndex, float offset, bool prioritizeRight)
    {
        float safeOffset = offset;
        bool prioritizingRight = prioritizeRight;

        for (int i = 0; i < _curve.Count; i++)
        {
            if (i == currentIndex)
                continue;

            if (_curve.GetPointPosition(i).X > safeOffset)
                break;

            if (Mathf.IsEqualApprox(_curve.GetPointPosition(i).X, safeOffset))
            {
                if (prioritizingRight)
                {
                    safeOffset += 0.00001f;
                    if (safeOffset > 1.0f)
                    {
                        safeOffset = 1.0f;
                        prioritizingRight = false;
                    }
                }
                else
                {
                    safeOffset -= 0.00001f;
                    if (safeOffset < 0.0f)
                    {
                        safeOffset = 0.0f;
                        prioritizingRight = true;
                    }
                }
                i = -1;
            }
        }

        return safeOffset;
    }

    private void AddPoint(Vector2 pos)
    {
        if (_curve == null)
            return;

        // Note: Undo/redo not supported, so we add the point directly
        _curve.AddPoint(pos, new(-0.1f, 0), new(0.1f, 0));
        SetSelectedIndex(_curve.Count - 1);
    }

    private void RemovePoint(int index)
    {
        if (_curve == null || index < 0 || index >= _curve.Count)
            return;

        // Note: Undo/redo not supported, so we remove the point directly
        int newSelectedIndex = _selectedIndex;
        if (newSelectedIndex > index)
            newSelectedIndex -= 1;
        else if (newSelectedIndex == index)
            newSelectedIndex = -1;

        _curve.RemovePoint(index);
        SetSelectedIndex(newSelectedIndex);
    }

    private void SetPointPosition(int index, Vector2 pos)
    {
        if (_curve == null || index < 0 || index >= _curve.Count)
            return;

        if (_initialGrabPos == pos)
            return;
        
        // _curve.SetPointValue(index, pos.Y);
        // int newIndex = _curve.SetPointOffset(index, pos.X);
        // SetSelectedIndex(newIndex);
        _curve.SetPointPosition(index, pos);
        SetSelectedIndex(index);
    }

    private void SetPointTangents(int index, float left, float right)
    {
        if (_curve == null || index < 0 || index >= _curve.Count)
            return;

        if (_initialGrabLeftTangent == left && _initialGrabRightTangent == right)
            return;

        // Note: Undo/redo not supported, so we set the tangents directly
        _curve.SetPointInTangent(index, left);
        _curve.SetPointOutTangent(index, right);
        SetSelectedIndex(index);
    }

    private void SetPointInTangent(int index, float tangent)
    {
        if (_curve == null || index < 0 || index >= _curve.Count)
            return;

        if (_initialGrabLeftTangent == tangent)
            return;

        // Note: Undo/redo not supported
        _curve.SetPointInTangent(index, tangent);
        SetSelectedIndex(index);
    }

    private void SetPointOutTangent(int index, float tangent)
    {
        if (_curve == null || index < 0 || index >= _curve.Count)
            return;

        if (_initialGrabRightTangent == tangent)
            return;

        // Note: Undo/redo not supported
        _curve.SetPointOutTangent(index, tangent);
        SetSelectedIndex(index);
    }

    // private void ToggleLinear(int index, TangentIndex tangent)
    // {
    //     if (_curve == null || index < 0 || index >= _curve.Count || tangent == TangentIndex.None)
    //         return;
    //
    //     // Note: Undo/redo not supported
    //     Curve.TangentMode prevMode = tangent == TangentIndex.Left ? _curve.GetPointLeftMode(index) : _curve.GetPointRightMode(index);
    //     Curve.TangentMode mode = prevMode == Curve.TangentMode.Linear ? Curve.TangentMode.Free : Curve.TangentMode.Linear;
    //
    //     if (tangent == TangentIndex.Left)
    //         _curve.SetPointLeftMode(index, mode);
    //     else
    //         _curve.SetPointRightMode(index, mode);
    // }

    private void SetSelectedIndex(int index)
    {
        if (_selectedIndex != index)
        {
            _selectedIndex = index;
            QueueRedraw();
        }
    }

    private void UpdateViewTransform()
    {
        float fontSize = 20; // Approximate default font size
        float margin = fontSize + 4; // Approximate EDSCALE as 1.0

        float minX = MinDomain;
        float maxX = MaxDomain;
        float minY = MinValue;
        float maxY = MaxValue;

        Rect2 worldRect = new Rect2(minX, minY, maxX - minX, maxY - minY);
        Vector2 viewMargin = new Vector2(margin, margin);
        Vector2 viewSize = Size - viewMargin * 2;
        Vector2 scale = viewSize / worldRect.Size;

        Transform2D worldTrans = Transform2D.Identity;
        worldTrans = worldTrans.Translated(-worldRect.Position - new Vector2(0, worldRect.Size.Y));
        worldTrans = worldTrans.Scaled(new Vector2(scale.X, -scale.Y));

        Transform2D viewTrans = Transform2D.Identity;
        viewTrans = viewTrans.Translated(viewMargin);

        _worldToView = viewTrans * worldTrans;
    }

    private Vector2 GetTangentViewPos(int index, TangentIndex tangent)
    {
        Vector2 dir;
        if (tangent == TangentIndex.Left)
            dir = -new Vector2(1, _curve.GetPointInTangent(index));
        else
            dir = new Vector2(1, _curve.GetPointOutTangent(index));

        Vector2 pointPos = _curve.GetPointPosition(index);
        Vector2 pointViewPos = GetViewPos(pointPos);
        Vector2 controlViewPos = GetViewPos(pointPos + dir);

        Vector2 distanceFromPoint = _tangentLength * (controlViewPos - pointViewPos).Normalized();
        Vector2 tangentViewPos = pointViewPos + distanceFromPoint;

        float fractionInside = 1.0f;
        if (distanceFromPoint.X != 0.0f)
            fractionInside = Mathf.Min(fractionInside, ((distanceFromPoint.X > 0 ? Size.X : 0) - pointViewPos.X) / distanceFromPoint.X);
        if (distanceFromPoint.Y != 0.0f)
            fractionInside = Mathf.Min(fractionInside, ((distanceFromPoint.Y > 0 ? Size.Y : 0) - pointViewPos.Y) / distanceFromPoint.Y);

        if (fractionInside < 1.0f && fractionInside > 0.5f)
            tangentViewPos = pointViewPos + distanceFromPoint * fractionInside;

        return tangentViewPos;
    }

    private Vector2 GetViewPos(Vector2 worldPos)
    {
        return _worldToView * worldPos;
    }

    private Vector2 GetWorldPos(Vector2 viewPos)
    {
        return _worldToView.AffineInverse() * viewPos;
    }

    private void PlotCurveAccurate(float step, Color lineColor, Color edgeLineColor)
    {
        if (_curve.Count <= 1)
        {
            float y = _curve.SampleX(0);
            DrawLine(GetViewPos(new Vector2(MinDomain, y)) + new Vector2(0.5f, 0), GetViewPos(new Vector2(MaxDomain, y)) - new Vector2(1.5f, 0), lineColor, LineWidth, true);
            return;
        }

        Vector2 firstPoint = _curve.GetPointPosition(0);
        Vector2 lastPoint = _curve.GetPointPosition(_curve.Count - 1);

        // DrawLine(GetViewPos(new Vector2(MinDomain, firstPoint.Y)) + new Vector2(0.5f, 0), GetViewPos(firstPoint), edgeLineColor, LineWidth, true);
        // DrawLine(GetViewPos(lastPoint), GetViewPos(new Vector2(MaxDomain, lastPoint.Y)) - new Vector2(1.5f, 0), edgeLineColor, LineWidth, true);

        int nSample = 128;
        List<Vector2> samples = new(nSample);
        for (int i = 0; i < nSample; i++)
        {
            float x = MinDomain + i * (DomainRange / (nSample-1));
            samples.Add(new(x, _curve.SampleX(x)));
        }
        for (int i = 1; i < nSample; i++)
        {
            DrawLine(GetViewPos(samples[i - 1]), GetViewPos(samples[i]), lineColor, LineWidth, true);
        }

        // for (int i = 1; i < _curve.Count; i++)
        // {
        //     Vector2 a = _curve.GetPointPosition(i - 1);
        //     Vector2 b = _curve.GetPointPosition(i);
        //
        //     Vector2 pos = a;
        //     Vector2 prevPos = a;
        //
        //     float samples = (b.X - a.X) / worldStepSize;
        //
        //     for (int j = 1; j < samples; j++)
        //     {
        //         float x = j * worldStepSize;
        //         pos.X = a.X + x;
        //         pos.Y = _curve.SampleX(x);
        //         GD.Print(pos.Y);
        //         DrawLine(GetViewPos(prevPos), GetViewPos(pos), lineColor, LineWidth, true);
        //         prevPos = pos;
        //     }
        //
        //     DrawLine(GetViewPos(prevPos), GetViewPos(b), lineColor, LineWidth, true);
        // }
    }

    public override void _Draw()
    {
        if (_curve == null)
            return;

        UpdateViewTransform();

        // Draw background
        DrawStyleBox(GetThemeStylebox("panel", "Tree"), new Rect2(Vector2.Zero, Size));

        // Draw primary grid
        DrawSetTransformMatrix(_worldToView);

        Vector2 minEdge = GetWorldPos(new Vector2(0, Size.Y));
        Vector2 maxEdge = GetWorldPos(new Vector2(Size.X, 0));

        Color gridColorPrimary = GetThemeColor("font_color", "Label") * new Color(1, 1, 1, 0.25f);
        Color gridColor = GetThemeColor("font_color", "Label") * new Color(1, 1, 1, 0.1f);

        Vector2I gridSteps = new Vector2I(4, 2);
        Vector2 stepSize = new Vector2(DomainRange, ValueRange) / gridSteps;

        DrawLine(new Vector2(minEdge.X, MinValue), new Vector2(maxEdge.X, MinValue), gridColorPrimary);
        DrawLine(new Vector2(maxEdge.X, MaxValue), new Vector2(minEdge.X, MaxValue), gridColorPrimary);
        DrawLine(new Vector2(MinDomain, minEdge.Y), new Vector2(MinDomain, maxEdge.Y), gridColorPrimary);
        DrawLine(new Vector2(MaxDomain, maxEdge.Y), new Vector2(MaxDomain, minEdge.Y), gridColorPrimary);

        for (int i = 1; i < gridSteps.X; i++)
        {
            float x = MinDomain + i * stepSize.X;
            DrawLine(new Vector2(x, minEdge.Y), new Vector2(x, maxEdge.Y), gridColor);
        }

        for (int i = 1; i < gridSteps.Y; i++)
        {
            float y = MinValue + i * stepSize.Y;
            DrawLine(new Vector2(minEdge.X, y), new Vector2(maxEdge.X, y), gridColor);
        }

        // Draw number markings
        DrawSetTransformMatrix(Transform2D.Identity);

        Font font = GetThemeFont("font", "Label");
        int fontSize = GetThemeFontSize("font_size", "Label");
        float fontHeight = font.GetHeight(fontSize);
        Color textColor = GetThemeColor("font_color", "Label");

        int pad = (int)Mathf.Round(2);

        for (int i = 0; i <= gridSteps.X; i++)
        {
            float x = MinDomain + i * stepSize.X;
            DrawString(font, GetViewPos(new Vector2(x, MinValue)) + new Vector2(pad, fontHeight - pad), x.ToString("F2"), HorizontalAlignment.Center, -1, fontSize, textColor);
        }

        for (int i = 0; i <= gridSteps.Y; i++)
        {
            float y = MinValue + i * stepSize.Y;
            DrawString(font, GetViewPos(new Vector2(MinDomain, y)) + new Vector2(pad, -pad), y.ToString("F2"), HorizontalAlignment.Left, -1, fontSize, textColor);
        }

        // Draw curve
        Color lineColor = GetThemeColor("font_color", "Label");
        Color edgeLineColor = GetThemeColor("font_color", "Label") * new Color(1, 1, 1, 0.75f);

        PlotCurveAccurate(StepSize, lineColor, edgeLineColor);

        // Draw points
        bool altPressed = Input.IsKeyPressed(Key.Alt);

        Color pointColor = GetThemeColor("font_color", "Label");

        for (int i = 0; i < _curve.Count; i++)
        {
            Vector2 pos = GetViewPos(_curve.GetPointPosition(i));
            if (_selectedIndex != i)
            {
                DrawRect(new Rect2(pos, Vector2.Zero).Grow(_pointRadius), pointColor);
            }
            if (_hoveredIndex == i && _hoveredTangentIndex == TangentIndex.None)
            {
                DrawRect(new Rect2(pos, Vector2.Zero).Grow(_hoverRadius - Mathf.Round(3)), lineColor, false, Mathf.Round(1));
            }
        }

        // Draw selected point and tangents
        if (_selectedIndex >= 0)
        {
            Vector2 pointPos = _curve.GetPointPosition(_selectedIndex);
            Color selectedPointColor = GetThemeColor("font_color", "Label");

            if (_grabbing == GrabMode.None || _initialGrabPos == pointPos || _selectedTangentIndex != TangentIndex.None)
            {
                Color selectedTangentColor = GetThemeColor("font_color", "Label").Darkened(0.25f);
                Color tangentColor = GetThemeColor("font_color", "Label").Darkened(0.25f);

                if (_selectedIndex != 0)
                {
                    Vector2 controlPos = GetTangentViewPos(_selectedIndex, TangentIndex.Left);
                    Color leftTangentColor = _selectedTangentIndex == TangentIndex.Left ? selectedTangentColor : tangentColor;

                    DrawLine(GetViewPos(pointPos), controlPos, leftTangentColor, 0.5f, true);
                    // if (_curve.GetPointLeftMode(_selectedIndex) == Curve.TangentMode.Free)
                    //     DrawCircle(controlPos, _tangentRadius, leftTangentColor);
                    // else
                        DrawRect(new Rect2(controlPos, Vector2.Zero).Grow(_tangentRadius), leftTangentColor);

                    // if (_hoveredTangentIndex == TangentIndex.Left || (_hoveredTangentIndex == TangentIndex.Right && !altPressed && _curve.GetPointLeftMode(_selectedIndex) != Curve.TangentMode.Linear))
                    if (_hoveredTangentIndex == TangentIndex.Left || (_hoveredTangentIndex == TangentIndex.Right && !altPressed))
                    {
                        DrawRect(new Rect2(controlPos, Vector2.Zero).Grow(_tangentHoverRadius - Mathf.Round(3)), tangentColor, false, Mathf.Round(1));
                    }
                }

                if (_selectedIndex != _curve.Count - 1)
                {
                    Vector2 controlPos = GetTangentViewPos(_selectedIndex, TangentIndex.Right);
                    Color rightTangentColor = _selectedTangentIndex == TangentIndex.Right ? selectedTangentColor : tangentColor;

                    DrawLine(GetViewPos(pointPos), controlPos, rightTangentColor, 0.5f, true);
                    // if (_curve.GetPointRightMode(_selectedIndex) == Curve.TangentMode.Free)
                    //     DrawCircle(controlPos, _tangentRadius, rightTangentColor);
                    // else
                        DrawRect(new Rect2(controlPos, Vector2.Zero).Grow(_tangentRadius), rightTangentColor);

                    // if (_hoveredTangentIndex == TangentIndex.Right || 
                    //     (_hoveredTangentIndex == TangentIndex.Left && 
                    //      !altPressed && _curve.GetPointRightMode(_selectedIndex) != Curve.TangentMode.Linear))
                    if (_hoveredTangentIndex == TangentIndex.Right || (_hoveredTangentIndex == TangentIndex.Left && !altPressed))
                    {
                        DrawRect(new Rect2(controlPos, Vector2.Zero).Grow(_tangentHoverRadius - Mathf.Round(3)), tangentColor, false, Mathf.Round(1));
                    }
                }
            }

            DrawRect(new Rect2(GetViewPos(pointPos), Vector2.Zero).Grow(_pointRadius), selectedPointColor);
        }

        // Draw help Samsung Electronics
        if (_selectedIndex > 0 && _selectedIndex < _curve.Count - 1 && _selectedTangentIndex == TangentIndex.None && _hoveredTangentIndex != TangentIndex.None && !altPressed)
        {
            float width = Size.X - 50;
            textColor.A *= 0.4f;
            DrawMultilineString(font, new Vector2(25, fontHeight - Mathf.Round(2)), "Hold Shift to edit tangents individually", HorizontalAlignment.Center, width, fontSize, -1, textColor);
        }
        else if (_selectedIndex != -1 && _selectedTangentIndex == TangentIndex.None)
        {
            Vector2 pointPos = _curve.GetPointPosition(_selectedIndex);
            float width = Size.X - 50;
            textColor.A *= 0.8f;
            DrawString(font, new Vector2(25, fontHeight - Mathf.Round(2)), $"({pointPos.X:F2}, {pointPos.Y:F2})", HorizontalAlignment.Center, width, fontSize, textColor);
        }
        else if (_selectedIndex != -1 && _selectedTangentIndex != TangentIndex.None)
        {
            float width = Size.X - 50;
            textColor.A *= 0.8f;
            float theta = Mathf.RadToDeg(Mathf.Atan(_selectedTangentIndex == TangentIndex.Left ? -_curve.GetPointInTangent(_selectedIndex) : _curve.GetPointOutTangent(_selectedIndex)));
            DrawString(font, new Vector2(25, fontHeight - Mathf.Round(2)), $"{theta:F1} °", HorizontalAlignment.Center, width, fontSize, textColor);
        }

        // Draw constraints
        DrawSetTransformMatrix(_worldToView);

        if (Input.IsKeyPressed(Key.Alt) && _grabbing != GrabMode.None && _selectedTangentIndex == TangentIndex.None)
        {
            float prevPointOffset = _selectedIndex > 0 ? _curve.GetPointPosition(_selectedIndex - 1).X : MinDomain;
            float nextPointOffset = _selectedIndex < _curve.Count - 1 ? _curve.GetPointPosition(_selectedIndex + 1).X : MaxDomain;

            DrawLine(new Vector2(prevPointOffset, MinValue), new Vector2(prevPointOffset, MaxValue), new Color(pointColor, 0.6f));
            DrawLine(new Vector2(nextPointOffset, MinValue), new Vector2(nextPointOffset, MaxValue), new Color(pointColor, 0.6f));
        }

        if (altPressed && _grabbing != GrabMode.None && _selectedTangentIndex == TangentIndex.None)
        {
            DrawLine(new Vector2(_initialGrabPos.X, MinValue), new Vector2(_initialGrabPos.X, MaxValue), GetThemeColor("font_color", "Label").Darkened(0.4f));
            DrawLine(new Vector2(MinDomain, _initialGrabPos.Y), new Vector2(MaxDomain, _initialGrabPos.Y), GetThemeColor("font_color", "Label").Darkened(0.4f));
        }
    }
}