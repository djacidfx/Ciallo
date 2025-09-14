using MessagePack;
using R3;

namespace Ciallo.Data;

[MessagePackObject(true), ToSerialize]
public class PolylineLayerSetting
{
    public readonly ReactiveProperty<float> Opacity = new(1.0f);
    public readonly ReactiveProperty<bool> IsLocked = new(false); // Need to implement
    public readonly ReactiveProperty<PolylineLayerRenderMode> RenderMode = new(PolylineLayerRenderMode.Realtime);
}

public enum PolylineLayerRenderMode
{
    Realtime,
    Rasterized, // For performance, render as rasterized image, need to implement
}