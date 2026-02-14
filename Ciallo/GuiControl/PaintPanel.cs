using Ciallo.Data;
using Ciallo.GuiBinding;
using Ciallo.Misc;
using Frent;
using Frent.Components;
using Godot;
using R3;

namespace Ciallo.GuiControl;

[SceneTree]
public partial class PaintPanel : PanelContainer, IInitable
{
    public readonly ReactiveProperty<float> CameraZoom = new(1f);
    public readonly ReactiveProperty<float> CameraRotation = new(0f);
    public readonly ReactiveProperty<Vector2> CameraOffset = new(Vector2.Zero);

    public void Init(Entity self) => WorldEventDispatcher.Document = self;

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

        CameraZoom.Subscribe(v => MainCamera.Zoom = Vector2.One * v).AddTo(this);
        CameraRotation.Subscribe(v => MainCamera.Rotation = v).AddTo(this);
        CameraOffset.Subscribe(v => MainCamera.Position = v).AddTo(this);
        _documentSetting.BackgroundColor.Subscribe(_background.SetColor).AddTo(this);

        ZoomControl.BindNumber(CameraZoom);
        var degCanvasRotation = CameraRotation.Project(
            rad => -Mathf.RadToDeg(rad),
            deg => -Mathf.DegToRad(deg),
            out var sub
        );
        sub.AddTo(RotationControl);
        RotationControl.BindNumber(degCanvasRotation);
        BackgroundColorControl.BindColor(_documentSetting.BackgroundColor);
    }
}