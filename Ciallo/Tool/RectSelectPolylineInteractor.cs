using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Misc;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class RectSelectPolylineInteractor : InteractiveSessionBase
{
    private StrokeView _boxSelectionDash;
    private Rect2 _boxSelectionRect;

    public override void Start(CursorButtonData data)
    {
        _boxSelectionDash = new StrokeView();
        _boxSelectionDash.Material = AutoloadRendering.DashWireframeMaterial;
        Document.Get<WorldOverlay>().AddChild(_boxSelectionDash);
        _boxSelectionRect.Position = data.WorldPosition;
        _boxSelectionRect.Size = Vector2.Zero;
    }

    public override void Interacting(CursorMotionData data)
    {
        _boxSelectionRect.Size = data.WorldPosition - _boxSelectionRect.Position;
        var points = _boxSelectionRect.GetCorners();
        _boxSelectionDash.SetGeometry([..points, points[0]], AppPreference.StrokeWireframeRadius);
    }

    public override void End(CursorButtonData data)
    {
        var worldBody = Document.Get<WorldBody>();
        var es = worldBody.RectQuery(_boxSelectionRect);
        var selectionManager = Document.Get<SelectionManager>();
        if (Input.IsKeyPressed(Key.Shift))
        {
            foreach (var e in es)
            {
                if (!selectionManager.SelectedPolylines.Remove(e))
                    selectionManager.SelectedPolylines.Add(e);
            }
        }
        else
        {
            selectionManager.SelectedPolylines.Clear();
            selectionManager.SelectedPolylines.AddRange(es);
        }

        Clear();
    }

    public override void Cancel()
    {
        Clear();
    }

    public void Clear()
    {
        _boxSelectionDash.QueueFree();
        _boxSelectionDash = null;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => true;
}