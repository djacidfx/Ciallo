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

    /// <summary>
    /// Horizontal scroll position expressed in <b>frames</b> (not pixels).
    /// A value of 2.5 means frame 2.5 is at the left edge of the visible area.
    /// Use <c>ScrollOffsetFrame * PixelsPerFrame</c> to convert to pixel offset where needed.
    /// </summary>
    public ReactiveProperty<float> ScrollOffsetFrame = new(-10f);
}