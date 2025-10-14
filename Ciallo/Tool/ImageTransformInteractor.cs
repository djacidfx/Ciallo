using Ciallo.Data;
using Ciallo.NodeControl;
using Godot;

namespace Ciallo.Tool;

public class ImageTransformInteractor : InteractorBase
{
    private readonly ImageEditHover _hover;
    private int TransformType = -1; // 0: Rotate, 1: Move, 2~5: Corner Resize

    public override bool CanInteract
    {
        get
        {
            if (_hover.RotationButton == null || _hover.MoveButton == null || _hover.CornerButtons.Length == 0)
                return false;

            if (_hover.RotationButton.IsHovered())
            {
                TransformType = 0;
                return true;
            }
            // if (_hover.MoveButton.IsHovered())
            // {
            //     TransformType = 1;
            //     return true;
            // }
            // for (int i = 0; i < _hover.CornerButtons.Length; i++)
            // {
            //     if (_hover.CornerButtons[i].IsHovered())
            //     {
            //         TransformType = 2 + i;
            //         return true;
            //     }
            // }
            return false;
        }
    }

    private ImageLayerSetting _setting;
    private Vector2 _startPos;
    private Transform2D _startTransform;

    public ImageTransformInteractor(ImageEditHover hover)
    {
        _hover = hover;
    }

    public override void Start(CursorButtonData data)
    {
        _setting = SelectionManager.WorkingLayer.Value.Get<ImageLayerSetting>();
        Vector2 center = _setting.Position;
        _startPos = data.WorldPosition;
        _startTransform = _setting.ImageTransform.Value;
    }

    public override void Interacting(CursorMotionData data)
    {
        if (TransformType == 0)
        {
            var startAngle = (_startPos - _setting.Position).Angle();
            var currentAngle = (data.WorldPosition - _setting.Position).Angle();
            var deltaAngle = currentAngle - startAngle;
            _setting.ImageTransform.Value = _startTransform.Rotated(deltaAngle); 
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
        TransformType = -1;
    }
}