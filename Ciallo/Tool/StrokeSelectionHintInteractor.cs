using Arch.Core;
using Arch.Core.Extensions;
using Ciallo.Command;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class StrokeSelectionHintInteractor : InteractorBase
{
    private StrokeOverlay _hintingOverlay;

    public override bool CanInteract => SelectionManager.WorkingLayer != Entity.Null;
    
    public override void Interacting(CursorMotionData data)
    {
        // See 2D ray cast for the method:
        // https://docs.godotengine.org/en/stable/tutorials/physics/ray-casting.html
        // https://godotforums.org/d/34175-collision-with-point
        var pp = new PhysicsPointQueryParameters2D()
        {
            CollideWithBodies = true,
            Position = data.WorldPosition,
            CollisionMask = (uint)AppLayers.Physics2DLayerMask.Stroke
        };
        var points = Document.Get<WorldOverlay>().GetWorld2D().DirectSpaceState.IntersectPoint(pp, 1);
        if(points.Count > 0)
        {
            var hit = points[0];
            var collider = (Node)hit["collider"];
            var overlay = (StrokeOverlay)collider.GetParent();
            if(overlay != _hintingOverlay)
            {
                _hintingOverlay?.SetColor(AppPreferences.StrokeWireframeColor);
                overlay.SetColor(AppPreferences.StrokeWireframeHintColor);
                _hintingOverlay = overlay;
            }
        }
        else
        {
            _hintingOverlay?.SetColor(AppPreferences.StrokeWireframeColor);
            _hintingOverlay = null;
        }
    }

    public override void End(CursorButtonData _)
    {
        _hintingOverlay?.SetColor(AppPreferences.StrokeWireframeColor);
        _hintingOverlay = null;
    }

    public override void Cancel()
    {
        _hintingOverlay?.SetColor(AppPreferences.StrokeWireframeColor);
        _hintingOverlay = null;
    }
}