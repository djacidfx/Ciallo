using Ciallo.Data;
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