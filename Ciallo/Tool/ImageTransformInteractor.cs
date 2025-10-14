using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

public class ImageTransformInteractor : InteractorBase
{
    private readonly ImageEditHover _hover;
    private int _transformType = -1; // 0: Rotate, 1: Move, 2~5: Corner Resize

    public override bool CanInteract
    {
        get
        {
            if (_hover.RotationButton == null || _hover.MoveButton == null || _hover.CornerButtons.Length == 0)
                return false;

            if (_hover.RotationButton.IsHovered())
            {
                _transformType = 0;
                return true;
            }
            if (_hover.MoveButton.IsHovered())
            {
                _transformType = 1;
                return true;
            }
            for (int i = 0; i < _hover.CornerButtons.Length; i++)
            {
                if (_hover.CornerButtons[i].IsHovered())
                {
                    _transformType = 2 + i;
                    return true;
                }
            }
            return false;
        }
    }

    private ImageLayerSetting _setting;
    private Vector2 _startPos;
    private Transform2D _startTransform;
    // Store starting corners so we can anchor the opposite one during resize
    private Vector2[] _startCorners = [];

    public ImageTransformInteractor(ImageEditHover hover)
    {
        _hover = hover;
    }

    public override void Start(CursorButtonData data)
    {
        _setting = SelectionManager.WorkingLayer.Value.Get<ImageLayerSetting>();
        _startPos = data.WorldPosition;
        _startTransform = _setting.ImageTransform.Value;
        _startCorners = _setting.GetCorners();
    }

    public override void Interacting(CursorMotionData data)
    {
        if (_transformType == 0)
        {
            var startAngle = (_startPos - _setting.Position).Angle();
            var currentAngle = (data.WorldPosition - _setting.Position).Angle();
            var deltaAngle = currentAngle - startAngle;
            _setting.ImageTransform.Value = _startTransform
                .Translated(-_startTransform.Origin)
                .Rotated(deltaAngle)
                .Translated(_startTransform.Origin);
        }

        if (_transformType == 1)
        {
            _setting.ImageTransform.Value = _startTransform.Translated(data.WorldPosition - _startPos);
        }

        if (_transformType >= 2)
        {
            // Gen by copilot, GPT-5 do this correctly.
            // Corner resize. Keep the opposite corner fixed, adjust center and scale along the original axes.
            var cornerIndex = _transformType - 2; // 0..3
            var oppositeIndex = (cornerIndex + 2) & 3; // (index + 2) % 4
            var fixedCorner = _startCorners[oppositeIndex];
            var draggedPos = data.WorldPosition;

            // New center is midpoint between fixed corner and current pointer.
            var newCenter = (fixedCorner + draggedPos) * 0.5f;

            // Axis directions from original transform (normalized)
            var axisXDir = _startTransform.X.Normalized();
            var axisYDir = _startTransform.Y.Normalized();

            // Vector from center to dragged corner (half diagonal in world space)
            var halfDiag = draggedPos - newCenter;

            // Project half diagonal onto axes to get new half sizes in world along each axis
            var newHalfXWorld = Mathf.Abs(halfDiag.Dot(axisXDir));
            var newHalfYWorld = Mathf.Abs(halfDiag.Dot(axisYDir));

            // Original half extents in local space (image pixel space)
            var startHalfLocal = _setting.ImageSize * 0.5f;

            // New scale lengths for basis vectors (world length per 1 local unit)
            // newHalfXWorld = startHalfLocal.X * scaleXLength  => scaleXLength = newHalfXWorld / startHalfLocal.X
            var newScaleXLength = newHalfXWorld / startHalfLocal.X;
            var newScaleYLength = newHalfYWorld / startHalfLocal.Y;

            // Compose new basis vectors maintaining rotation
            var newX = axisXDir * newScaleXLength;
            var newY = axisYDir * newScaleYLength;

            _setting.ImageTransform.Value = new Transform2D(newX, newY, newCenter);
        }
    }

    public override void End(CursorButtonData data)
    {
        Clear();
    }

    public override void Cancel()
    {
        _setting.ImageTransform.Value = _startTransform;
        Clear();
    }

    public void Clear()
    {
        _transformType = -1;
    }
}