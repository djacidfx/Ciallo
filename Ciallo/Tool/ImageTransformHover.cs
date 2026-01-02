using System;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class ImageTransformHover : InteractiveSessionBase
{
    public Body RotationArea;
    public Body TranslationArea;
    public Body[] CornerAreas = [];

    public override void Start(CursorButtonData data)
    {
        var setting = WorkingLayer.Get<ImageLayerSetting>();
        var manager = Document.Get<WorldBody>();

        WorkingLayer.Get<TransformOverlayBox>().Visible = true;

        // Create areas
        Body[] areas = manager.CreateAddTransformAreas(setting.ImageSize, setting.ImageTransform.Value);
        RotationArea = areas[0];
        TranslationArea = areas[1];
        CornerAreas = areas[2..6];
    }

    public override void Interacting(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        RotationArea.QueueFree();
        TranslationArea.QueueFree();

        Array.ForEach(CornerAreas, b => b.QueueFree());
        RotationArea = null;
        TranslationArea = null;
        CornerAreas = [];
        WorkingLayer.Get<TransformOverlayBox>().Visible = false;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data)
    {
        return false;
    }
}