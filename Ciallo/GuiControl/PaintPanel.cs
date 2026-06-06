using Ciallo.Data;
using Ciallo.Geometry;
using Ciallo.Rendering;
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
    public readonly ReactiveProperty<bool> MirrorHorizontal = new(false);
    public readonly ReactiveProperty<bool> MirrorVertical = new(false);

    public void Init(Entity document)
    {
        WorldEventDispatcher.Document = document;

        var setting = document.Get<DocumentSetting>();

        float w = setting.ReferenceSize.Value.X, h = setting.ReferenceSize.Value.Y;
        Background.SetPolygonFromRawRing(new Vector2[] { new(-w / 2, -h / 2), new(w / 2, -h / 2), new(w / 2, h / 2), new(-w / 2, h / 2) });

        CameraZoom.Subscribe(_ => ApplyCameraZoom()).AddTo(this);
        CameraRotation.Subscribe(v => MainCamera.Rotation = v).AddTo(this);
        CameraOffset.Subscribe(v => MainCamera.Position = v).AddTo(this);
        MirrorHorizontal.Subscribe(_ => ApplyCameraZoom()).AddTo(this);
        MirrorVertical.Subscribe(_ => ApplyCameraZoom()).AddTo(this);
        setting.BackgroundColor.Subscribe(Background.SetColor).AddTo(this);

        ZoomControl.BindNumber(CameraZoom);
        var degCanvasRotation = CameraRotation.Project(
            rad => -Mathf.RadToDeg(rad),
            deg => -Mathf.DegToRad(deg)
        ).AddTo(RotationControl);
        RotationControl.BindNumber(degCanvasRotation);
        BackgroundColorControl.BindColor(setting.BackgroundColor);
        MirrorHorizontal.Subscribe(MirrorHorizontalButton.SetPressedNoSignal).AddTo(this);
        MirrorHorizontalButton.OnToggledAsObservable().Subscribe(v => MirrorHorizontal.Value = v).AddTo(this);
        MirrorVertical.Subscribe(MirrorVerticalButton.SetPressedNoSignal).AddTo(this);
        MirrorVerticalButton.OnToggledAsObservable().Subscribe(v => MirrorVertical.Value = v).AddTo(this);
    }

    private void ApplyCameraZoom()
    {
        var zoom = CameraZoom.Value;
        MainCamera.Zoom = new Vector2(
            MirrorHorizontal.Value ? -zoom : zoom,
            MirrorVertical.Value ? -zoom : zoom
        );
    }
}
