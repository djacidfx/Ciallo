using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Frent;
using Godot;

namespace Ciallo.Tool;

public class ImageEditHover : HoverBase
{
    public CursorDetectionArea RotationArea;
    public CursorDetectionArea MoveArea;
    public List<CursorDetectionArea> CornerAreas = [];
    private Entity _layerE;

    public override bool CanInteract
    {
        get
        {
            var layerE = SelectionManager.WorkingLayer.Value;
            return layerE.IsNotNull() && layerE.Has<ImageLayerSetting>();
        }
    }

    public override void Start(CursorMotionData data)
    {
        _layerE = SelectionManager.WorkingLayer.Value;
        var setting = _layerE.Get<ImageLayerSetting>();
        var manager = Document.Get<WorldCursorDetectionArea>();

        _layerE.Get<ImageLayerOverlay>().Visible = true;

        // Create areas
        // Rotation
        RotationArea = manager.CreateAddRect(setting.Position, setting.ImageSize);
        RotationArea.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        RotationArea.Rotation = setting.Rotation;
        RotationArea.Scale = setting.Scale * 1.2f;

        // Image move
        MoveArea = manager.CreateAddRect(setting.Position, setting.ImageSize);
        MoveArea.MouseDefaultCursorShape = Control.CursorShape.Drag;
        MoveArea.Rotation = setting.Rotation;
        MoveArea.Scale = setting.Scale;

        // Corner
        var corners = _layerE.Get<ImageLayerSetting>().GetCorners();
        var areas = new CursorDetectionArea[corners.Length];
        foreach (var (idx, pos) in corners.Index())
        {
            var a = manager.CreateAddRect(pos, 100.0f / 3, CursorRectFlags.ScreenSize);
            a.MouseDefaultCursorShape = idx % 2 == 0 ? Control.CursorShape.Fdiagsize : Control.CursorShape.Bdiagsize;
            areas[idx] = a;
        }
        CornerAreas = [..areas];
    }

    public override void Cancel()
    {
        RotationArea.QueueFree();
        MoveArea.QueueFree();
        CornerAreas.ForEach(b => b.QueueFree());
        RotationArea = null;
        MoveArea = null;
        CornerAreas = [];
        _layerE.Get<ImageLayerOverlay>().Visible = false;
        _layerE = Entity.Null;
    }
}