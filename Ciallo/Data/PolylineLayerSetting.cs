using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class PolylineLayerSetting
{
    [DataMember] public ReactiveProperty<float> Opacity = new(1.0f);
    [DataMember] public ReactiveProperty<bool> IsLocked = new(false); // Need to implement
    [DataMember] public ReactiveProperty<PolylineLayerRenderMode> RenderMode = new(PolylineLayerRenderMode.Realtime);
}

public enum PolylineLayerRenderMode
{
    Realtime,
    Rasterized, // For performance, render as rasterized image, need to implement
}