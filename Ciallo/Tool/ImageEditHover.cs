using System.Collections.Generic;
using System.Linq;
using Ciallo.Data;
using Ciallo.NodeControl;
using Ciallo.Rendering;
using Godot;
using Massive;

namespace Ciallo.Tool;

public class ImageEditHover : HoverBase
{
    public CursorDetectionArea RotationArea;
    public CursorDetectionArea MoveArea;
    public List<CursorDetectionArea> CornerAreas = [];

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
        var layerE = SelectionManager.WorkingLayer.Value;
        var setting = layerE.Get<ImageLayerSetting>();
        var manager = Document.Get<WorldArea>();

        // Rotation button
        RotationArea = manager.AddRect(setting.Position, setting.ImageSize);
        RotationArea.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
        RotationArea.Rotation = setting.Rotation;
        RotationArea.Scale = setting.Scale * 1.2f;

        // Image move button
        MoveArea = manager.AddRect(setting.Position, setting.ImageSize);
        MoveArea.MouseDefaultCursorShape = Control.CursorShape.Drag;
        MoveArea.Rotation = setting.Rotation;
        MoveArea.Scale = setting.Scale;

        // Corner buttons
        var corners = layerE.Get<ImageLayerSetting>().GetCorners();
        var areas = new CursorDetectionArea[corners.Length];
        foreach (var (idx, pos) in corners.Index())
        {
            var a = manager.AddRect(pos, 100.0f / 3, CursorRectFlags.ScreenSize);
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
    }
}