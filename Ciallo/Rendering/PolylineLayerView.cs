using Godot;

namespace Ciallo.Rendering;

public partial class PolylineLayerView : CanvasGroup
{
    public PolylineLayerView()
    {
    }

    // if true, this node can be replaced by a regular node2D
    public bool IsDefault => true;
}