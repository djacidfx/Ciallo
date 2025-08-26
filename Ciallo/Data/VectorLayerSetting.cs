using MessagePack;
using R3;

namespace Ciallo.Data;

[MessagePackObject(true), ToSerialize]
public class VectorLayerSetting
{
    public readonly ReactiveProperty<float> Opacity = new(1.0f);
    public readonly ReactiveProperty<bool> IsVisible = new(true);
    public readonly ReactiveProperty<bool> IsLocked = new(false); // Need to implement
    public readonly ReactiveProperty<VectorLayerRenderMode> RenderMode = new(VectorLayerRenderMode.Realtime); // Need to implement
}

public enum VectorLayerRenderMode
{
    Realtime,
    Rasterized, // For performance, render as rasterized image
}