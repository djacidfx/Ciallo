using Ciallo.Data;
using Frent;
using Frent.Components;
using Godot;
using R3;

namespace Ciallo.GuiControl;

[SceneTree, Instantiable(init: "Initialize")]
public partial class PaintPanel : PanelContainer, IInitable
{
    public readonly ReactiveProperty<float> CameraZoom = new(1f);
    public readonly ReactiveProperty<float> CameraRotation = new(0f);
    public readonly ReactiveProperty<Vector2> CameraOffset = new(Vector2.Zero);

    public void Init(Entity document)
    {
        WorldEventDispatcher.Document = document;

        var setting = document.Get<DocumentSetting>();

        float w = setting.ReferenceSize.Value.X, h = setting.ReferenceSize.Value.Y;
        Background.Polygon = [new(-w / 2, -h / 2), new(w / 2, -h / 2), new(w / 2, h / 2), new(-w / 2, h / 2)];

        CameraZoom.Subscribe(v => MainCamera.Zoom = Vector2.One * v).AddTo(this);
        CameraRotation.Subscribe(v => MainCamera.Rotation = v).AddTo(this);
        CameraOffset.Subscribe(v => MainCamera.Position = v).AddTo(this);
        setting.BackgroundColor.Subscribe(Background.SetColor).AddTo(this);

        ZoomControl.BindNumber(CameraZoom);
        var degCanvasRotation = CameraRotation.Project(
            rad => -Mathf.RadToDeg(rad),
            deg => -Mathf.DegToRad(deg)
        ).AddTo(RotationControl);
        RotationControl.BindNumber(degCanvasRotation);
        BackgroundColorControl.BindColor(setting.BackgroundColor);
    }
}