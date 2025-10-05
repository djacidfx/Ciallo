using System.Runtime.Serialization;
using Ciallo.Geometry;
using Ciallo.Misc;
using Ciallo.NodeControl;
using Ciallo.Widget;
using Godot;
using MessagePack;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class BrushSetting
{
    [DataMember] public ReactiveProperty<string> Name = new("");
    [DataMember] public ObservableList<BrushLabel> Labels = [];
    [DataMember] public ReactiveProperty<Color> Color = new(Colors.Black); // RGB+Flow
    [DataMember] public ReactiveProperty<float> BaseRadius = new(8.0f);
    [DataMember] public BezierCurve Pressure2RadiusRatioCurve = BezierCurve.Linear(0.2f, 1.0f); // radius = baseRadius * curve(pressure)
    [DataMember] public ReactiveProperty<BrushRenderingType> RenderingType = new(BrushRenderingType.Stamp);

    // Vanilla
    [DataMember] public ReactiveProperty<float> DashLength = new(-1.0f);
    [DataMember] public ReactiveProperty<float> GapLength = new(-1.0f);
    [DataMember] public ReactiveProperty<float> DashForwardSpeed = new(0.0f);
    
    // Stamp
    [DataMember] public ReactiveProperty<float> StampInterval = new(0.4f); // in radius unit
    [DataMember] public ImageTexture StampTexture = ImageTexture.CreateFromImage(CreateDefaultWhiteImage());
    [DataMember] public ReactiveProperty<float> StampRotation = new(0.0f); // in radian
    [DataMember] public ImageTexture NoiseTexture = ImageTexture.CreateFromImage(CreateDefaultWhiteImage());

    [DataMember] public ReactiveProperty<FastNoiseLite.NoiseTypeEnum> RotationNoiseType = new(FastNoiseLite.NoiseTypeEnum.Simplex);
    [DataMember] public ReactiveProperty<float> RotationNoiseAmplitude = new(0.0f);
    [DataMember] public ReactiveProperty<float> RotationNoiseFrequency = new(0.01f);


    // Airbrush
    [DataMember] public BezierCurve FalloffCurve = BezierCurve.Linear(1.0f, 0.0f);

    public void DrawProperty(PropertyContainer container)
    {
        var nameEdit = new LineEdit()
        {
            FocusMode = Control.FocusModeEnum.Click,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
        };
        nameEdit.BindString(Name);
        container.AddProperty("Name", nameEdit);
        
        var baseRadiusControl = new SpinSlider
        {
            MinValue = 0.1,
            MaxValue = 256,
            Step = 0.03333333,
            ExpEdit = true,
        };
        baseRadiusControl.BindNumber(BaseRadius);
        container.AddProperty("Base radius", baseRadiusControl);
        
        var colorPickerButton = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 30),
        };
        var picker = colorPickerButton.GetPicker();
        picker.ColorModesVisible = false;
        picker.ColorMode = ColorPicker.ColorModeType.Rgb;
        colorPickerButton.BindColor(Color);
        container.AddProperty("RGB+Flow", colorPickerButton);

        var pressureCurveEdit = new MappingCurveEdit {MinValue = 0.01f}; // MinValue avoid potential zero radius issue.
        pressureCurveEdit.Curve = Pressure2RadiusRatioCurve;
        var aspectBox = new AspectRatioContainer();
        aspectBox.AddChild(pressureCurveEdit);
        container.AddProperty("Pen pressure", aspectBox);

        var typeButton = new OptionButton();
        typeButton.BindEnum(RenderingType);
        container.AddProperty("Rendering type", typeButton);
        
        // Stamp
        var stampIntervalControl = new SpinSlider
        {
            MinValue = 1f/16,
            MaxValue = 6,
            Step = 0.001,
            ExpEdit = true,
            AllowLesser = true,
            AllowGreater = true,
        };
        stampIntervalControl.BindNumber(StampInterval);
        container.AddProperty("Interval", stampIntervalControl)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);
        
        var stampTextureEdit = ImageTextureEdit.Instantiate(StampTexture, ConvertStampImage);
        container.AddProperty("Stamp texture", stampTextureEdit)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);
        
        var noiseTextureEdit = ImageTextureEdit.Instantiate(NoiseTexture, ConvertStampImage);
        container.AddProperty("Noise texture", noiseTextureEdit)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);
        
        var noiseTypeButton = new OptionButton();
        noiseTypeButton.BindEnum(RotationNoiseType);
        container.AddProperty("Rotation noise type", noiseTypeButton)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);

        var stampRotationControl = new SpinSlider
        {
            MinValue = -180,
            MaxValue = 180,
            Step = 0.1,
        };
        var degreeView = StampRotation.Project(Mathf.RadToDeg, Mathf.DegToRad, out var subs);
        subs.AddTo(stampRotationControl);
        stampRotationControl.BindNumber(degreeView);
        container.AddProperty("Rotation", stampRotationControl)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);
        
        var rotationNoiseAmplitudeControl = new SpinSlider
        {
            MinValue = 0,
            MaxValue = Mathf.Pi * 10,
            Step = 0.01,
        };
        rotationNoiseAmplitudeControl.BindNumber(RotationNoiseAmplitude);
        container.AddProperty("Rotation noise amplitude", rotationNoiseAmplitudeControl)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);
        
        var rotationNoiseFrequencyControl = new SpinSlider
        {
            MinValue = 0.0001,
            MaxValue = 0.4,
            Step = 0.0001,
            ExpEdit = true,
        };
        rotationNoiseFrequencyControl.BindNumber(RotationNoiseFrequency);
        container.AddProperty("Rotation noise frequency", rotationNoiseFrequencyControl)
            .VisibleIf(RenderingType, BrushRenderingType.Stamp);
        
        // Airbrush
        var falloffCurveEdit = new MappingCurveEdit();
        falloffCurveEdit.Curve = FalloffCurve;
        container.AddProperty("Opacity falloff", falloffCurveEdit)
            .VisibleIf(RenderingType, BrushRenderingType.Airbrush);
    }

    private static void ConvertStampImage(Image img)
    {
        img.Convert(Image.Format.L8);
        var size = img.GetSize();
        Vector2I maxSize = 256 * Vector2I.One;
        size = size.Min(maxSize);
        if (size.X != size.Y)
        {
            size.X = size.Y = Mathf.Max(size.X, size.Y);
        }
        img.Resize(size.X, size.Y);
    }

    public BrushSetting Clone()
    {
        var bytes = MessagePackSerializer.Serialize(this);
        var setting = MessagePackSerializer.Deserialize<BrushSetting>(bytes); // Note: Fields or properties cannot be readonly
        setting.Labels.Remove(BrushLabel.BuiltIn);
        return setting;
    }

    public static Image CreateDefaultWhiteImage()
    {
        return Image.CreateFromData(1, 1, true, Image.Format.L8, new byte[] { 255 } );
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