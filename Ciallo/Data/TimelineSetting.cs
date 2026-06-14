using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Godot;
using R3;

namespace Ciallo.Data;

[DataContract, ToSerialize]
public class TimelineSetting
{
    // [start, end)
    [DataMember, ProjectField] public ReactiveProperty<int> PlaybackStart = new(0);
    [DataMember, ProjectField] public ReactiveProperty<int> PlaybackEnd = new(24);
    [DataMember, ProjectField] public ReactiveProperty<float> FrameRate = new(24);
    [DataMember, ProjectField] public ReactiveProperty<bool> LoopPlaybackEnabled = new(false);
    [DataMember, ProjectField] public ReactiveProperty<bool> OnionSkinEnabled = new(false);
    // Offsets in exposure index, negative for past frames, positive for future frames.
    [DataMember, ProjectField(StorageKind.Blob)] public ReactiveProperty<ImmutableArray<int>> OnionSkinOffsets = new([-1, 1]);

    public ReactiveProperty<bool> IsRollingFrame = new(false);
    public readonly ReadOnlyReactiveProperty<SortedList<int, ShaderMaterial>> OnionSkinMaterials;

    public TimelineSetting()
    {
        var shader = GD.Load<Shader>("res://Rendering/OnionSkin.gdshader");
        OnionSkinMaterials = OnionSkinOffsets.Select(offsets =>
        {
            var materials = new SortedList<int, ShaderMaterial>();
            foreach (var offset in offsets)
            {
                var material = new ShaderMaterial() { Shader = shader };
                Color color = offset switch
                {
                    < 0 => Colors.Blue with { A = 0.4f },
                    > 0 => Colors.Red with { A = 0.4f },
                    _ => Colors.White
                };
                material.SetShaderParameter("OverridingColor", color);
                materials.Add(offset, material);
            }

            return materials;
        }).ToReadOnlyReactiveProperty();
    }

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
        OnionSkinOffsets.Value = other.OnionSkinOffsets.Value;
        PixelsPerFrame.Value = other.PixelsPerFrame.Value;
        ScrollOffsetFrame.Value = other.ScrollOffsetFrame.Value;
    }
}
