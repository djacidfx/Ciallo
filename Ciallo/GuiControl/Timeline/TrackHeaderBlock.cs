using Godot;

namespace Ciallo.GuiControl;

/// <summary>
/// The visual block for a timeline track header row.
/// Structurally and visually identical to <see cref="LayerBlock"/> but is a distinct type
/// so entities can hold both a <see cref="LayerBlock"/> (Layer panel)
/// and a <see cref="TrackHeaderBlock"/> (Timeline header) as separate Frent components.
/// </summary>
[Instantiable(init: "")]
public partial class TrackHeaderBlock : LayerBlock
{
    /// <summary>
    /// Set by <see cref="TrackTree.Create"/> before the node enters the scene tree.
    /// Avoids fragile scene-depth navigation.
    /// </summary>
    internal LayerWrapper OwningWrapper;

    public override LayerWrapper Wrapper => OwningWrapper;
}