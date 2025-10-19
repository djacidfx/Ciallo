using System;
using Ciallo.Data;
using Ciallo.Rendering;
using Frent;

namespace Ciallo.Tool;

public class ImageEditHover : HoverBase
{
    public CursorDetectionArea RotationArea;
    public CursorDetectionArea TranslationArea;
    public CursorDetectionArea[] CornerAreas = [];
    private Entity _layerE;

    public override bool CanInteract
    {
        get
        {
            var layerE = SelectionManager.WorkingLayer.Value;
            return layerE.IsNotNull() && layerE.Has<ImageLayerSetting>();
        }
    }

    public override void Start()
    {
        _layerE = SelectionManager.WorkingLayer.Value;
        var setting = _layerE.Get<ImageLayerSetting>();
        var manager = Document.Get<WorldCursorDetectionArea>();

        _layerE.Get<TransformOverlayBox>().Visible = true;

        // Create areas
        CursorDetectionArea[] areas = manager.CreateAddTransformAreas(setting.ImageSize, setting.ImageTransform.Value);
        RotationArea = areas[0];
        TranslationArea = areas[1];
        CornerAreas = areas[2..6];
    }

    public override void End()
    {
        RotationArea.QueueFree();
        TranslationArea.QueueFree();

        Array.ForEach(CornerAreas, b => b.QueueFree());
        RotationArea = null;
        TranslationArea = null;
        CornerAreas = [];
        _layerE.Get<TransformOverlayBox>().Visible = false;
        _layerE = Entity.Null;
    }
}