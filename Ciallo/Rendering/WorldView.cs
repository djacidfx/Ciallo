namespace Ciallo.Rendering;

public partial class WorldView : FolderLayerView
{
    // Pitfall: _Ready() invoke when PackScene and Exporting to godot.
    // Should always avoid this.
}