using System;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class PolylineRectSelectInteractor : InteractiveSessionBase
{
    private StrokeView _boxSelectionDash;
    private Rect2 _boxSelectionRect;
    private Entity[] _baseSelection;
    private bool _hasMoved;

    public override void Start(CursorButtonData data)
    {
        _boxSelectionDash = new StrokeView();
        _boxSelectionDash.Material = AutoloadRendering.DashWireframeMaterial;
        Document.Get<WorldOverlay>().AddChild(_boxSelectionDash);
        _boxSelectionRect.Position = data.WorldPosition;
        _boxSelectionRect.Size = Vector2.Zero;
        _baseSelection = [..Document.Get<SelectionManager>().SelectedShapes];
        _hasMoved = false;
    }

    public override void Moving(CursorMotionData data)
    {
        // Dash
        _hasMoved = true;
        _boxSelectionRect.Size = data.WorldPosition - _boxSelectionRect.Position;
        var points = _boxSelectionRect.GetCorners();
        _boxSelectionDash.SetGeometry([..points, points[0]], AppPreference.StrokeWireframeRadius);

        // Selection
        var es = Document.Get<WorldBody>().RectQuery(_boxSelectionRect);
        var selectedShapes = Document.Get<SelectionManager>().SelectedShapes;
        if (Input.IsKeyPressed(Key.Shift))
        {
            // XOR: base ∆ rectResults
            selectedShapes.Clear();
            foreach (var e in _baseSelection)
            {
                if (!es.Contains(e))
                    selectedShapes.Add(e);
            }
            foreach (var e in es)
            {
                if (Array.IndexOf(_baseSelection, e) < 0)
                    selectedShapes.Add(e);
            }
        }
        else
        {
            selectedShapes.Clear();
            selectedShapes.AddRange(es);
        }
    }

    public override void End(CursorButtonData data)
    {
        if (!_hasMoved && !Input.IsKeyPressed(Key.Shift))
            Document.Get<SelectionManager>().SelectedShapes.Clear();
        Clear();
    }

    public override void Cancel()
    {
        var selectedShapes = Document.Get<SelectionManager>().SelectedShapes;
        selectedShapes.Clear();
        selectedShapes.AddRange(_baseSelection);
        Clear();
    }

    public void Clear()
    {
        _boxSelectionDash.QueueFree();
        _boxSelectionDash = null;
        _baseSelection = null;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}