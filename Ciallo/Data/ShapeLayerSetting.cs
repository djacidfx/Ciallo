using System.Runtime.Serialization;
using MessagePack;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class ShapeLayerSetting
{
    [DataMember, ProjectField] public ReactiveProperty<PolylineLayerRenderMode> RenderMode = new(PolylineLayerRenderMode.Realtime);

    public ShapeLayerSetting Clone()
    {
        var bytes = MessagePackSerializer.Serialize(this);
        return MessagePackSerializer.Deserialize<ShapeLayerSetting>(bytes);
    }
}

public enum PolylineLayerRenderMode
{
    Realtime,
    Rasterized, // For performance, render as rasterized image, need to implement
}
