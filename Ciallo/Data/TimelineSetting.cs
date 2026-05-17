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

    public void CopyFrom(TimelineSetting other)
    {
        PlaybackStart.Value = other.PlaybackStart.Value;
        PlaybackEnd.Value = other.PlaybackEnd.Value;
        FrameRate.Value = other.FrameRate.Value;
        OnionSkinFrames.Clear();
        foreach (var frame in other.OnionSkinFrames)
            OnionSkinFrames.Add(frame);
        PixelsPerFrame.Value = other.PixelsPerFrame.Value;
        ScrollOffsetFrame.Value = other.ScrollOffsetFrame.Value;
    }
}