using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class PolylineLayerSetting
{
    [DataMember] public ReactiveProperty<PolylineLayerRenderMode> RenderMode = new(PolylineLayerRenderMode.Realtime);
}

public enum PolylineLayerRenderMode
{
    Realtime,
    Rasterized, // For performance, render as rasterized image, need to implement
}