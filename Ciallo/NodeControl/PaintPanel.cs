using Ciallo.Data;
using Ciallo.Misc;
using Godot;
using R3;

namespace Ciallo.NodeControl;

[SceneTree]
public partial class PaintPanel : PanelContainer
{
    public readonly ReactiveProperty<float> Zoom = new(1f);
    public readonly ReactiveProperty<float> CanvasRotation = new(0f); // in deg not rad
    public readonly ReactiveProperty<Vector2> Offset = new(Vector2.Zero);

    private Polygon2D _background;
    private DocumentSetting _documentSetting;

    [OnInstantiate]
    private void Initialise(DocumentSetting setting)
    {
        _documentSetting = setting;
    }

    public override void _Ready()
    {
        _background = GetNode<Polygon2D>("%Background");
        float w = _documentSetting.ReferenceSize.Value.X, h = _documentSetting.ReferenceSize.Value.Y;
        _background.Polygon = [new(-w / 2, -h / 2), new(w / 2, -h / 2), new(w / 2, h / 2), new(-w / 2, h / 2)];

        Zoom.Subscribe(v => MainCamera.Zoom = Vector2.One * v);
        CanvasRotation.Subscribe(v => MainCamera.Rotation = -Mathf.DegToRad(v));
        Offset.Subscribe(v => MainCamera.Position = v);
        _documentSetting.BackgroundColor.Subscribe(_background.SetColor).AddTo(this);

        ZoomControl.BindNumber(Zoom);
        RotationControl.BindNumber(CanvasRotation);
        BackgroundColorControl.BindColor(_documentSetting.BackgroundColor);
    }
}