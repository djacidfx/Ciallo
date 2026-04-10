using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Ciallo.Geometry;
using Ciallo.Widget;
using Godot;
using MessagePack;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class StrokeBrushSetting
{
    [DataMember] public ReactiveProperty<string> Name = new("");
    [DataMember] public ObservableList<BrushLabel> Labels = [];
    [DataMember] public ReactiveProperty<Color> Color = new(Colors.Black); // RGB+Flow
    [DataMember] public ReactiveProperty<BrushFlags> ActiveBrushFlags = new();
    [DataMember] public ReactiveProperty<float> BaseRadius = new(5.0f);
    [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> Pressure2RadiusCurve = new(BezierCurveFactory.Linear(0.2f, 1.0f)); // radius = baseRadius * curve(pressure)
    [DataMember] public ReactiveProperty<BrushRenderingType> RenderingType = new(BrushRenderingType.Stamp);
    [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> Pressure2FlowCurve = new(BezierCurveFactory.Constant(1.0f)); // finalFlow = curve(pressure) * Color.a

    // Vanilla
    [DataMember] public ReactiveProperty<float> DashLength = new(2.0f);
    [DataMember] public ReactiveProperty<float> GapLength = new(2.0f);
    [DataMember] public ReactiveProperty<float> DashForwardSpeed = new(0.0f);

    // Stamp
    [DataMember] public ReactiveProperty<StampFlags> ActiveStampFlags = new();
    [DataMember] public ReactiveProperty<float> StampInterval = new(0.4f); // in radius unit
    [DataMember] public ReactiveProperty<ImageTexture> StampTexture = new(null);
    [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> DiskOpacityCurve = new(BezierCurveFactory.EaseInOut(1.0f, 0.0f));
    [DataMember] public ReactiveProperty<float> StampRotation = new(0.0f); // in radian
    [DataMember] public ReactiveProperty<ImageTexture> MaskTexture = new(null);

    [DataMember] public ReactiveProperty<int> RotationNoiseOctave = new(1);
    [DataMember] public ReactiveProperty<float> RotationNoiseAmplitude = new(0.0f);
    [DataMember] public ReactiveProperty<float> RotationNoiseFrequency = new(0.01f);

    // Airbrush
    [DataMember] public ReactiveProperty<ImmutableArray<BezierPoint>> FalloffCurve = new(BezierCurveFactory.Linear(1.0f, 0.0f));
    [DataMember] public ReactiveProperty<float> AlphaDensity = new(1.0f);

    public void DrawProperty(PropertyContainer container)
    {
        var nameEdit = new LineEdit
        {
            FocusMode = Control.FocusModeEnum.Click,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
        }.BindString(Name);
        container.AddProperty("Name", nameEdit);

        var baseRadiusControl = new SpinSlider
        {
            MinValue = 0.1,
            MaxValue = 128,
            Step = 0.03333333,
            ExpEdit = true,
        }.BindNumber(BaseRadius);
        container.AddProperty("Base radius", baseRadiusControl);

        var colorPickerButton = new ColorPickerButton()
        {
            CustomMinimumSize = new(0, 32),
        };
        var picker = colorPickerButton.GetPicker();
        picker.ColorModesVisible = false;
        picker.ColorMode = ColorPicker.ColorModeType.Rgb;
        container.AddProperty("RGB+Flow", colorPickerButton.BindColor(Color));

        var pp2RadiusCurveEdit = new MappingCurveEdit { MinValue = 0.01f }.BindCurve(Pressure2RadiusCurve);
        var aspectBox = new AspectRatioContainer();
        aspectBox.AddChild(pp2RadiusCurveEdit);
        container.AddProperty("Pressure to radius", aspectBox);

        var pp2FlowCurveEdit = new MappingCurveEdit().BindCurve(Pressure2FlowCurve);
        var flowCurveFlagCheck = new CheckBox().BindFlag(ActiveBrushFlags, BrushFlags.Pressure2Flow);
        container.CreateCheckBoxCombo("Pressure to flow", flowCurveFlagCheck, pp2FlowCurveEdit).AddToChildOf(container);

        var typeButton = new OptionButton().BindEnum(RenderingType);
        container.AddProperty("Rendering type", typeButton);

        // ---------Stamp------------ 
        var stampBox = container.CreateBox();
        stampBox.VisibleIf(RenderingType, BrushRenderingType.Stamp);
        container.AddChild(stampBox);

        var stampIntervalControl = new SpinSlider
        {
            MinValue = 1f / 64,
            MaxValue = 6,
            Step = 0.001,
            ExpEdit = true,
            AllowLesser = true,
            AllowGreater = true,
        }.BindNumber(StampInterval);
        container.CreatePropertyBox("Interval", stampIntervalControl).AddToChildOf(stampBox);

        var stampTextureFlagCheck = new CheckBox().BindFlag(ActiveStampFlags, StampFlags.StampTexture);
        var stampTextureEdit = ImageTextureEdit.Instantiate(StampTexture, ConvertStampImage)
            .VisibleIf(ActiveStampFlags, v => v.HasFlag(StampFlags.StampTexture));
        container.CreateCheckBoxCombo("Stamp texture", stampTextureFlagCheck, stampTextureEdit).AddToChildOf(stampBox);

        var maskDiskFlagCheck = new CheckBox().BindFlag(ActiveStampFlags, StampFlags.MaskDisk);
        var diskOpacityCurveEdit = new MappingCurveEdit().BindCurve(DiskOpacityCurve);
        container.CreateCheckBoxCombo("Mask disk", maskDiskFlagCheck, diskOpacityCurveEdit).AddToChildOf(stampBox);

        var maskTextureFlagCheck = new CheckBox().BindFlag(ActiveStampFlags, StampFlags.MaskTexture);
        var maskTextureEdit = ImageTextureEdit.Instantiate(MaskTexture, ConvertStampImage);
        container.CreateCheckBoxCombo("Mask texture", maskTextureFlagCheck, maskTextureEdit).AddToChildOf(stampBox);

        var stampRotationControl = new SpinSlider
        {
            MinValue = -180,
            MaxValue = 180,
            Step = 0.1,
        };
        var degreeView = StampRotation.Project(Mathf.RadToDeg, Mathf.DegToRad);
        degreeView.AddTo(stampRotationControl);
        stampRotationControl.BindNumber(degreeView);
        container.CreatePropertyBox("Stamp rotation", stampRotationControl).AddToChildOf(stampBox);

        var rotationNoiseFlagCheck = new CheckBox().BindFlag(ActiveStampFlags, StampFlags.RotationNoise);
        var rotationNoiseBox = new VBoxContainer();
        container.CreateCheckBoxCombo("Rotation noise", rotationNoiseFlagCheck, rotationNoiseBox).AddToChildOf(stampBox);

        var noiseOctaveControl = new SpinSlider()
        {
            MinValue = 1,
            MaxValue = 8,
            Step = 1,
            AllowGreater = true,
            Rounded = true,
        }.BindNumber(RotationNoiseOctave);
        container.CreatePropertyBox("Rotation noise octave", noiseOctaveControl).AddToChildOf(rotationNoiseBox);

        var rotationNoiseAmplitudeControl = new SpinSlider
        {
            MinValue = 0.0,
            MaxValue = Mathf.Pi * 16,
            Step = 0.01,
        }.BindNumber(RotationNoiseAmplitude);
        container.CreatePropertyBox("Rotation noise amplitude", rotationNoiseAmplitudeControl).AddToChildOf(rotationNoiseBox);

        var rotationNoiseFrequencyControl = new SpinSlider
        {
            MinValue = 0.001,
            MaxValue = 0.5,
            Step = float.E / 10000,
            AllowGreater = true,
            ExpEdit = true,
        }.BindNumber(RotationNoiseFrequency);
        container.CreatePropertyBox("Rotation noise frequency", rotationNoiseFrequencyControl).AddToChildOf(rotationNoiseBox);

        // ---------Airbrush----------
        var falloffCurveEdit = new MappingCurveEdit().BindCurve(FalloffCurve);
        container.AddProperty("Opacity falloff", falloffCurveEdit).VisibleIf(RenderingType, BrushRenderingType.Airbrush);

        var alphaDensityControl = new SpinSlider
        {
            MinValue = 0.1,
            MaxValue = 6,
            Step = 0.01,
            ExpEdit = true,
        }.BindNumber(AlphaDensity);
        container.AddProperty("Opacity density", alphaDensityControl).VisibleIf(RenderingType, BrushRenderingType.Airbrush);
    }

    /// <summary>
    /// Converts the stamp image to L8 format, enforces a maximum size of 256x256 pixels,
    /// and resizes the image to ensure a square aspect ratio.
    /// </summary>
    public static void ConvertStampImage(Image img)
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

    public StrokeBrushSetting Clone()
    {
        var bytes = MessagePackSerializer.Serialize(this);
        var setting = MessagePackSerializer.Deserialize<StrokeBrushSetting>(bytes); // Note: Fields or properties cannot be readonly
        setting.Labels.Remove(BrushLabel.BuiltIn);
        return setting;
    }

    public Func<CursorMotionData, float> ToRadiusSampler()
    {
        var baseRadius = BaseRadius.Value;
        var points = Pressure2RadiusCurve.Value; // capture snapshot at sampler creation time
        return data => baseRadius * points.SampleX(data.Pressure);
    }
}

[Flags]
public enum BrushFlags
{
    Pressure2Flow = 1 << 0,
    Dash = 1 << 1,
}

[Flags]
public enum StampFlags
{
    StampTexture = 1 << 0,
    MaskTexture = 1 << 1,
    RotationNoise = 1 << 2,
    MaskDisk = 1 << 3,
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