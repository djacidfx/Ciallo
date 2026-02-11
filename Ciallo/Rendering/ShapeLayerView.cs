using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.Rendering;

public partial class ShapeLayerView : CanvasGroup
{
    public ShapeLayerView() { }

    // if true, this node can be replaced by a regular node2D
    public bool IsDefault => SelfModulate.IsEqualApprox(Colors.White);

    public CompositeDisposable ObserveLayerSetting(CommonLayerSetting setting)
    {
        CompositeDisposable subs = new();
        setting.IsVisible.Subscribe(SetVisible).AddTo(subs);
        setting.Opacity.Subscribe(v =>
        {
            var color = SelfModulate;
            color.A = v;
            SelfModulate = color;
        }).AddTo(subs);
        return subs;
    }
}