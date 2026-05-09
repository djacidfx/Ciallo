using System.Runtime.Serialization;
using ObservableCollections;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class TimelineSetting
{
    // [start, end)
    [DataMember] public ReactiveProperty<int> PlaybackStart = new(0);
    [DataMember] public ReactiveProperty<int> PlaybackEnd = new(24);
    [DataMember] public ReactiveProperty<float> FrameRate = new(24);
    [DataMember] public ObservableHashSet<int> OnionSkinFrames = new([-1, 1]);

    public ReactiveProperty<float> PixelsPerFrame = new(20f);
    public ReactiveProperty<float> ScrollOffsetPixels = new(0f);
}