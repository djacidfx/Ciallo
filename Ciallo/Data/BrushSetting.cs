using System.Runtime.Serialization;
using Ciallo.Geometry;
using Ciallo.Misc;
using Ciallo.Tool;
using Ciallo.Widget;
using Godot;
using R3;
using MessagePack;
using ObservableCollections;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushSetting : IPropertySource
{
    public static readonly Shader StrokeShader = GD.Load<Shader>("res://Rendering/Stroke.gdshader");

    [DataMember] public readonly ReactiveProperty<string> Name = new(TranslationServer.Translate(""));
    [DataMember] public readonly ObservableList<BrushLabel> Labels = [];
    [DataMember] public readonly ReactiveProperty<Color> Color = new(Colors.Black); // RGB+Flow
    [DataMember] public readonly BezierCurve Pressure2RadiusRatioCurve = BezierCurve.Linear(); // radius = baseRadius * curve(pressure)
    [DataMember] public readonly ReactiveProperty<BrushRenderingType> RenderingType = new(BrushRenderingType.Stamp);

    // Vanilla
    [DataMember] public ReactiveProperty<float> DashLength = new(-1.0f);
    [DataMember] public ReactiveProperty<float> GapLength = new(-1.0f);
    [DataMember] public ReactiveProperty<float> DashForwardSpeed = new(0.0f);
    
    // Stamp
    [DataMember] public ReactiveProperty<float> StampInterval = new(0.4f); // in radius unit
    
    // Airbrush
    [DataMember] public BezierCurve FalloffCurve = BezierCurve.Linear(1.0f, 0.0f);
    
    public void DrawProperty(PropertyContainer container)
    {
        var nameEdit = new LineEdit();
        nameEdit.BindString(Name).AddTo(nameEdit);
        var box = container.AddPropertyControl("Name", nameEdit);
        Labels.ObserveChanged().Subscribe(_ => box.Visible = Labels.Contains(BrushLabel.BuiltIn)).AddTo(nameEdit);


        var colorPickerButton = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 30),
        };
        var picker = colorPickerButton.GetPicker();
        picker.ColorModesVisible = false;
        picker.ColorMode = ColorPicker.ColorModeType.Rgb;
        picker.HexVisible = false;
        colorPickerButton.BindColor(Color).AddTo(colorPickerButton);
        container.AddPropertyControl("RGB+Flow", colorPickerButton);

        var pressureCurveEdit = new MappingCurveEdit();
        pressureCurveEdit.Curve = Pressure2RadiusRatioCurve;
        container.AddPropertyControl("Pen pressure", pressureCurveEdit);

        var typeButton = new OptionButton();
        typeButton.BindEnum(RenderingType).AddTo(typeButton);
        container.AddPropertyControl("Rendering type", typeButton);
        
        var falloffCurveEdit = new MappingCurveEdit();
        falloffCurveEdit.Curve = FalloffCurve;
        container.AddPropertyControl("Opacity falloff", falloffCurveEdit)
            .VisibleIf(RenderingType, BrushRenderingType.Airbrush).AddTo(falloffCurveEdit);
    }
    
    public ShaderMaterial CreateBoundBrushMaterial(out CompositeDisposable subs)
    {
        subs = new();
        var material = new ShaderMaterial
        {
            Shader = StrokeShader,
        };
        RenderingType.Subscribe(type => material.SetShaderParameter("strokeType", (int)type)).AddTo(subs);
        Color.Subscribe(color => material.SetShaderParameter("materialColor", color)).AddTo(subs);
        DashLength.Subscribe(length => material.SetShaderParameter("dashLength", length)).AddTo(subs);
        GapLength.Subscribe(length => material.SetShaderParameter("gapLength", length)).AddTo(subs);
        DashForwardSpeed.Subscribe(speed => material.SetShaderParameter("dashForwardSpeed", speed)).AddTo(subs);
        StampInterval.Subscribe(interval => material.SetShaderParameter("stampInterval", interval)).AddTo(subs);
        // TODO: falloff curve.
        
        return material;
    }

    public BrushSetting Clone()
    {
        var bytes = MessagePackSerializer.Serialize(this);
        var setting = MessagePackSerializer.Deserialize<BrushSetting>(bytes);
        return setting;
    }
}

public enum BrushRenderingType
{
    Vanilla = 0,
    Stamp,
    Airbrush,
}

public enum BrushLabel
{
    BuiltIn = 0,
}