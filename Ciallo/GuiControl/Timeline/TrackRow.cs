using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// A full-width timeline track row.
/// Left panel: <see cref="TrackHeaderBlock"/> (always visible).
/// Right panel: <see cref="CelTrack"/> (only for CelFolder layers) or an empty placeholder.
/// Split offset is kept in sync with the HSplitRuler by <see cref="TrackTree"/>.
/// </summary>
[Tool]
public partial class TrackRow : HSplitContainer
{
    /// <summary>The header block occupying the left panel.</summary>
    public TrackHeaderBlock HeaderBlock { get; set; }

    /// <summary>
    /// The cel track in the right panel, or <c>null</c> for non-CelFolder layers.
    /// </summary>
    public CelTrack CelTrack { get; set; }

    public TrackRow()
    {
        // Hard coded separation value from HSplitRuler's separation.
        AddThemeConstantOverride("separation", 12);

        AddThemeStyleboxOverride("split_bar_background", new StyleBoxEmpty());
    }
}
