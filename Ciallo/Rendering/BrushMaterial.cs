using System.Linq;
using System.Runtime.InteropServices;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;
using R3;

namespace Ciallo.Rendering;

public partial class BrushMaterial : ShaderMaterial
{
    public CompositeDisposable Subs;

    public BrushMaterial()
    {
        Shader = AutoloadRendering.StrokeShader;
        ResourceLocalToScene = true;
    }

    public void ObserveBrushSetting(BrushSetting setting)
    {
        Subs?.Dispose();
        Subs = new();
        setting.RenderingType.Subscribe(type => SetShaderParameter("StrokeType", (int)type)).AddTo(Subs);
        setting.Color.Subscribe(color => SetShaderParameter("MaterialColor", color)).AddTo(Subs);
        setting.DashLength.Subscribe(length => SetShaderParameter("DashLength", length)).AddTo(Subs);
        setting.GapLength.Subscribe(length => SetShaderParameter("GapLength", length)).AddTo(Subs);
        setting.DashForwardSpeed.Subscribe(speed => SetShaderParameter("DashForwardSpeed", speed)).AddTo(Subs);
        // Brush-level flags
        setting.ActiveBrushFlags.Subscribe(value => SetShaderParameter("ActiveBrushFlags", (int)value)).AddTo(Subs);
        var pp2FlowTex = ImageTexture.CreateFromImage(BakeCurve(setting.Pressure2FlowCurve));
        setting.Pressure2FlowCurve.Changed.Prepend(new Unit()).Subscribe(_ =>
        {
            pp2FlowTex.Update(BakeCurve(setting.Pressure2FlowCurve));
            SetShaderParameter("Pressure2FlowCurve", pp2FlowTex);
        }).AddTo(Subs);

        // Stamp
        setting.ActiveStampFlags.Subscribe(value => SetShaderParameter("ActiveStampFlags", (int)value)).AddTo(Subs);
        setting.StampInterval.Subscribe(interval => SetShaderParameter("StampInterval", interval)).AddTo(Subs);
        SetShaderParameter("StampTexture", setting.StampTexture);
        SetShaderParameter("MultiplyTexture", setting.MaskTexture);
        setting.StampRotation.Subscribe(rotation =>
        {
            var transform = new Transform2D(rotation, Vector2.Zero);
            SetShaderParameter("CoordinateTransform", transform);
        }).AddTo(Subs);

        setting.RotationNoiseOctave.Subscribe(value => SetShaderParameter("RotationNoiseOctave", value)).AddTo(Subs);
        setting.RotationNoiseAmplitude.Subscribe(value => SetShaderParameter("RotationNoiseAmplitude", value)).AddTo(Subs);
        setting.RotationNoiseFrequency.Subscribe(value => SetShaderParameter("RotationNoiseFrequency", value)).AddTo(Subs);

        var falloffTex = ImageTexture.CreateFromImage(BakeCurve(setting.FalloffCurve));
        setting.FalloffCurve.Changed.Prepend(new Unit()).Subscribe(_ =>
        {
            falloffTex.Update(BakeCurve(setting.FalloffCurve));
            SetShaderParameter("FalloffCurve", falloffTex);
        }).AddTo(Subs);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GD.Print("deleting brush material");
            Subs.Dispose();
        }
    }

    public static Image BakeCurve(BezierCurve curve)
    {
        int n = 512;
        curve.Tessellate(n);
        var data = curve.SampleXList(Enumerable.Range(0, n).Select(i => (float)i / n).ToArray());
        var bytes = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(data));
        var img = Image.CreateFromData(data.Count, 1, false, Image.Format.Rf, bytes);
        img.GenerateMipmaps();
        return img;
    }
}