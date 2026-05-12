namespace Ciallo.GuiControl;

/// <summary>
/// Wraps a timeline track-header's block.
/// Structurally identical to <see cref="LayerWrapper"/> but is a distinct type so that
/// each layer entity can hold BOTH a <see cref="LayerWrapper"/> (for the Layer panel)
/// and a <see cref="TrackHeaderWrapper"/> (for the Timeline header) as separate Frent components.
/// </summary>
public partial class TrackHeaderWrapper : LayerWrapper;