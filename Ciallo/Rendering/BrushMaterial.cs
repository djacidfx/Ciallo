using System.Linq;
using Ciallo.Data;
using Ciallo.Geometry;
using Godot;
using R3;

namespace Ciallo.Rendering;

public partial class BrushMaterial : ShaderMaterial
{
    public CompositeDisposable Subs;
    public static readonly Shader StrokeShader = GD.Load<Shader>("res://Rendering/Stroke.gdshader");
    
    public BrushMaterial()
    {
        Shader = StrokeShader;
    }
    
    public void ObserveBrushSetting(BrushSetting setting)
    {
        Subs?.Dispose();
        Subs = new();
        setting.RenderingType.Subscribe(type => SetShaderParameter("strokeType", (int)type)).AddTo(Subs);
        setting.Color.Subscribe(color => SetShaderParameter("materialColor", color)).AddTo(Subs);
        setting.DashLength.Subscribe(length => SetShaderParameter("dashLength", length)).AddTo(Subs);
        setting.GapLength.Subscribe(length => SetShaderParameter("gapLength", length)).AddTo(Subs);
        setting.DashForwardSpeed.Subscribe(speed => SetShaderParameter("dashForwardSpeed", speed)).AddTo(Subs);
        
        setting.StampInterval.Subscribe(interval => SetShaderParameter("stampInterval", interval)).AddTo(Subs);
        
        var falloffTex = ImageTexture.CreateFromImage(BakeCurve(setting.FalloffCurve));
        setting.FalloffCurve.Changed.Prepend(new Unit()).Subscribe(_ =>
        {
            falloffTex.Update(BakeCurve(setting.FalloffCurve));
            SetShaderParameter("falloffCurve", falloffTex);
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
        int n = 256;
        var data = curve.SampleXList(Enumerable.Range(0, n).Select(i => (float)i / n).ToArray());
        var img = Image.CreateFromData(data.Count, 1, false, Image.Format.L8, data.Select(c => (byte)(c * 255)).ToArray());
        img.GenerateMipmaps();
        // img.GenerateMipmaps();
        return img;
    }
}