using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.Rendering;

public partial class BrushMaterial : ShaderMaterial
{
    public readonly CompositeDisposable Subs = new();
    public static readonly Shader StrokeShader = GD.Load<Shader>("res://Rendering/Stroke.gdshader");
    
    public BrushMaterial()
    {
        Shader = StrokeShader;
    }
    
    public void ObserveBrushSetting(BrushSetting setting)
    {
        Subs.Dispose();
        var subs = Subs;
        setting.RenderingType.Subscribe(type => SetShaderParameter("strokeType", (int)type)).AddTo(subs);
        setting.Color.Subscribe(color => SetShaderParameter("materialColor", color)).AddTo(subs);
        setting.DashLength.Subscribe(length => SetShaderParameter("dashLength", length)).AddTo(subs);
        setting.GapLength.Subscribe(length => SetShaderParameter("gapLength", length)).AddTo(subs);
        setting.DashForwardSpeed.Subscribe(speed => SetShaderParameter("dashForwardSpeed", speed)).AddTo(subs);
        setting.StampInterval.Subscribe(interval => SetShaderParameter("stampInterval", interval)).AddTo(subs);
        // TODO: falloff curve.
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GD.Print("deleting brush material");
            Subs.Dispose();
        }
    }
}