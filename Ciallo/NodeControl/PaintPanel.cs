using Godot;
using System;
using Ciallo.Data;
using Ciallo.Misc;
using Ciallo.Widget;
using R3;

namespace Ciallo.NodeControl;

public partial class PaintPanel : PanelContainer
{
    private Camera2D _camera;
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
        _camera = GetNode<Camera2D>("%Camera2D");
        _background = GetNode<Polygon2D>("%Background");
        float w = _documentSetting.ReferenceSize.Value.X, h = _documentSetting.ReferenceSize.Value.Y;
        _background.Polygon = [ new(-w/2, -h/2), new(w/2, -h/2), new(w/2, h/2), new(-w/2, h/2) ];
        
        Zoom.Subscribe(v => _camera.Zoom = Vector2.One * v);
        CanvasRotation.Subscribe(v => _camera.Rotation = Mathf.DegToRad(v));
        Offset.Subscribe(v => _camera.Position = v);
        _documentSetting.BackgroundColor.Subscribe(c => _background.Color = c);
        
        GetNode<SpinSlider>("%ZoomControl").BindValue(Zoom).AddTo(this);
        GetNode<SpinSlider>("%RotationControl").BindValue(CanvasRotation).AddTo(this);
        GetNode<ColorPickerButton>("%BackgroundColorControl").BindColor(_documentSetting.BackgroundColor).AddTo(this);
    }
}
