using System;
using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
using Godot;

namespace Ciallo.Tool;

public class ImageTransformHover : InteractiveSessionBase
{
    public Body RotationBody;
    public Body TranslationBody;
    public Body[] CornerBodies = [];

    public override void Start(CursorButtonData data)
    {
        var setting = WorkingLayer.Get<ImageLayerSetting>();
        var worldBody = Document.Get<WorldBody>();

        WorkingLayer.Get<TransformOverlayBox>().Visible = true;

        // Create bodies
        Body[] bodies = worldBody.CreateAddTransformAreas(setting.ImageSize, setting.ImageTransform.Value);
        RotationBody = bodies[0];
        TranslationBody = bodies[1];
        CornerBodies = bodies[2..6];
    }

    public override void Interacting(CursorMotionData data) { }
    public override void End(CursorButtonData data) => Cancel();
    public override void Cancel()
    {
        RotationBody.QueueFree();
        TranslationBody.QueueFree();

        Array.ForEach(CornerBodies, b => b.QueueFree());
        RotationBody = null;
        TranslationBody = null;
        CornerBodies = [];
        WorkingLayer.Get<TransformOverlayBox>().Visible = false;
    }

    public override bool OnKey(InputEventKey key, CursorButtonData data)
    {
        return false;
    }
}