using System.Collections.Immutable;
using System.Runtime.Serialization;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class TimelineSetting
{
    // [start, end)
    [DataMember] public ReactiveProperty<int> PlaybackStart = new(0);
    [DataMember] public ReactiveProperty<int> PlaybackEnd = new(24);
    [DataMember] public ReactiveProperty<float> FrameRate = new(24);
    [DataMember] public ReactiveProperty<bool> LoopPlaybackEnabled = new(false);
    [DataMember] public ReactiveProperty<bool> OnionSkinEnabled = new(false);
    [DataMember] public ReactiveProperty<ImmutableArray<int>> OnionSkinFrames = new([-1, 1]);

    public ReactiveProperty<float> PixelsPerFrame = new(32f);

    /// <summary>
    /// Horizontal scroll position expressed in <b>frames</b> (not pixels).
    /// A value of 2.5 means frame 2.5 is at the left edge of the visible area.
    /// Use <c>ScrollOffsetFrame * PixelsPerFrame</c> to convert to pixel offset where needed.
    /// </summary>
    public ReactiveProperty<float> ScrollOffsetFrame = new(-5f);

    public void CopyFrom(TimelineSetting other)
    {
        PlaybackStart.Value = other.PlaybackStart.Value;
        PlaybackEnd.Value = other.PlaybackEnd.Value;
        FrameRate.Value = other.FrameRate.Value;
        LoopPlaybackEnabled.Value = other.LoopPlaybackEnabled.Value;
        OnionSkinEnabled.Value = other.OnionSkinEnabled.Value;
        OnionSkinFrames.Value = other.OnionSkinFrames.Value;
        PixelsPerFrame.Value = other.PixelsPerFrame.Value;
        ScrollOffsetFrame.Value = other.ScrollOffsetFrame.Value;
    }
}
