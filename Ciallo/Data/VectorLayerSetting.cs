using MessagePack;
using R3;

namespace Ciallo.Data;

[MessagePackObject(true), ToSerialize]
public class VectorLayerSetting
{
    public readonly ReactiveProperty<float> Opacity = new(1.0f);
    public readonly ReactiveProperty<bool> IsLocked = new(false); // Need to implement
    public readonly ReactiveProperty<VectorLayerRenderMode> RenderMode = new(VectorLayerRenderMode.Realtime);
}

public enum VectorLayerRenderMode
{
    Realtime,
    Rasterized, // For performance, render as rasterized image, need to implement
}