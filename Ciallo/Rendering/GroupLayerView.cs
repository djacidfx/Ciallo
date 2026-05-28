using Ciallo.Data;
using Godot;
using R3;

namespace Ciallo.Rendering;

/// <summary>
/// For layers using CanvasGroup, i.e. ShapeLayer and FolderLayer. CelLayer uses a custom CelFolderView instead.
/// </summary>
public partial class GroupLayerView : CanvasGroup
{
    // if true, this node can be replaced by a regular node2D
    public bool IsDefault => SelfModulate.IsEqualApprox(Colors.White);

    public CompositeDisposable ObserveLayerSetting(CommonLayerSetting setting)
    {
        CompositeDisposable subs = new();
        setting.IsVisible.Subscribe(SetVisible).AddTo(subs);
        setting.Opacity.Subscribe(v =>
        {
            SelfModulate = SelfModulate with { A = v };
        }).AddTo(subs);
        return subs;
    }
}