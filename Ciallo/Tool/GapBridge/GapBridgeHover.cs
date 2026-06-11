using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class GapBridgeHover : InteractiveSessionBase
{
    public new GapBridgeTool Tool => (GapBridgeTool)base.Tool;

    private Vector2 _lastWorldPosition = Vector2.Inf;
    private GapBridgeTarget _hoveredTarget;
    private bool _hasHoveredTarget;

    public override void Start(CursorButtonData data) => RefreshHover(data.WorldPosition);
    public override void Moving(CursorMotionData data) => RefreshHover(data.WorldPosition);
    public override void End(CursorButtonData data) => Cancel();

    public override void Cancel()
    {
        _lastWorldPosition = Vector2.Inf;
        _hasHoveredTarget = false;
        Document.Get<WorldBody>().DefaultCursorShape = default;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data) => false;

    public bool TryGetHoveredTarget(out GapBridgeTarget target)
    {
        target = _hoveredTarget;
        return _hasHoveredTarget;
    }

    public bool RefreshHover(Vector2 worldPosition)
    {
        _lastWorldPosition = worldPosition;
        RefreshTarget();
        ApplyCursor();
        return _hasHoveredTarget;
    }

    public void RefreshCursor()
    {
        RefreshTarget();
        ApplyCursor();
    }

    private void RefreshTarget()
    {
        _hasHoveredTarget = false;
        if (!IsFinite(_lastWorldPosition))
            return;

        _hasHoveredTarget = Tool.TryPickTarget(_lastWorldPosition, out _hoveredTarget);
    }

    private void ApplyCursor()
    {
        var body = Document.Get<WorldBody>();
        if (Tool.Arrangement?.ArrReady.CurrentValue == null)
        {
            body.DefaultCursorShape = Control.CursorShape.Wait;
            return;
        }

        body.DefaultCursorShape = _hasHoveredTarget
            ? Control.CursorShape.PointingHand
            : default;
    }

    private static bool IsFinite(Vector2 v) => float.IsFinite(v.X) && float.IsFinite(v.Y);
}
