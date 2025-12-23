using System.Runtime.Serialization;
using Ciallo.Command;
using R3;

namespace Ciallo.Data;

/// <summary>
/// Common layer settings component for all layer types.
/// </summary>
[DataContract, ToSerialize]
public class CommonLayerSetting
{
    [DataMember] public ReactiveProperty<string> Name = new("");
    [DataMember] public ReactiveProperty<bool> IsVisible = new(true);
    [DataMember] public ReactiveProperty<float> Opacity = new(1.0f);
    [DataMember] public ReactiveProperty<bool> IsLocked = new(false);

    public CompositeDisposable RegisterProperties(CommandManager manager)
    {
        CompositeDisposable subs = new();
        manager.RegisterProperty(Name).AddTo(subs);
        manager.RegisterProperty(IsVisible).AddTo(subs);
        manager.RegisterProperty(Opacity).AddTo(subs);
        return subs;
    }

    public void CopySettingFrom(CommonLayerSetting other)
    {
        Name.Value = other.Name.Value;
        IsVisible.Value = other.IsVisible.Value;
        Opacity.Value = other.Opacity.Value;
        IsLocked.Value = other.IsLocked.Value;
    }
}