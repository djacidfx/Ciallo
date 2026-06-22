namespace Ciallo.GuiControl;

/// <summary>
/// Wraps a full-width timeline track row (<see cref="TrackRow"/>) for one layer entity.
/// Structurally mirrors <see cref="LayerWrapper"/> but its
/// <see cref="FoldableVBoxContainer.Title"/> is a <see cref="TrackRow"/> (HSplitContainer)
/// rather than a bare <see cref="LayerBlock"/>.
/// Each entity holds both a <see cref="LayerWrapper"/> (Layer panel) and a
/// <see cref="TrackRowWrapper"/> (Timeline) as separate Frent components.
/// </summary>
public partial class TrackRowWrapper : LayerWrapper
{
    /// <summary>The header block extracted from the <see cref="TrackRow"/> title.</summary>
    public override ILayerBlock Block => (Title as TrackRow)?.HeaderBlock;

    public TrackHeaderBlock HeaderBlock => (Title as TrackRow)?.HeaderBlock;

    public override void _EnterTree()
    {
        base._EnterTree();
        // Timeline shows cel folders via their CelTrack, not as individual child rows.
        // Hide every row inside a cel folder so an expanded cel folder shows only its archetypes.
        Visible = !IsBeingCeled;
    }
}
